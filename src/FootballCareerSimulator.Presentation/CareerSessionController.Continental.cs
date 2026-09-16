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

        _continentalTournament = new ContinentalTournamentEngine(
            timeline.RootSeed,
            clubId,
            managedName,
            managedStrength,
            otherClubs,
            startDayNumber: startDay);

        return _continentalTournament;
    }

    public ContinentalTournamentDigest BuildContinentalTournamentDigest()
    {
        var engine = GetOrInitContinentalTournament();
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
        var opponentStrength = EuropeanElite202627DataPack.TryGetClub(opponentId, out var elite)
            ? elite.SportiveStrength
            : 80;

        var homeStrength = isHome ? managedStrength : opponentStrength;
        var awayStrength = isHome ? opponentStrength : managedStrength;

        var seed = unchecked(engine.WorldSeed ^ (int)fixture.FixtureId ^ currentDay);
        var outcome = MvpFixtureMatchSimulator.SimulateWithKeyMoments(
            seed,
            fixture.FixtureId,
            homeStrength,
            awayStrength);

        var homeGoals = outcome.Score.HomeGoals;
        var awayGoals = outcome.Score.AwayGoals;
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

        // Sonucu kıtasal motora kaydet
        engine.RecordManagedMatchResult(fixture.FixtureId, homeGoals, awayGoals, outcome.KeyMoments);

        // UEFA Primini Transfer Bütçesine Ekle
        if (managedGoals > opponentGoals)
        {
            Host.ClubModule.TransferBudget.CreditPrize(
                new ClubId(engine.ManagedClubId),
                ContinentalTournamentEngine.WinPrizeMoney);
        }
        else if (managedGoals == opponentGoals)
        {
            Host.ClubModule.TransferBudget.CreditPrize(
                new ClubId(engine.ManagedClubId),
                ContinentalTournamentEngine.DrawPrizeMoney);
        }

        var scoreLine = $"{fixture.HomeClubName} {homeGoals} - {awayGoals} {fixture.AwayClubName}";
        var prizeLine = managedGoals > opponentGoals
            ? $"\n⭐ Avrupa Galibiyeti Primi: +{ContinentalTournamentEngine.WinPrizeMoney:N0} TRY transfer bütçesine eklendi!"
            : managedGoals == opponentGoals
                ? $"\n⭐ Avrupa Beraberliği Primi: +{ContinentalTournamentEngine.DrawPrizeMoney:N0} TRY transfer bütçesine eklendi!"
                : string.Empty;

        // 🎖️ Avrupa Maçı Menajer XP İlerlemesi
        var managerCareer = Host.ManagerModule.Store.Career;
        var xpEarned = managedGoals > opponentGoals
            ? (int)(ManagerProgression.WinXp * 1.5)
            : (managedGoals == opponentGoals ? (int)(ManagerProgression.DrawXp * 1.5) : ManagerProgression.LossXp);
        var oldLevel = managerCareer.Progression.Level;
        var updatedManager = managerCareer.AddExperience(xpEarned);
        Host.ManagerModule.Store.Replace(updatedManager);

        var matchLines = new List<string>
        {
            $"⭐ {fixture.StageName}: {scoreLine}{prizeLine}",
            updatedManager.Progression.Level > oldLevel
                ? $"🎖️ TEBRİKLER! Menajer Seviye {updatedManager.Progression.Level}'e yükseldi! (+{xpEarned} XP · Becerileriniz gelişti)"
                : $"🎖️ Menajer Deneyimi: +{xpEarned} XP (Seviye {updatedManager.Progression.Level} · Toplam {updatedManager.Progression.ExperiencePoints} XP)"
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
