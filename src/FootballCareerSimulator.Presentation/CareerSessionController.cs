using FootballCareerSimulator.Application.Competition.Commands;
using FootballCareerSimulator.Application.Competition.Composition;
using FootballCareerSimulator.Application.ManagerCareer.Commands;
using FootballCareerSimulator.Application.TeamPreparation.Commands;
using FootballCareerSimulator.Application.TrainingPhysicalState.Commands;
using FootballCareerSimulator.Application.TrainingPhysicalState.Queries;
using FootballCareerSimulator.Application.WorldCalendar.Commands;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.TrainingPhysicalState;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Presentation;

/// <summary>
/// Kariyer hub'ının Application komutlarını tek yerden çağırır; UI'ye Türkçe sonuç mesajı döner.
/// </summary>
public sealed class CareerSessionController
{
    public const long DefaultSeasonId = 1;

    private long _nextPlanningPeriodId = 1;

    public CareerSessionController(CareerPresentationHost host)
    {
        Host = host ?? throw new ArgumentNullException(nameof(host));
    }

    public CareerPresentationHost Host { get; }

    public bool SaveFileExists() => File.Exists(Host.DefaultSavePath);

    public UiActionResult EnsureLeagueReady()
    {
        try
        {
            var competition = Host.CompetitionModule;
            var world = Host.WorldModule;
            var currentDay = world.Queries.GetCurrentGameDate().DayNumber;
            var season = competition.Queries.GetCurrentSeason();

            if (season is null)
            {
                competition.CreateSeason.Handle(
                    new CreateSeasonCommand(Guid.NewGuid(), DefaultSeasonId, currentDay));
                season = competition.Queries.GetCurrentSeason();
            }

            if (season is null)
            {
                return UiActionResult.Fail("Sezon oluşturulamadı.");
            }

            if (season.ParticipantCount < CompetitionMvpConstraints.LeagueTeamCount)
            {
                for (var clubId = season.ParticipantCount + 1L;
                     clubId <= CompetitionMvpConstraints.LeagueTeamCount;
                     clubId++)
                {
                    competition.RegisterSeasonParticipant.Handle(
                        new RegisterSeasonParticipantCommand(Guid.NewGuid(), DefaultSeasonId, clubId));
                }

                season = competition.Queries.GetCurrentSeason()!;
            }

            if (string.Equals(season.Status, nameof(SeasonStatus.Preseason), StringComparison.Ordinal))
            {
                competition.StartSeason.Handle(
                    new StartSeasonCommand(Guid.NewGuid(), DefaultSeasonId, currentDay));
                season = competition.Queries.GetCurrentSeason()!;
            }

            if (season.FixtureCount == 0)
            {
                competition.PlanLeagueFixtures.Handle(
                    new PlanLeagueFixturesCommand(
                        Guid.NewGuid(),
                        DefaultSeasonId,
                        ComputeFirstMatchdayDayNumber(currentDay),
                        StartingFixtureId: 1));

                season = competition.Queries.GetCurrentSeason()!;
            }

            return UiActionResult.Ok(
                $"Lig hazır: sezon #{season.SeasonId}, {season.ParticipantCount} takım, {season.FixtureCount} maç.");
        }
        catch (Exception ex)
        {
            return UiActionResult.Fail($"Lig kurulumu başarısız: {ex.Message}");
        }
    }

    public UiActionResult ApproveDefaultSelectionForNextDueMatch()
    {
        try
        {
            var currentDay = Host.WorldModule.Queries.GetCurrentGameDate().DayNumber;
            var pending = Host.TeamPreparationModule.SelectionQueries.GetNextDueManagedFixture(currentDay)
                ?? throw new InvalidOperationException("Onaylanacak vadesi gelmiş maç yok.");

            if (pending.IsApproved)
            {
                return UiActionResult.Ok(
                    $"Kadro zaten onaylı: fikstür #{pending.FixtureId} ({pending.ScheduledIsoDate}).");
            }

            var clubId = Host.ManagerModule.Queries.GetCareer().EmployedClubId
                ?? throw new InvalidOperationException("Menajer kulübü yok.");

            Host.TeamPreparationModule.ApproveDefaultSelection.Handle(
                new ApproveDefaultMatchSelectionCommand(
                    Guid.NewGuid(),
                    pending.FixtureId,
                    clubId));

            var opponent = GetClubDisplayName(pending.OpponentClubId);
            var venue = pending.IsHome ? "ev sahibi" : "deplasman";
            return UiActionResult.Ok(
                $"Kadro onaylandı: fikstür #{pending.FixtureId} · {venue} vs {opponent}.");
        }
        catch (Exception ex)
        {
            return UiActionResult.Fail($"Kadro onaylanamadı: {ex.Message}");
        }
    }

    public UiActionResult GenerateJobOffer()
    {
        try
        {
            var handler = Host.ManagerModule.GenerateJobOffer
                ?? throw new InvalidOperationException("İş teklifi servisi bağlı değil.");

            var result = handler.Handle(new GenerateUnemployedJobOfferCommand(Guid.NewGuid()));
            if (result.ClubId is not long clubId)
            {
                return UiActionResult.Fail("İş teklifi üretilemedi.");
            }

            var clubName = GetClubDisplayName(clubId);
            return result.WasAlreadyHeld
                ? UiActionResult.Ok($"Bekleyen teklif zaten var: {clubName} (#{result.OfferId}).")
                : UiActionResult.Ok($"Yeni iş teklifi: {clubName} (#{result.OfferId}).");
        }
        catch (ManagerCareerInvariantViolationException ex)
        {
            return UiActionResult.Fail($"İş teklifi alınamadı: {ex.Message}");
        }
        catch (Exception ex)
        {
            return UiActionResult.Fail($"İş teklifi hatası: {ex.Message}");
        }
    }

    public UiActionResult AcceptJobOffer()
    {
        try
        {
            var handler = Host.ManagerModule.AcceptJobOffer
                ?? throw new InvalidOperationException("İş teklifi kabul servisi bağlı değil.");

            var result = handler.Handle(new AcceptPendingJobOfferCommand(Guid.NewGuid()));
            var clubName = GetClubDisplayName(result.ClubId);
            return UiActionResult.Ok($"Teklif kabul edildi — yeni kulüp: {clubName}.");
        }
        catch (ManagerCareerInvariantViolationException ex)
        {
            return UiActionResult.Fail($"Teklif kabul edilemedi: {ex.Message}");
        }
        catch (Exception ex)
        {
            return UiActionResult.Fail($"Teklif kabul hatası: {ex.Message}");
        }
    }

    public ClubTrainingSummaryReadModel GetTrainingSummary() =>
        Host.TrainingModule.Queries.GetManagedClubSummary();

    public UiActionResult SetWeeklyTraining(TrainingIntensity intensity)
    {
        try
        {
            var result = Host.TrainingModule.SetWeeklyPlan.Handle(
                new SetWeeklyTrainingPlanCommand(
                    Guid.NewGuid(),
                    (int)TrainingFocus.General,
                    (int)intensity,
                    (int)RestApproach.Normal));

            var injuryText = result.InjuredSlotCount > 0
                ? $" · sakat {result.InjuredSlotCount}"
                : string.Empty;
            return UiActionResult.Ok(
                $"Antrenman uygulandı ({intensity}): yorgunluk {result.AverageFatigue}, fitness {result.AverageFitness}{injuryText}.");
        }
        catch (TrainingPhysicalStateInvariantViolationException ex)
        {
            return UiActionResult.Fail($"Antrenman uygulanamadı: {ex.Message}");
        }
        catch (Exception ex)
        {
            return UiActionResult.Fail($"Antrenman hatası: {ex.Message}");
        }
    }

    public PlayMatchesUiResult PlayDueMatches()
    {
        try
        {
            var competition = Host.CompetitionModule;
            var world = Host.WorldModule;
            var playHandler = competition.PlayFixtureMatch
                ?? throw new InvalidOperationException("Maç oynatma servisi bağlı değil.");

            var season = competition.Queries.GetCurrentSeason()
                ?? throw new InvalidOperationException("Aktif sezon yok. Önce ligi kur.");

            var currentDay = world.Queries.GetCurrentGameDate().DayNumber;
            var pendingSelection = Host.TeamPreparationModule.SelectionQueries
                .GetNextDueManagedFixture(currentDay);
            if (pendingSelection is not null && !pendingSelection.IsApproved)
            {
                return new PlayMatchesUiResult(
                    false,
                    "Önce kendi maçın için kadroyu onayla (Kadro Onayla).",
                    Array.Empty<string>());
            }

            var dueFixtures = competition.Queries
                .GetSeasonFixtures(season.SeasonId)
                .Where(fixture =>
                    fixture.ScheduledDayNumber <= currentDay
                    && string.Equals(fixture.Status, nameof(FixtureStatus.Planned), StringComparison.Ordinal))
                .ToArray();

            if (dueFixtures.Length == 0)
            {
                return new PlayMatchesUiResult(
                    false,
                    "Oynanacak planlı maç yok. Tarihi ilerlet veya başka bir haftaya bak.",
                    Array.Empty<string>());
            }

            var lines = new List<string>(dueFixtures.Length);
            foreach (var fixture in dueFixtures)
            {
                var result = playHandler.Handle(
                    new PlayFixtureMatchCommand(
                        Guid.NewGuid(),
                        season.SeasonId,
                        fixture.FixtureId,
                        currentDay));

                var home = GetClubDisplayName(fixture.HomeClubId);
                var away = GetClubDisplayName(fixture.AwayClubId);
                lines.Add($"{home} {result.HomeGoals}-{result.AwayGoals} {away}");
            }

            return new PlayMatchesUiResult(
                true,
                $"{lines.Count} maç oynandı (gün {currentDay}).",
                lines);
        }
        catch (TeamPreparationInvariantViolationException ex)
        {
            return new PlayMatchesUiResult(false, $"Kadro engeli: {ex.Message}", Array.Empty<string>());
        }
        catch (Exception ex)
        {
            return new PlayMatchesUiResult(false, $"Maç oynatma hatası: {ex.Message}", Array.Empty<string>());
        }
    }

    public UiActionResult AdvanceDays(int dayCount)
    {
        var world = Host.WorldModule;
        var current = world.Queries.GetCurrentGameDate();
        var result = world.AdvanceSimulationTime.Handle(
            new AdvanceSimulationTimeCommand(Guid.NewGuid(), current.DayNumber + dayCount));

        if (result.WasBlocked)
        {
            return UiActionResult.Fail(FormatBlockers(result.Blockers.Select(b =>
                (b.SourceContext, b.DescriptionCode))));
        }

        var day = Host.WorldModule.Queries.GetCurrentGameDate();
        var declined = Host.PlayerCareerModule.Development.ApplyDueAging(day);
        var agingText = declined > 0 ? $" · yaşlanma: {declined} oyuncu düştü" : string.Empty;
        return UiActionResult.Ok(
            $"Tarih ilerledi: gün {result.PreviousDayNumber} → {result.NewDayNumber}{agingText}.");
    }

    public UiActionResult CompleteSeason()
    {
        try
        {
            var competition = Host.CompetitionModule;
            var season = competition.Queries.GetCurrentSeason()
                ?? throw new InvalidOperationException("Aktif sezon yok.");

            var current = Host.WorldModule.Queries.GetCurrentGameDate();
            competition.CompleteSeason.Handle(
                new CompleteSeasonCommand(Guid.NewGuid(), season.SeasonId, current.DayNumber));
            var declined = Host.PlayerCareerModule.Development.ApplyDueAging(current);

            var agingText = declined > 0 ? $" · yaşlanma: {declined} oyuncu düştü" : string.Empty;
            return UiActionResult.Ok($"Sezon #{season.SeasonId} kapatıldı{agingText}.");
        }
        catch (Exception ex)
        {
            return UiActionResult.Fail($"Sezon kapatılamadı: {ex.Message}");
        }
    }

    public UiActionResult ArchiveSeason()
    {
        try
        {
            var competition = Host.CompetitionModule;
            var season = competition.Queries.GetCurrentSeason()
                ?? throw new InvalidOperationException("Aktif sezon yok.");

            var currentDay = Host.WorldModule.Queries.GetCurrentGameDate().DayNumber;
            competition.ArchiveSeason.Handle(
                new ArchiveSeasonCommand(Guid.NewGuid(), season.SeasonId, currentDay));

            return UiActionResult.Ok($"Sezon #{season.SeasonId} arşivlendi.");
        }
        catch (Exception ex)
        {
            return UiActionResult.Fail($"Sezon arşivlenemedi: {ex.Message}");
        }
    }

    public UiActionResult StartNewSeason()
    {
        try
        {
            var competition = Host.CompetitionModule;
            var world = Host.WorldModule;
            var currentDay = world.Queries.GetCurrentGameDate().DayNumber;
            var nextSeasonId = competition.Queries.GetCurrentSeason()?.SeasonId + 1 ?? DefaultSeasonId;

            competition.CreateSeason.Handle(
                new CreateSeasonCommand(Guid.NewGuid(), nextSeasonId, currentDay));

            for (var clubId = 1L; clubId <= CompetitionMvpConstraints.LeagueTeamCount; clubId++)
            {
                competition.RegisterSeasonParticipant.Handle(
                    new RegisterSeasonParticipantCommand(Guid.NewGuid(), nextSeasonId, clubId));
            }

            competition.StartSeason.Handle(
                new StartSeasonCommand(Guid.NewGuid(), nextSeasonId, currentDay));

            competition.PlanLeagueFixtures.Handle(
                new PlanLeagueFixturesCommand(
                    Guid.NewGuid(),
                    nextSeasonId,
                    ComputeFirstMatchdayDayNumber(currentDay),
                    StartingFixtureId: 1));

            return UiActionResult.Ok($"Yeni sezon #{nextSeasonId} başlatıldı.");
        }
        catch (Exception ex)
        {
            return UiActionResult.Fail($"Yeni sezon başlatılamadı: {ex.Message}");
        }
    }

    public UiActionResult SaveGame()
    {
        try
        {
            var result = Host.GameSession.Save(Host.DefaultSavePath);
            return UiActionResult.Ok(
                $"Kayıt tamam: gün {result.SavedDayNumber}, {result.SavedFixtureCount} maç.\n{result.SavePath}");
        }
        catch (Exception ex)
        {
            return UiActionResult.Fail($"Kayıt hatası: {ex.Message}");
        }
    }

    public UiActionResult LoadGame()
    {
        try
        {
            if (!File.Exists(Host.DefaultSavePath))
            {
                return UiActionResult.Fail("Kayıt dosyası bulunamadı.");
            }

            var result = Host.GameSession.Load(Host.DefaultSavePath);
            var migrateNote = result.WasMigrated ? " (şema migrate edildi)" : string.Empty;
            return UiActionResult.Ok(
                $"Yükleme tamam{migrateNote}: gün {result.LoadedDayNumber}, {result.LoadedFixtureCount} maç.");
        }
        catch (Exception ex)
        {
            return UiActionResult.Fail($"Yükleme hatası: {ex.Message}");
        }
    }

    public UiActionResult OpenPlanningPeriod()
    {
        try
        {
            var current = Host.WorldModule.Queries.GetCurrentGameDate();
            var result = Host.WorldModule.OpenPlanningPeriod.Handle(
                new OpenPlanningPeriodCommand(
                    Guid.NewGuid(),
                    _nextPlanningPeriodId,
                    current.DayNumber));

            _nextPlanningPeriodId++;
            return UiActionResult.Ok(
                $"Planlama dönemi açıldı: #{result.PlanningPeriodId} ({result.Status}).");
        }
        catch (Exception ex)
        {
            return UiActionResult.Fail($"Planlama dönemi açılamadı: {ex.Message}");
        }
    }

    public UiActionResult CompletePlanningPeriod()
    {
        try
        {
            var result = Host.WorldModule.CompletePlanningPeriod.Handle(
                new CompletePlanningPeriodCommand(Guid.NewGuid()));

            return UiActionResult.Ok(
                $"Planlama dönemi tamamlandı: #{result.PlanningPeriodId} (gün {result.CompletedAtDayNumber}).");
        }
        catch (Exception ex)
        {
            return UiActionResult.Fail($"Planlama dönemi tamamlanamadı: {ex.Message}");
        }
    }

    public string GetClubDisplayName(long clubId) =>
        Host.ClubModule.Queries.GetClub(clubId)?.DisplayName ?? $"Kulüp {clubId}";

    public string FormatActiveBlockerSummary()
    {
        var eligibility = Host.WorldModule.Queries.GetTimeAdvanceEligibility();
        if (eligibility.CanAdvance)
        {
            return "İlerleme engeli yok.";
        }

        return FormatBlockers(eligibility.Blockers.Select(b => (b.SourceContext, b.DescriptionCode)));
    }

    public static string FormatBlockers(IEnumerable<(string SourceContext, string DescriptionCode)> blockers)
    {
        var parts = blockers.Select(b => DescribeBlocker(b.SourceContext, b.DescriptionCode));
        return "İlerleme engellendi: " + string.Join(" · ", parts);
    }

    public static string DescribeBlocker(string sourceContext, string descriptionCode) =>
        descriptionCode switch
        {
            "UnplayedFixturesDue" =>
                "Oynanmamış maçlar var — önce 'Bugünün maçlarını oyna'.",
            _ => $"{sourceContext}/{descriptionCode}",
        };

    public static int ComputeFirstMatchdayDayNumber(int currentDayNumber) =>
        GameDate.FromDayNumber(currentDayNumber).AddDays(30).DayNumber;

    /// <summary>
    /// Smoke test için lig kurulumunu uygular (TimeControlScreen self-check ile aynı sıra).
    /// </summary>
    public static void SetupLeagueSeasonForSelfCheck(
        CompetitionModule competition,
        WorldCalendarModule world)
    {
        var currentDay = world.Queries.GetCurrentGameDate().DayNumber;

        if (competition.Queries.GetCurrentSeason() is null)
        {
            competition.CreateSeason.Handle(
                new CreateSeasonCommand(Guid.NewGuid(), DefaultSeasonId, currentDay));
        }

        for (var clubId = 1L; clubId <= CompetitionMvpConstraints.LeagueTeamCount; clubId++)
        {
            competition.RegisterSeasonParticipant.Handle(
                new RegisterSeasonParticipantCommand(Guid.NewGuid(), DefaultSeasonId, clubId));
        }

        competition.StartSeason.Handle(
            new StartSeasonCommand(Guid.NewGuid(), DefaultSeasonId, currentDay));

        competition.PlanLeagueFixtures.Handle(
            new PlanLeagueFixturesCommand(
                Guid.NewGuid(),
                DefaultSeasonId,
                ComputeFirstMatchdayDayNumber(currentDay),
                StartingFixtureId: 1));
    }
}

public sealed record UiActionResult(bool Succeeded, string Message)
{
    public static UiActionResult Ok(string message) => new(true, message);

    public static UiActionResult Fail(string message) => new(false, message);
}

public sealed record PlayMatchesUiResult(
    bool Succeeded,
    string Message,
    IReadOnlyList<string> MatchLines);
