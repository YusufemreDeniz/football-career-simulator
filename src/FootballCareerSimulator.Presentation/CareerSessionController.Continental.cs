using FootballCareerSimulator.Application.CareerHub.Queries;
using FootballCareerSimulator.Application.Competition.Commands;
using FootballCareerSimulator.Application.Competition.Queries;
using FootballCareerSimulator.Domain.Match;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Simulation.Continental;
using FootballCareerSimulator.Simulation.DataPacks;
using FootballCareerSimulator.Simulation.Match;
using FootballCareerSimulator.Simulation.TeamPreparation;
using FootballCareerSimulator.Domain.ManagerCareer;

namespace FootballCareerSimulator.Presentation;

public sealed partial class CareerSessionController
{
    private ContinentalTournamentEngine? _continentalTournament;

    public ContinentalTournamentEngine GetOrInitContinentalTournament()
    {
        if (_continentalTournament is not null)
        {
            return _continentalTournament;
        }

        var timeline = Host.WorldModule.TimelineStore.Timeline;
        var career = Host.ManagerModule.Queries.GetCareer();
        var clubId = career.EmployedClubId ?? 1L;
        var managedClub = Host.ClubModule.Queries.GetClub(clubId);
        var managedName = managedClub?.DisplayName ?? "Kulübümüz";
        var managedStrength = managedClub?.SportiveStrength ?? 80;

        var otherClubs = Host.ClubModule.Queries.GetAllClubs()
            .Where(c => c.ClubId != clubId)
            .Select(c => (c.ClubId, c.DisplayName, c.SportiveStrength))
            .ToList();

        // Sezon yılı belirleme: 1 Temmuz'da başlayan sezon (örn. 2026-2027)
        // Avrupa Şampiyonlar Kupası grup maçları Eylül ayında başlar (15 Eylül)
        var currentDate = timeline.CurrentDate;
        var seasonStartYear = currentDate.Month >= 7 ? currentDate.Year : currentDate.Year - 1;
        var startDay = GameDate.FromCalendarDate(seasonStartYear, 9, 15).DayNumber;

        // Kayıtlı turnuva aynı sezon ve kulübe aitse, motor aynı tohum ve güçle kurulur ve
        // yönetilen kulübün sonuçları sırayla yeniden uygulanır (primler zaten bütçeye yazıldığı için tekrar ödenmez).
        var record = Host.Continental.ResumeOrStart(seasonStartYear, clubId, managedStrength);
        managedStrength = record.ManagedClubStrength;

        var engine = new ContinentalTournamentEngine(
            timeline.RootSeed,
            clubId,
            managedName,
            managedStrength,
            otherClubs,
            startDayNumber: startDay);

        foreach (var result in record.ManagedResults)
        {
            engine.RecordManagedMatchResult(result.FixtureId, result.HomeGoals, result.AwayGoals);
        }

        _continentalTournament = engine;
        return engine;
    }

    /// <summary>
    /// Yönetilen kulüp elense bile kalan yapay zeka turları güncel güne kadar oynanır.
    /// </summary>
    private ContinentalTournamentEngine GetContinentalTournamentAdvancedToToday()
    {
        var engine = GetOrInitContinentalTournament();
        engine.AdvanceTo(Host.WorldModule.Queries.GetCurrentGameDate().DayNumber);
        return engine;
    }

    public ContinentalTournamentDigest BuildContinentalTournamentDigest()
    {
        var engine = GetContinentalTournamentAdvancedToToday();
        var currentDay = Host.WorldModule.Queries.GetCurrentGameDate().DayNumber;
        return ContinentalTournamentDigest.Compose(engine, currentDay);
    }

    public bool HasDueContinentalMatch()
    {
        var engine = GetOrInitContinentalTournament();
        var currentDay = Host.WorldModule.Queries.GetCurrentGameDate().DayNumber;
        var nextManaged = engine.GetNextManagedFixture(currentDay);
        return nextManaged is not null && nextManaged.ScheduledDayNumber <= currentDay;
    }

    public ContinentalFixture? GetNextContinentalFixture()
    {
        var engine = GetOrInitContinentalTournament();
        var currentDay = Host.WorldModule.Queries.GetCurrentGameDate().DayNumber;
        return engine.GetNextManagedFixture(currentDay);
    }

    public IReadOnlyList<ContinentalFixture> GetContinentalFixtures(int? roundNumber = null)
    {
        var engine = GetOrInitContinentalTournament();
        if (roundNumber.HasValue)
        {
            return engine.AllFixtures.Where(f => f.RoundNumber == roundNumber.Value).ToList();
        }

        return engine.AllFixtures;
    }

    public PlayMatchesUiResult PlayDueContinentalMatch()
    {
        var engine = GetOrInitContinentalTournament();
        var currentDay = Host.WorldModule.Queries.GetCurrentGameDate().DayNumber;
        var fixture = engine.GetNextManagedFixture(currentDay);

        if (fixture is null || fixture.IsPlayed)
        {
            return new PlayMatchesUiResult(
                false,
                "Oynanacak Avrupa maçı bulunamadı.",
                Array.Empty<string>(),
                Narrative: MatchNightNarrative.Failure("Oynanacak Avrupa maçı yok."));
        }

        var isHome = fixture.HomeClubId == engine.ManagedClubId;
        var opponentId = isHome ? fixture.AwayClubId : fixture.HomeClubId;
        var opponentName = isHome ? fixture.AwayClubName : fixture.HomeClubName;

        var managedClub = Host.ClubModule.Queries.GetClub(engine.ManagedClubId);
        var managedStrength = managedClub?.SportiveStrength ?? 78;
        var opponentStrength = engine.GetClubStrength(opponentId);

        var homeStrength = isHome ? managedStrength : opponentStrength;
        var awayStrength = isHome ? opponentStrength : managedStrength;

        var seed = unchecked(engine.WorldSeed ^ (int)fixture.FixtureId ^ currentDay);
        var outcome = MvpFixtureMatchSimulator.SimulateWithKeyMoments(
            seed,
            fixture.FixtureId,
            homeStrength,
            awayStrength);

        // Eleme maçında beraberlik penaltılarla çözülür; kayıt ve ekran aynı skoru kullanır.
        var (homeGoals, awayGoals, decidedOnPenalties) = engine.ApplyKnockoutTieBreak(
            fixture,
            outcome.Score.HomeGoals,
            outcome.Score.AwayGoals);
        var managedGoals = isHome ? homeGoals : awayGoals;
        var opponentGoals = isHome ? awayGoals : homeGoals;

        // Haritalama: Oyuncu isimlerini anlara iliştir
        var homeProfiles = GetRosterProfiles(fixture.HomeClubId);
        var awayProfiles = GetRosterProfiles(fixture.AwayClubId);

        var readMoments = outcome.KeyMoments.Select(m =>
        {
            var roster = m.IsHomeSide ? homeProfiles : awayProfiles;
            var pName = m.PrimarySlotIndex >= 0 && m.PrimarySlotIndex < roster.Count
                ? roster[m.PrimarySlotIndex].DisplayName
                : "Oyuncu";
            var aName = m.AssistSlotIndex.HasValue && m.AssistSlotIndex.Value >= 0 && m.AssistSlotIndex.Value < roster.Count
                ? roster[m.AssistSlotIndex.Value].DisplayName
                : null;

            return new MatchKeyMomentReadModel(
                m.Kind.ToString(),
                m.Minute,
                m.IsHomeSide,
                m.PrimarySlotIndex,
                m.AssistSlotIndex,
                pName,
                aName);
        }).ToList();

        // Sonucu kıtasal motora ve kayda yazılacak özete kaydet; motorun döndürdüğü prim tam olarak bütçeye yazılır
        // (grup galibiyeti/beraberliği, tur atlama ve kupa primi dahil).
        var prizeDelta = engine.RecordManagedMatchResult(fixture.FixtureId, homeGoals, awayGoals, outcome.KeyMoments);
        Host.Continental.RecordManagedResult(fixture.FixtureId, homeGoals, awayGoals);

        if (prizeDelta > 0)
        {
            Host.ClubModule.TransferBudget.CreditPrize(new ClubId(engine.ManagedClubId), prizeDelta);
        }

        var scoreLine = $"{fixture.HomeClubName} {homeGoals} - {awayGoals} {fixture.AwayClubName}"
            + (decidedOnPenalties ? " (pen.)" : string.Empty);
        var prizeLine = prizeDelta > 0
            ? $"\n⭐ Avrupa Primi: +{prizeDelta:N0} TRY transfer bütçesine eklendi!"
            : string.Empty;

        // 🎖️ Avrupa maçı menajer XP'si Application servisi üzerinden verilir (lig maçlarında yönetim değerlendirmesi verir).
        var award = Host.ManagerModule.Experience.AwardContinentalMatch(managedGoals, opponentGoals);

        var matchLines = new List<string>
        {
            $"⭐ {fixture.StageName}: {scoreLine}{prizeLine}",
            award.LeveledUp
                ? $"🎖️ TEBRİKLER! Menajer Seviye {award.Level}'e yükseldi! (+{award.ExperienceEarned} XP · Becerileriniz gelişti)"
                : $"🎖️ Menajer Deneyimi: +{award.ExperienceEarned} XP (Seviye {award.Level} · Toplam {award.TotalExperiencePoints} XP)"
        };

        var beats = new List<string>
        {
            managedGoals > opponentGoals
                ? $"⭐ Avrupa'da Tarihi Zafer! {managedClub?.DisplayName ?? "Kulübümüz"}, {opponentName} karşısında sahadan 3 puanla ayrıldı."
                : managedGoals == opponentGoals
                    ? $"⭐ Avrupa'da Kritik Puan! {managedClub?.DisplayName ?? "Kulübümüz"}, {opponentName} ile berabere kaldı."
                    : $"⭐ Avrupa'da Zorlu Gece: {opponentName} sahadan galip ayrıldı."
        };

        var narrative = MatchNightNarrative.Compose(
            scoreline: scoreLine,
            homeGoals: homeGoals,
            awayGoals: awayGoals,
            managedIsHome: isHome,
            hasManagedMatch: true,
            tacticNote: $"⭐ {fixture.StageName}",
            dayNumber: currentDay,
            beatLines: beats,
            afterWhistleLines: matchLines,
            otherScorelines: Array.Empty<string>());

        return new PlayMatchesUiResult(
            Succeeded: true,
            Message: $"Avrupa maçı tamamlandı: {scoreLine}",
            MatchLines: matchLines,
            ManagedGoals: managedGoals,
            OpponentGoals: opponentGoals,
            MatchSequenceSeed: seed,
            Narrative: narrative,
            KeyMoments: readMoments);
    }

    private IReadOnlyList<MvpSquadPlayerProfile> GetRosterProfiles(long clubId)
    {
        if (EuropeanElite202627DataPack.TryGetClub(clubId, out var elite))
        {
            return elite.Players;
        }

        return GetSquadProfiles(new ClubId(clubId));
    }
}
