using FootballCareerSimulator.Application.CareerHub.Queries;
using FootballCareerSimulator.Application.Competition.Commands;
using FootballCareerSimulator.Application.Competition.Composition;
using FootballCareerSimulator.Application.Competition.Queries;
using FootballCareerSimulator.Application.ManagerCareer.Commands;
using FootballCareerSimulator.Application.TeamPreparation.Commands;
using FootballCareerSimulator.Application.TeamPreparation.Queries;
using FootballCareerSimulator.Application.TrainingPhysicalState.Commands;
using FootballCareerSimulator.Application.TrainingPhysicalState.Queries;
using FootballCareerSimulator.Application.Transfer.Queries;
using FootballCareerSimulator.Application.WorldCalendar.Commands;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Application.Interaction.Queries;
using FootballCareerSimulator.Application.WorldCalendar.Queries;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.ContractRegistration;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Interaction;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.TrainingPhysicalState;
using FootballCareerSimulator.Domain.Transfer;
using FootballCareerSimulator.Domain.Match;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Simulation.Match;
using FootballCareerSimulator.Simulation.TeamPreparation;
using FootballCareerSimulator.Simulation.TrainingPhysicalState;

namespace FootballCareerSimulator.Presentation;

/// <summary>
/// Kariyer hub'ının Application komutlarını tek yerden çağırır; UI'ye Türkçe sonuç mesajı döner.
/// </summary>
public sealed class CareerSessionController
{
    public const long DefaultSeasonId = 1;

    private long _nextPlanningPeriodId = 1;
    private TrainingFocus _trainingFocus = TrainingFocus.General;
    private TrainingIntensity _trainingIntensity = TrainingIntensity.Medium;
    private RestApproach _trainingRest = RestApproach.Normal;
    private IReadOnlyList<string>? _pendingInjuryClearedNames;
    private IReadOnlyList<string>? _pendingCleanXiBridgeNames;
    private string? _pendingWeekStoryClosure;
    private bool _weekStoryClosureDismissOnNextAdvance;
    private readonly List<MatchupPlanNotebookEntry> _matchupPlanHistory = [];

    public CareerSessionController(CareerPresentationHost host)
    {
        Host = host ?? throw new ArgumentNullException(nameof(host));
    }

    public CareerPresentationHost Host { get; }

    /// <summary>Son başarılı LoadGame sonrası nabız doğrulama özeti (menü/Dosya).</summary>
    public CareerResumeDigest? LastCareerResume { get; private set; }

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
                for (var participantClubId = season.ParticipantCount + 1L;
                     participantClubId <= CompetitionMvpConstraints.LeagueTeamCount;
                     participantClubId++)
                {
                    competition.RegisterSeasonParticipant.Handle(
                        new RegisterSeasonParticipantCommand(
                            Guid.NewGuid(),
                            DefaultSeasonId,
                            participantClubId));
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

            if (Host.ManagerModule.Queries.GetCareer().EmployedClubId is long employedClubId)
            {
                var day = Host.WorldModule.TimelineStore.Timeline.CurrentDate;
                var id = new Domain.Shared.ClubId(employedClubId);
                Host.PlayerCareerModule.Development.EnsureClub(
                    id,
                    Host.WorldModule.TimelineStore.Timeline.RootSeed,
                    day);
                Host.TeamPreparationModule.ClubSquad?.SyncFromActiveContracts(id, day);
                Host.TeamPreparationModule.TacticPlans.EnsureDefault(id, day);
            }

            return UiActionResult.Ok(
                $"Lig hazır: {season.ParticipantCount} takım, {LeagueMatchweeks(season.ParticipantCount)} haftalık sezon.");
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
                var alreadyOpponent = GetClubDisplayName(pending.OpponentClubId);
                var alreadyVenue = pending.IsHome ? "ev sahibi" : "deplasman";
                return UiActionResult.Ok(
                    $"Kadro zaten onaylı: {alreadyVenue} vs {alreadyOpponent}"
                    + $" ({GameDate.ToDisplayDateString(pending.ScheduledDayNumber)}).");
            }

            var clubId = Host.ManagerModule.Queries.GetCareer().EmployedClubId
                ?? throw new InvalidOperationException("Menajer kulübü yok.");

            var moodBefore = BuildWeekMood();
            var approval = Host.TeamPreparationModule.ApproveDefaultSelection.Handle(
                new ApproveDefaultMatchSelectionCommand(
                    Guid.NewGuid(),
                    pending.FixtureId,
                    clubId));

            var opponent = GetClubDisplayName(pending.OpponentClubId);
            var venue = pending.IsHome ? "ev sahibi" : "deplasman";
            var swapNote = string.IsNullOrWhiteSpace(approval.AutoSwapSummary)
                ? string.Empty
                : $" · {approval.AutoSwapSummary}";
            var message =
                $"Kadro onaylandı: {venue} vs {opponent}{swapNote}.";

            var tempoOffice = PostMatchOfficeDigest.AfterMoodTempoShift(
                moodBefore.MoodCode,
                BuildWeekMood(),
                nextCtaLabel: "Maç Gününe Git");
            if (tempoOffice is not null)
            {
                return UiActionResult.Ok(
                    message + "\n" + tempoOffice.Headline,
                    narrativeBridgeLine: tempoOffice.ToDisplayText(),
                    nextFocusCode: tempoOffice.NextFocusCode);
            }

            return UiActionResult.Ok(message);
        }
        catch (Exception ex)
        {
            return UiActionResult.Fail($"Kadro onaylanamadı: {ex.Message}");
        }
    }

    public UiActionResult SwapLastStarterWithFirstBenchForNextDueMatch()
    {
        var board = BuildSquadSelectionBoard();
        if (!board.HasMatch || board.StartingXi.Count == 0 || board.Bench.Count == 0)
        {
            return UiActionResult.Fail("Değiştirilebilir ilk 11 ve yedek bulunamadı.");
        }

        return SwapStarterWithBenchForNextDueMatch(
            board.StartingXi[^1].SlotIndex,
            board.Bench[0].SlotIndex);
    }

    public UiActionResult SwapStarterWithBenchForNextDueMatch(
        int starterSlotIndex,
        int benchSlotIndex)
    {
        try
        {
            var currentDay = Host.WorldModule.Queries.GetCurrentGameDate().DayNumber;
            var pending = Host.TeamPreparationModule.SelectionQueries.GetNextDueManagedFixture(currentDay)
                ?? throw new InvalidOperationException("Değiştirilecek vadesi gelmiş maç yok.");

            var clubId = Host.ManagerModule.Queries.GetCareer().EmployedClubId
                ?? throw new InvalidOperationException("Menajer kulübü yok.");

            var board = BuildSquadSelectionBoard();
            var startingIndex = board.StartingXi
                .Select((player, index) => (player.SlotIndex, Index: index))
                .FirstOrDefault(item => item.SlotIndex == starterSlotIndex)
                .Index;
            var benchIndex = board.Bench
                .Select((player, index) => (player.SlotIndex, Index: index))
                .FirstOrDefault(item => item.SlotIndex == benchSlotIndex)
                .Index;
            if (startingIndex < 0
                || benchIndex < 0
                || board.StartingXi.ElementAtOrDefault(startingIndex)?.SlotIndex != starterSlotIndex
                || board.Bench.ElementAtOrDefault(benchIndex)?.SlotIndex != benchSlotIndex)
            {
                throw new TeamPreparationInvariantViolationException(
                    "Seçilen oyuncular güncel ilk 11 ve yedek listesinde bulunamadı.");
            }

            var result = Host.TeamPreparationModule.SwapStarterWithBench.Handle(
                new SwapStarterWithBenchCommand(
                    Guid.NewGuid(),
                    pending.FixtureId,
                    clubId,
                    StartingIndex: startingIndex,
                    BenchIndex: benchIndex));

            var summary = string.IsNullOrWhiteSpace(result.SwapSummary)
                ? $"slot {result.OutSlotIndex} ↔ {result.InSlotIndex}"
                : result.SwapSummary;
            return UiActionResult.Ok(
                $"Değişiklik: {summary}.",
                narrativeBridgeLine: result.HalfTimeBridgeLine);
        }
        catch (TeamPreparationInvariantViolationException ex)
        {
            return UiActionResult.Fail($"Kadro değiştirilemedi: {ex.Message}");
        }
        catch (Exception ex)
        {
            return UiActionResult.Fail($"Kadro değişim hatası: {ex.Message}");
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

    public UiActionResult PromoteOverflowPlayerToSquad()
    {
        try
        {
            var clubId = Host.ManagerModule.Queries.GetCareer().EmployedClubId
                ?? throw new InvalidOperationException("Menajer kulübü yok.");
            var day = Host.WorldModule.TimelineStore.Timeline.CurrentDate;
            var id = new Domain.Shared.ClubId(clubId);
            var squad = Host.TeamPreparationModule.ClubSquad
                ?? throw new InvalidOperationException("Kadro servisi yok.");

            var result = squad.PromoteFirstOverflowToSquad(id, day);
            return UiActionResult.Ok(
                $"Taşan kadroya alındı: #{result.PromotedPlayerId} → slot {result.SlotIndex}."
                + $"\nKadro dışı düşen: #{result.DemotedPlayerId} (sözleşmesi duruyor)."
                + "\nÖneri: Sıradaki Maç'ta XI'yi kontrol et.");
        }
        catch (TeamPreparationInvariantViolationException ex)
        {
            return UiActionResult.Fail($"Kadroya alınamadı: {ex.Message}");
        }
        catch (Exception ex)
        {
            return UiActionResult.Fail($"Kadroya alma hatası: {ex.Message}");
        }
    }

    public UiActionResult ReleaseToFreeSquadCapacity()
    {
        try
        {
            var clubId = Host.ManagerModule.Queries.GetCareer().EmployedClubId
                ?? throw new InvalidOperationException("Menajer kulübü yok.");
            var day = Host.WorldModule.TimelineStore.Timeline.CurrentDate;
            var id = new Domain.Shared.ClubId(clubId);
            var squad = Host.TeamPreparationModule.ClubSquad
                ?? throw new InvalidOperationException("Kadro servisi yok.");

            var candidateId = squad.SuggestReleaseCandidatePlayerId(id, day)
                ?? throw new InvalidOperationException(
                    "Yer açılacak oyuncu yok — kadro zaten açık.");

            var before = squad.GetCapacityDigest(id, day);
            var wasOverflow = before.OverflowPlayerIds.Contains(candidateId);
            var result = Host.ContractModule.Registration.ReleasePlayerFromClub(
                new PlayerId(candidateId),
                id,
                day,
                wasOverflow);
            squad.SyncFromActiveContracts(id, day);
            var after = squad.GetCapacityDigest(id, day);

            var kind = result.WasOverflow ? "Taşan serbest bırakıldı" : "Kadrodan serbest bırakıldı";
            var releasedName = GetPlayerDisplayName(result.PlayerId);
            return UiActionResult.Ok(
                $"Yer Açıldı\n{kind}: {releasedName}."
                + $"\n· Aktif sözleşme {result.RemainingActiveContracts}/{ClubSquad.MaxMembers}"
                + (after.IsOverCapacity
                    ? $" · hâlâ {after.OverflowPlayerIds.Count} taşan"
                    : " · kapasite rahatladı")
                + "\nÖneri: Serbesti Geri İmzala veya yeni imza artık mümkün olabilir.");
        }
        catch (ContractRegistrationInvariantViolationException ex)
        {
            return UiActionResult.Fail($"Serbest bırakılamadı: {ex.Message}");
        }
        catch (Exception ex)
        {
            return UiActionResult.Fail($"Yer açma hatası: {ex.Message}");
        }
    }

    public UiActionResult SellFringePlayerFromManagedClub()
    {
        var candidateId = SuggestSaleCandidatePlayerId();
        if (candidateId is null)
        {
            return UiActionResult.Fail("Satılacak kenar oyuncu yok — kadro çok ince.");
        }

        return SellManagedClubPlayer(candidateId.Value);
    }

    public UiActionResult SellManagedClubPlayer(long playerId)
    {
        try
        {
            var clubId = Host.ManagerModule.Queries.GetCareer().EmployedClubId
                ?? throw new InvalidOperationException("Menajer kulübü yok.");
            var day = Host.WorldModule.TimelineStore.Timeline.CurrentDate;
            var id = new Domain.Shared.ClubId(clubId);
            var squad = Host.TeamPreparationModule.ClubSquad
                ?? throw new InvalidOperationException("Kadro servisi yok.");

            var candidateId = playerId;
            if (candidateId <= 0)
            {
                throw new InvalidOperationException("Satılacak oyuncu seçilmedi.");
            }

            _ = Host.TransferModule.Needs.DeclarePlayerExitRequest(
                id,
                new PlayerId(candidateId),
                day);

            var seed = Host.WorldModule.TimelineStore.Timeline.RootSeed;
            var sale = Host.TransferModule.AiSimulation.TrySellManagedClubPlayer(
                id,
                new PlayerId(candidateId),
                day,
                seed);

            if (!sale.Sold)
            {
                return UiActionResult.Fail(
                    $"Satışa Çıkış\n{GetPlayerDisplayName(candidateId)} listelendi."
                    + $"\n· {sale.Message}");
            }

            var after = squad.GetCapacityDigest(id, day);
            var buyerName = sale.BuyingClubId is long buyer
                ? GetClubDisplayName(buyer)
                : "—";
            var soldPlayerId = sale.PlayerId
                ?? throw new InvalidOperationException("Satış sonucu oyuncu kimliği eksik.");
            var transferFee = sale.TransferFee
                ?? throw new InvalidOperationException("Satış sonucu bedel eksik.");
            var managerId = Host.ManagerModule.Queries.GetCareer().ManagerId;
            var aftermath = ManagedSaleAftermathDigest.Compose(
                soldPlayerId,
                managerId,
                buyerName,
                transferFee,
                after.ActiveContractCount,
                ClubSquad.MaxMembers,
                Host.TransferModule.NeedStore.Needs,
                Host.SocialContinuityModule.PromiseStore.Promises,
                Host.SocialContinuityModule.RelationshipStore.Relationships,
                Host.SocialContinuityModule.MemoryStore.Memories);

            return UiActionResult.Ok(aftermath.ToStatusMessage());
        }
        catch (TransferInvariantViolationException ex)
        {
            return UiActionResult.Fail($"Satış yapılamadı: {ex.Message}");
        }
        catch (Exception ex)
        {
            return UiActionResult.Fail($"Satış hatası: {ex.Message}");
        }
    }

    public UiActionResult SignNextFreeAgentToManagedClub()
    {
        try
        {
            var signable = Host.ContractModule.Queries.GetNextSignableFreeAgentForManagedClub()
                ?? throw new InvalidOperationException("İmzalanacak serbest oyuncu yok.");

            var clubId = Host.ManagerModule.Queries.GetCareer().EmployedClubId
                ?? throw new InvalidOperationException("Menajer kulübü yok.");

            var day = Host.WorldModule.TimelineStore.Timeline.CurrentDate;
            var id = new Domain.Shared.ClubId(clubId);
            if (Host.TeamPreparationModule.ClubSquad is { } squadService
                && !squadService.HasFreeSquadCapacity(id, day))
            {
                return UiActionResult.Fail(
                    $"Kadro dolu ({ClubSquad.MaxMembers}/{ClubSquad.MaxMembers}) — önce yer aç veya sözleşme bitmesini bekle.");
            }

            var result = Host.ContractModule.Registration.SignFreeAgentToLastClub(
                new PlayerId(signable.PlayerId),
                id,
                day);
            Host.TeamPreparationModule.ClubSquad?.SyncFromActiveContracts(id, day);

            return UiActionResult.Ok(
                $"Serbest oyuncu imzalandı: {GetPlayerDisplayName(result.PlayerId)}"
                + $" · ücret {result.WeeklyWage}"
                + $" · sözleşme bitişi {GameDate.ToDisplayDateString(result.EndDayNumber)}.");
        }
        catch (ContractRegistrationInvariantViolationException ex)
        {
            return UiActionResult.Fail($"Serbest imza başarısız: {ex.Message}");
        }
        catch (Exception ex)
        {
            return UiActionResult.Fail($"Serbest imza hatası: {ex.Message}");
        }
    }

    public UiActionResult PromiseStartingOpportunityToOldestSquadPlayer()
    {
        var candidateId = GetManagedSquadMembers()
            .Where(member => member.SlotIndex >= Domain.TeamPreparation.MatchSelection.StartingXiSize)
            .OrderBy(member => member.SlotIndex)
            .Select(member => (long?)member.PlayerId.Value)
            .FirstOrDefault()
            ?? GetManagedSquadMembers()
                .OrderByDescending(member => member.SlotIndex)
                .Select(member => (long?)member.PlayerId.Value)
                .FirstOrDefault();
        return candidateId is long playerId
            ? PromiseStartingOpportunityToPlayer(playerId)
            : UiActionResult.Fail("Söz verilemedi: kadroda oyuncu yok.");
    }

    public UiActionResult PromiseStartingOpportunityToPlayer(long playerId)
    {
        try
        {
            var career = Host.ManagerModule.Store.Career;
            var clubId = career.ActiveEmployment?.ClubId
                ?? throw new InvalidOperationException("Menajer kulübü yok.");
            var day = Host.WorldModule.TimelineStore.Timeline.CurrentDate;
            var member = GetManagedSquadMember(playerId);

            var promise = Host.SocialContinuityModule.StartingOpportunity.Create(
                career.ManagerId,
                member.PlayerId,
                clubId,
                targetStarts: 3,
                deadlineOn: day.AddDays(30),
                createdOn: day);

            var tensionHint = member.SlotIndex >= Domain.TeamPreparation.MatchSelection.StartingXiSize
                ? " · dikkat: varsayılan kadroda yedek/dışarıda"
                : string.Empty;

            return UiActionResult.Ok(
                $"İlk 11 sözü verildi: {GetPlayerDisplayName(member.PlayerId.Value)}"
                + $" · slot {member.SlotIndex}"
                + $" · hedef {promise.TargetStarts}"
                + $" · son gün {GameDate.ToDisplayDateString(promise.DeadlineOn.DayNumber)}{tensionHint}.");
        }
        catch (SocialContinuityInvariantViolationException ex)
        {
            return UiActionResult.Fail($"Söz verilemedi: {ex.Message}");
        }
        catch (Exception ex)
        {
            return UiActionResult.Fail($"Söz hatası: {ex.Message}");
        }
    }

    public UiActionResult PromisePlayingTimeToOldestSquadPlayer()
    {
        var candidateId = GetManagedSquadMembers()
            .OrderBy(member => member.SlotIndex)
            .Select(member => (long?)member.PlayerId.Value)
            .FirstOrDefault();
        return candidateId is long playerId
            ? PromisePlayingTimeToPlayer(playerId)
            : UiActionResult.Fail("Söz verilemedi: kadroda oyuncu yok.");
    }

    public UiActionResult PromisePlayingTimeToPlayer(long playerId)
    {
        try
        {
            var career = Host.ManagerModule.Store.Career;
            var clubId = career.ActiveEmployment?.ClubId
                ?? throw new InvalidOperationException("Menajer kulübü yok.");
            var day = Host.WorldModule.TimelineStore.Timeline.CurrentDate;
            var member = GetManagedSquadMember(playerId);

            var promise = Host.SocialContinuityModule.PlayingTime.Create(
                career.ManagerId,
                member.PlayerId,
                clubId,
                targetAppearances: 5,
                deadlineOn: day.AddDays(45),
                createdOn: day);

            return UiActionResult.Ok(
                $"Oyun süresi sözü verildi: {GetPlayerDisplayName(member.PlayerId.Value)}"
                + $" · hedef {promise.TargetStarts} maç günü"
                + $" · son gün {GameDate.ToDisplayDateString(promise.DeadlineOn.DayNumber)}.");
        }
        catch (SocialContinuityInvariantViolationException ex)
        {
            return UiActionResult.Fail($"Söz verilemedi: {ex.Message}");
        }
        catch (Exception ex)
        {
            return UiActionResult.Fail($"Söz hatası: {ex.Message}");
        }
    }

    public UiActionResult OpenPlayingTimeDecisionForOldestSquadPlayer() =>
        OpenDecisionForOldestSquadPlayer(
            (playerId, day) => Host.InteractionModule.Decisions.OpenPlayingTimeRequest(playerId, day),
            "forma süresi talebi");

    public UiActionResult OpenPlayingTimeDecisionForPlayer(long playerId) =>
        OpenDecisionForPlayer(
            playerId,
            (id, day) => Host.InteractionModule.Decisions.OpenPlayingTimeRequest(id, day),
            "forma süresi talebi");

    public UiActionResult OpenStartingOpportunityDecisionForOldestSquadPlayer() =>
        OpenDecisionForOldestSquadPlayer(
            (playerId, day) => Host.InteractionModule.Decisions.OpenStartingOpportunityRequest(playerId, day),
            "ilk 11 fırsatı talebi");

    public UiActionResult OpenStartingOpportunityDecisionForPlayer(long playerId) =>
        OpenDecisionForPlayer(
            playerId,
            (id, day) => Host.InteractionModule.Decisions.OpenStartingOpportunityRequest(id, day),
            "ilk 11 fırsatı talebi");

    public UiActionResult OpenTransferDecisionForOldestSquadPlayer() =>
        OpenDecisionForOldestSquadPlayer(
            (playerId, day) => Host.InteractionModule.Decisions.OpenTransferRequest(playerId, day),
            "transfer isteği");

    public UiActionResult OpenTransferDecisionForPlayer(long playerId) =>
        OpenDecisionForPlayer(
            playerId,
            (id, day) => Host.InteractionModule.Decisions.OpenTransferRequest(id, day),
            "transfer isteği");

    public UiActionResult OpenDisciplineDecisionForOldestSquadPlayer() =>
        OpenDecisionForOldestSquadPlayer(
            (playerId, day) => Host.InteractionModule.Decisions.OpenDisciplineRequest(playerId, day),
            "disiplin görüşmesi");

    public UiActionResult OpenDisciplineDecisionForPlayer(long playerId) =>
        OpenDecisionForPlayer(
            playerId,
            (id, day) => Host.InteractionModule.Decisions.OpenDisciplineRequest(id, day),
            "disiplin görüşmesi");

    public UiActionResult OpenBoardDemandDecision()
    {
        try
        {
            var day = Host.WorldModule.TimelineStore.Timeline.CurrentDate;
            var request = Host.InteractionModule.Decisions.OpenBoardDemandRequest(day);
            return UiActionResult.Ok(
                $"Yönetim talebi açıldı · son gün {GameDate.ToDisplayDateString(request.DeadlineOn.DayNumber)}.");
        }
        catch (InteractionInvariantViolationException ex)
        {
            return UiActionResult.Fail($"Yönetim talebi açılamadı: {ex.Message}");
        }
        catch (Exception ex)
        {
            return UiActionResult.Fail($"Yönetim talebi hatası: {ex.Message}");
        }
    }

    public UiActionResult OpenPressQuestionDecisionForOldestSquadPlayer() =>
        OpenDecisionForOldestSquadPlayer(
            (playerId, day) => Host.InteractionModule.Decisions.OpenPressQuestionRequest(playerId, day),
            "kritik basın sorusu");

    public UiActionResult AnswerOldestPendingDecision(bool grantPromise)
    {
        try
        {
            var pending = Host.InteractionModule.Queries.GetPending(take: 1).OpenRequests.FirstOrDefault()
                ?? throw new InvalidOperationException("Bekleyen karar yok.");
            var day = Host.WorldModule.TimelineStore.Timeline.CurrentDate;
            var dialogue = Host.InteractionModule.DialogueOptions.GetForDecision(
                new DecisionRequestId(pending.DecisionRequestId));
            var generated = grantPromise
                ? dialogue.Options.FirstOrDefault(o =>
                    !string.Equals(o.OptionCode, DecisionRequest.OptionRefuse, StringComparison.Ordinal))
                : dialogue.Options.FirstOrDefault(o =>
                    string.Equals(o.OptionCode, DecisionRequest.OptionRefuse, StringComparison.Ordinal));
            if (generated is null)
            {
                return UiActionResult.Fail("Seçenek diyalog setinde yok.");
            }

            if (!generated.IsEligible)
            {
                return UiActionResult.Fail(
                    generated.IneligibilityReason ?? "Seçenek şu an uygun değil.");
            }

            var causalityBeforeAnswer = Host.InteractionModule.Queries.ExplainCausality(
                new DecisionRequestId(pending.DecisionRequestId));

            Host.InteractionModule.Decisions.Answer(
                new DecisionRequestId(pending.DecisionRequestId),
                generated.OptionCode,
                day);
            return UiActionResult.Ok(
                ComposeDecisionAnswerStatus(pending, generated, causalityBeforeAnswer));
        }
        catch (InteractionInvariantViolationException ex)
        {
            return UiActionResult.Fail($"Karar yanıtlanamadı: {ex.Message}");
        }
        catch (SocialContinuityInvariantViolationException ex)
        {
            return UiActionResult.Fail($"Karar/söz hatası: {ex.Message}");
        }
        catch (Exception ex)
        {
            return UiActionResult.Fail($"Karar hatası: {ex.Message}");
        }
    }

    public UiActionResult AnswerOldestPendingWithOption(string optionCode)
    {
        try
        {
            var pending = Host.InteractionModule.Queries.GetPending(take: 1).OpenRequests.FirstOrDefault()
                ?? throw new InvalidOperationException("Bekleyen karar yok.");
            var day = Host.WorldModule.TimelineStore.Timeline.CurrentDate;
            var dialogue = Host.InteractionModule.DialogueOptions.GetForDecision(
                new DecisionRequestId(pending.DecisionRequestId));
            var generated = dialogue.Options.FirstOrDefault(o =>
                string.Equals(o.OptionCode, optionCode, StringComparison.Ordinal));
            if (generated is null)
            {
                return UiActionResult.Fail("Seçenek diyalog setinde yok.");
            }

            if (!generated.IsEligible)
            {
                return UiActionResult.Fail(
                    generated.IneligibilityReason ?? "Seçenek şu an uygun değil.");
            }

            var causalityBeforeAnswer = Host.InteractionModule.Queries.ExplainCausality(
                new DecisionRequestId(pending.DecisionRequestId));

            Host.InteractionModule.Decisions.Answer(
                new DecisionRequestId(pending.DecisionRequestId),
                generated.OptionCode,
                day);
            return UiActionResult.Ok(
                ComposeDecisionAnswerStatus(pending, generated, causalityBeforeAnswer));
        }
        catch (InteractionInvariantViolationException ex)
        {
            return UiActionResult.Fail($"Karar yanıtlanamadı: {ex.Message}");
        }
        catch (Domain.Discipline.DisciplineInvariantViolationException ex)
        {
            return UiActionResult.Fail($"Disiplin hatası: {ex.Message}");
        }
        catch (SocialContinuityInvariantViolationException ex)
        {
            return UiActionResult.Fail($"Karar/söz hatası: {ex.Message}");
        }
        catch (Exception ex)
        {
            return UiActionResult.Fail($"Karar hatası: {ex.Message}");
        }
    }

    private string ComposeDecisionAnswerStatus(
        DecisionRequestLineReadModel pending,
        DialogueOptionReadModel option,
        string? causalityLine = null)
    {
        var remaining = Host.InteractionModule.Queries.GetPending(take: 1).OpenCount;

        string? nextHint = null;
        if (string.Equals(
                option.OptionCode,
                DecisionRequest.OptionAcknowledgeTransferRequest,
                StringComparison.Ordinal))
        {
            nextHint = $"Sıradaki: Günün Nabzı / Transfer → Satışa Çıkar — {GetPlayerDisplayName(pending.SubjectPlayerId)}";
        }
        else if (string.Equals(option.OptionCode, DecisionRequest.OptionRefuse, StringComparison.Ordinal)
                 && pending.KindName.Contains("Transfer", StringComparison.OrdinalIgnoreCase))
        {
            nextHint = "Oyuncu yönetimi ve ilişki satırına bak — kırgınlık gerçek.";
        }
        else if (string.Equals(
                     option.OptionCode,
                     DecisionRequest.OptionGrantPlayingTimePromise,
                     StringComparison.Ordinal))
        {
            nextHint = causalityLine is not null
                       && (causalityLine.Contains("yedek", StringComparison.OrdinalIgnoreCase)
                           || causalityLine.Contains("kadro dışı", StringComparison.OrdinalIgnoreCase))
                ? $"Sıradaki: Hazırlık / Maç Günü — {GetPlayerDisplayName(pending.SubjectPlayerId)} için XI veya yedek tut."
                : $"Sıradaki: Hazırlık / Maç Günü — {GetPlayerDisplayName(pending.SubjectPlayerId)} sözünü maç görünürlüğüyle ilerlet.";
        }
        else if (string.Equals(option.OptionCode, DecisionRequest.OptionRefuse, StringComparison.Ordinal)
                 && (pending.KindName.Contains("süre", StringComparison.OrdinalIgnoreCase)
                     || pending.KindName.Contains("Forma", StringComparison.OrdinalIgnoreCase))
                 && causalityLine is not null
                 && (causalityLine.Contains("yedek", StringComparison.OrdinalIgnoreCase)
                     || causalityLine.Contains("kadro dışı", StringComparison.OrdinalIgnoreCase)))
        {
            nextHint = "Oyuncu yönetimi — yedek kalma hafızası ve güven satırına bak.";
        }

        var subjectName = pending.SubjectPlayerId > 0
            ? GetPlayerDisplayName(pending.SubjectPlayerId)
            : null;
        return DecisionAnswerNarrative.Compose(
            pending.KindName,
            option.OptionCode,
            option.DisplayText,
            pending.SubjectPlayerId,
            pending.IsHardBlocker,
            remaining,
            nextHint,
            causalityLine,
            subjectName).ToStatusMessage();
    }

    private UiActionResult OpenDecisionForOldestSquadPlayer(
        Func<PlayerId, GameDate, DecisionRequest> open,
        string kindLabel)
    {
        try
        {
            var clubId = Host.ManagerModule.Store.Career.ActiveEmployment?.ClubId
                ?? throw new InvalidOperationException("Menajer kulübü yok.");
            var day = Host.WorldModule.TimelineStore.Timeline.CurrentDate;
            Host.TeamPreparationModule.ClubSquad?.SyncFromActiveContracts(clubId, day);
            var squad = Host.TeamPreparationModule.SquadStore.Get(clubId)
                ?? throw new InvalidOperationException("Kadro yok.");
            var member = squad.Members.OrderBy(m => m.SlotIndex).FirstOrDefault()
                ?? throw new InvalidOperationException("Kadroda oyuncu yok.");

            var request = open(member.PlayerId, day);
            return UiActionResult.Ok(
                $"Karar açıldı: {kindLabel} · {GetPlayerDisplayName(member.PlayerId.Value)}"
                + $" · son gün {GameDate.ToDisplayDateString(request.DeadlineOn.DayNumber)}.");
        }
        catch (InteractionInvariantViolationException ex)
        {
            return UiActionResult.Fail($"Karar açılamadı: {ex.Message}");
        }
        catch (Exception ex)
        {
            return UiActionResult.Fail($"Karar hatası: {ex.Message}");
        }
    }

    public TodayPulseDigest BuildTodayPulse()
    {
        var day = Host.WorldModule.Queries.GetCurrentGameDate().DayNumber;
        var desk = BuildDecisionDeskDigest();
        var match = BuildNextMatchBriefing();
        var prep = BuildPreparationBriefing();
        var league = BuildLeagueWorldBriefing();
        var transfer = BuildTransferDeskBriefing();
        var recoveryPath = BuildInjuryRecoveryPath();
        var weekStory = BuildWeekStory(match, recoveryPath);
        var weekMood = BuildWeekMood(desk, match, prep, league, transfer, weekStory.IsActive);
        return TodayPulseDigest.Compose(
            desk,
            match,
            prep,
            league,
            BuildSquadCapacityDigest(),
            transfer,
            seasonTransitionReady: CanTransitionToNextSeason(),
            seasonArchivePhase: IsSeasonArchivePhase(),
            recoveryPath: recoveryPath,
            weekStory: weekStory,
            weekMood: weekMood,
            dayNumber: day);
    }

    public InjuryRecoveryPathDigest BuildInjuryRecoveryPath()
    {
        var prep = BuildPreparationBriefing();
        var match = BuildNextMatchBriefing();
        var training = GetTrainingSummary();
        return InjuryRecoveryPathDigest.Compose(
            hasInjuryPressure: prep.HasInjuryPressure,
            injuredPlayerNames: prep.InjuredNames,
            isOnRecoveryPlan: training.Focus == (int)TrainingFocus.Recovery,
            hasDueMatch: match.HasMatch,
            isMatchApproved: match.IsReadyToKickOff,
            freshlyRecoveredNames: _pendingInjuryClearedNames);
    }

    public WeekStoryDigest BuildWeekStory(
        PreMatchBriefing? match = null,
        InjuryRecoveryPathDigest? recoveryPath = null) =>
        WeekStoryDigest.Compose(
            recoveryPath ?? BuildInjuryRecoveryPath(),
            match ?? BuildNextMatchBriefing(),
            closedArcVerdictBeat: _pendingWeekStoryClosure);

    public WeekMoodDigest BuildWeekMood(
        DecisionDeskDigest? desk = null,
        PreMatchBriefing? match = null,
        PreparationBriefing? prep = null,
        LeagueWorldBriefing? league = null,
        TransferDeskBriefing? transfer = null,
        bool? weekStoryActive = null)
    {
        var day = Host.WorldModule.Queries.GetCurrentGameDate().DayNumber;
        desk ??= BuildDecisionDeskDigest();
        match ??= BuildNextMatchBriefing();
        prep ??= BuildPreparationBriefing();
        league ??= BuildLeagueWorldBriefing();
        transfer ??= BuildTransferDeskBriefing();
        var storyActive = weekStoryActive ?? BuildWeekStory(match).IsActive;
        var dressingRoomEcho = BuildDressingRoomEcho();
        return WeekMoodDigest.Compose(
            desk,
            match,
            prep,
            league,
            transfer,
            storyActive,
            dressingRoomEcho?.MomentumCode,
            dressingRoomEcho?.MomentumLength ?? 0);
    }

    public DecisionDeskDigest BuildDecisionDeskDigest()
    {
        var day = Host.WorldModule.Queries.GetCurrentGameDate().DayNumber;
        var pending = Host.InteractionModule.Queries.GetPending(take: 5);
        string? causality = null;
        string? subjectPlayerName = null;
        if (pending.OpenRequests.Count > 0)
        {
            var first = pending.OpenRequests[0];
            causality = Host.InteractionModule.Queries.ExplainCausality(
                new Domain.Interaction.DecisionRequestId(first.DecisionRequestId));
            if (first.SubjectPlayerId > 0)
            {
                subjectPlayerName = GetPlayerDisplayName(first.SubjectPlayerId);
            }
        }

        return DecisionDeskDigest.Compose(pending, day, causality, subjectPlayerName);
    }

    public PreMatchBriefing BuildNextMatchBriefing()
    {
        var currentDay = Host.WorldModule.Queries.GetCurrentGameDate().DayNumber;
        var pending = Host.TeamPreparationModule.SelectionQueries
            .GetNextDueManagedFixture(currentDay);
        return ComposePreMatchBriefing(currentDay, pending);
    }

    private UiActionResult OpenDecisionForPlayer(
        long playerId,
        Func<PlayerId, GameDate, DecisionRequest> open,
        string kindLabel)
    {
        try
        {
            var member = GetManagedSquadMember(playerId);
            var day = Host.WorldModule.TimelineStore.Timeline.CurrentDate;
            var request = open(member.PlayerId, day);
            return UiActionResult.Ok(
                $"Karar açıldı: {kindLabel} · {GetPlayerDisplayName(member.PlayerId.Value)}"
                + $" · son gün {GameDate.ToDisplayDateString(request.DeadlineOn.DayNumber)}.");
        }
        catch (InteractionInvariantViolationException ex)
        {
            return UiActionResult.Fail($"Karar açılamadı: {ex.Message}");
        }
        catch (Exception ex)
        {
            return UiActionResult.Fail($"Karar hatası: {ex.Message}");
        }
    }

    private IReadOnlyList<SquadMember> GetManagedSquadMembers()
    {
        var clubId = Host.ManagerModule.Store.Career.ActiveEmployment?.ClubId;
        if (clubId is null)
        {
            return Array.Empty<SquadMember>();
        }

        var day = Host.WorldModule.TimelineStore.Timeline.CurrentDate;
        Host.TeamPreparationModule.ClubSquad?.SyncFromActiveContracts(clubId.Value, day);
        return Host.TeamPreparationModule.SquadStore.Get(clubId.Value)?.Members
            ?? Array.Empty<SquadMember>();
    }

    private SquadMember GetManagedSquadMember(long playerId) =>
        GetManagedSquadMembers().FirstOrDefault(member => member.PlayerId.Value == playerId)
        ?? throw new InvalidOperationException("Seçili oyuncu yönetilen kulübün A takımında değil.");

    public string GetPlayerDisplayName(long playerId) =>
        DescribeManagedPlayer(playerId).Name ?? "kenar oyuncu";

    private (string? Name, string? Detail) DescribeManagedPlayer(long? playerId)
    {
        if (playerId is not long id)
        {
            return (null, null);
        }

        var player = BuildPlayerManagementDigest().Players
            .FirstOrDefault(candidate => candidate.PlayerId == id);
        return player is null
            ? (null, null)
            : (player.DisplayName, player.ToSaleLabel());
    }

    private static int LeagueMatchweeks(int participantCount) =>
        participantCount > 1
            ? (participantCount - 1) * 2
            : CompetitionMvpConstraints.LeagueMatchesPerTeam;

    public TechnicalDirectorNotebookDigest BuildTechnicalDirectorNotebook() =>
        TechnicalDirectorNotebookDigest.Compose(_matchupPlanHistory);

    public RepeatedPatternWarningDigest BuildRepeatedPatternWarning() =>
        RepeatedPatternWarningDigest.Compose(BuildMatchupPlan(), _matchupPlanHistory);

    public AlternativePlanPrescriptionDigest BuildAlternativePlanPrescription()
    {
        var plan = BuildMatchupPlan();
        var warning = RepeatedPatternWarningDigest.Compose(plan, _matchupPlanHistory);
        return AlternativePlanPrescriptionDigest.Compose(plan, warning);
    }

    public UiActionResult ApplyAlternativePlanPrescription()
    {
        try
        {
            var prescription = BuildAlternativePlanPrescription();
            if (!prescription.HasPrescription
                || prescription.SuggestedFormation is not Formation formation
                || prescription.SuggestedApproach is not TacticalApproach approach)
            {
                return UiActionResult.Fail("Uygulanabilir alternatif plan yok.");
            }

            var clubId = Host.ManagerModule.Queries.GetCareer().EmployedClubId
                ?? throw new InvalidOperationException("Menajer kulübü yok.");
            var day = Host.WorldModule.TimelineStore.Timeline.CurrentDate;
            Host.TeamPreparationModule.TacticPlans.SetPlan(
                new ClubId(clubId),
                formation,
                approach,
                day);

            return UiActionResult.Ok(
                $"Alternatif plan uygulandı: {MatchupPlanDigest.FormatFormationLabel(formation)}"
                + $" · {MatchupPlanDigest.FormatApproachLabel(approach)}."
                + " Eşleşme değerlendirmesi yenilendi.");
        }
        catch (TeamPreparationInvariantViolationException ex)
        {
            return UiActionResult.Fail($"Alternatif plan uygulanamadı: {ex.Message}");
        }
        catch (Exception ex)
        {
            return UiActionResult.Fail($"Alternatif plan hatası: {ex.Message}");
        }
    }

    public OpponentDossierDigest? BuildOpponentDossier()
    {
        var currentDay = Host.WorldModule.Queries.GetCurrentGameDate().DayNumber;
        var pending = Host.TeamPreparationModule.SelectionQueries
            .GetNextDueManagedFixture(currentDay);
        var season = Host.CompetitionModule.Queries.GetCurrentSeason();
        if (pending is null || season is null)
        {
            return null;
        }

        var managedClub = Host.ClubModule.Queries.GetClub(pending.ManagedClubId);
        var opponentClub = Host.ClubModule.Queries.GetClub(pending.OpponentClubId);
        if (managedClub is null || opponentClub is null)
        {
            return null;
        }

        return OpponentDossierDigest.Compose(
            pending.OpponentClubId,
            opponentClub.DisplayName,
            pending.IsHome,
            managedClub.SportiveStrength,
            opponentClub.SportiveStrength,
            Host.CompetitionModule.Queries.GetStandings(season.SeasonId),
            Host.CompetitionModule.Queries.GetSeasonFixtures(season.SeasonId));
    }

    public MatchupPlanDigest? BuildMatchupPlan()
    {
        var currentDay = Host.WorldModule.Queries.GetCurrentGameDate().DayNumber;
        var pending = Host.TeamPreparationModule.SelectionQueries
            .GetNextDueManagedFixture(currentDay);
        if (pending is null)
        {
            return null;
        }

        var plan = Host.TeamPreparationModule.TacticPlanStore
            .Get(new ClubId(pending.ManagedClubId));
        var dossier = BuildOpponentDossier();
        if (plan is null || dossier is null)
        {
            return null;
        }

        return MatchupPlanDigest.Compose(plan.Formation, plan.Approach, dossier);
    }

    /// <summary>
    /// Maç gününe varış — ofisteki "kadro kilitli, düdük yakın" flash'ının maç ekranındaki karşılığı.
    /// </summary>
    public Application.CareerHub.Queries.MatchDayTempoFlash.Flash? BuildMatchDayTempoFlash()
    {
        var currentDay = Host.WorldModule.Queries.GetCurrentGameDate().DayNumber;
        var pending = Host.TeamPreparationModule.SelectionQueries
            .GetNextDueManagedFixture(currentDay);
        if (pending is null)
        {
            return null;
        }

        return Application.CareerHub.Queries.MatchDayTempoFlash.ResolveArrival(
            BuildWeekMood(),
            hasDueMatch: true,
            hasInjuryPressure: BuildPreparationBriefing().HasInjuryPressure);
    }

    /// <summary>
    /// Düdük anı — ofisteki "kadro kilitli, düdük yakın" flash'ının maç başlangıcındaki karşılığı.
    /// </summary>
    public Application.Competition.Queries.MatchKickoffMoment BuildMatchKickoffMoment()
    {
        var dressingRoomEcho = BuildDressingRoomEcho();
        var opponentDossier = BuildOpponentDossier();
        return Application.Competition.Queries.MatchKickoffMoment.Compose(
            BuildNextMatchBriefing(),
            BuildMatchDayTempoFlash(),
            dressingRoomEcho?.MomentumCode,
            dressingRoomEcho?.MomentumLength ?? 0,
            opponentDossier?.WinningStreakLength ?? 0,
            opponentDossier?.LosingStreakLength ?? 0);
    }

    public MatchDayLineupStrip BuildMatchDayLineupStrip()
    {
        var currentDay = Host.WorldModule.Queries.GetCurrentGameDate().DayNumber;
        var pending = Host.TeamPreparationModule.SelectionQueries
            .GetNextDueManagedFixture(currentDay);
        if (pending is null)
        {
            return MatchDayLineupStrip.Clear();
        }

        var timeline = Host.WorldModule.TimelineStore.Timeline;
        var clubId = new Domain.Shared.ClubId(pending.ManagedClubId);
        var squad = Host.TeamPreparationModule.SquadStore.Get(clubId);
        var playerProfiles = MvpSquadRosterGenerator.GeneratePlayerProfiles(clubId, timeline.RootSeed);
        var names = playerProfiles.Select(player => player.DisplayName).ToArray();
        var positions = playerProfiles.Select(player => player.PositionCode).ToArray();

        if (!Simulation.TrainingPhysicalState.MvpAvailabilityAwareSelection
                .TryPreviewPreferredStartingXi(
                    clubId,
                    timeline.CurrentDate,
                    Host.TrainingModule.Store.PhysicalBySlot,
                    squad,
                    out var preferredStarting,
                    out var swaps))
        {
            return new MatchDayLineupStrip(
                true,
                pending.IsApproved,
                "XI şeridi — yeterli sağlıklı oyuncu yok.",
                Array.Empty<MatchDayLineupChip>(),
                Array.Empty<MatchDayLineupChip>());
        }

        IReadOnlyList<int> displayStarting = preferredStarting;
        if (pending.IsApproved)
        {
            var approved = Host.TeamPreparationModule.SelectionQueries
                .Get(pending.FixtureId, pending.ManagedClubId);
            if (approved?.StartingSlotIndices is { Count: > 0 } starting)
            {
                displayStarting = starting;
            }
        }

        return MatchDayLineupStrip.Compose(
            hasMatch: true,
            isApproved: pending.IsApproved,
            displayStartingSlots: displayStarting,
            swaps: swaps,
            playerNames: names,
            cleanReturnNames: ResolveCleanXiBridgeNames(),
            positionCodes: positions);
    }

    public LineupCompatibilityDigest BuildLineupCompatibility()
    {
        var strip = BuildMatchDayLineupStrip();
        if (!strip.HasMatch || strip.StartingXi.Count != Domain.TeamPreparation.MatchSelection.StartingXiSize)
        {
            return LineupCompatibilityDigest.Clear();
        }

        var clubId = Host.ManagerModule.Queries.GetCareer().EmployedClubId;
        if (clubId is null)
        {
            return LineupCompatibilityDigest.Clear();
        }

        var id = new Domain.Shared.ClubId(clubId.Value);
        var timeline = Host.WorldModule.TimelineStore.Timeline;
        var plan = Host.TeamPreparationModule.TacticPlanStore.Get(id)
            ?? Domain.TeamPreparation.TacticPlan.CreateDefault(id, timeline.CurrentDate);
        var squad = MvpSquadRosterGenerator.GeneratePlayerProfiles(id, timeline.RootSeed);

        return LineupCompatibilityDigest.Compose(
            plan.Formation,
            strip.StartingXi.Select(player => player.SlotIndex).ToArray(),
            squad);
    }

    public SquadSelectionBoardDigest BuildSquadSelectionBoard()
    {
        var currentDay = Host.WorldModule.Queries.GetCurrentGameDate().DayNumber;
        var pending = Host.TeamPreparationModule.SelectionQueries
            .GetNextDueManagedFixture(currentDay);
        if (pending is null)
        {
            return SquadSelectionBoardDigest.Clear();
        }

        var timeline = Host.WorldModule.TimelineStore.Timeline;
        var clubId = new Domain.Shared.ClubId(pending.ManagedClubId);
        var profiles = MvpSquadRosterGenerator.GeneratePlayerProfiles(clubId, timeline.RootSeed);
        var ratings = Host.TeamPreparationModule.SquadQueries
            .GetClubSquad(clubId.Value, timeline.RootSeed)
            .ToDictionary(player => player.SlotIndex, player => player.Rating);
        var squad = Host.TeamPreparationModule.SquadStore.Get(clubId);
        var physical = Host.TrainingModule.Store.PhysicalBySlot;

        IReadOnlyList<int> starting;
        IReadOnlyList<int> bench;
        var approved = Host.TeamPreparationModule.SelectionQueries
            .Get(pending.FixtureId, pending.ManagedClubId);
        if (approved is not null)
        {
            starting = approved.StartingSlotIndices;
            bench = approved.BenchSlotIndices;
        }
        else
        {
            if (!Simulation.TrainingPhysicalState.MvpAvailabilityAwareSelection
                    .TryPreviewPreferredStartingXi(
                        clubId,
                        timeline.CurrentDate,
                        physical,
                        squad,
                        out starting,
                        out _))
            {
                return SquadSelectionBoardDigest.Clear();
            }

            var candidates = squad is not null && squad.Members.Count > 0
                ? squad.Members.Select(member => member.SlotIndex).OrderBy(slot => slot).ToArray()
                : Enumerable.Range(
                    Domain.TeamPreparation.MatchSelection.MinSquadSlot,
                    Domain.TeamPreparation.MatchSelection.MaxSquadSlot
                    - Domain.TeamPreparation.MatchSelection.MinSquadSlot + 1).ToArray();
            bench = candidates
                .Where(slot => !starting.Contains(slot))
                .OrderBy(slot => physical.TryGetValue((clubId.Value, slot), out var state)
                    && !state.IsAvailableOn(timeline.CurrentDate))
                .ThenBy(slot => slot)
                .Take(Domain.TeamPreparation.MatchSelection.MaxBenchSize)
                .ToArray();
        }

        return SquadSelectionBoardDigest.Compose(
            clubId,
            timeline.CurrentDate,
            approved is not null,
            starting,
            bench,
            profiles,
            ratings,
            physical);
    }

    public bool IsSeasonArchivePhase()
    {
        var season = Host.CompetitionModule.Queries.GetCurrentSeason();
        if (season is null)
        {
            return false;
        }

        var progress = Host.CompetitionModule.Queries.GetSeasonProgress(season.SeasonId);
        return progress is { CanArchive: true, CanComplete: false };
    }

    public SquadCapacityDigest BuildSquadCapacityDigest()
    {
        if (Host.ManagerModule.Queries.GetCareer().EmployedClubId is not long clubId
            || Host.TeamPreparationModule.ClubSquad is null)
        {
            return SquadCapacityDigest.Unemployed();
        }

        var day = Host.WorldModule.TimelineStore.Timeline.CurrentDate;
        return Host.TeamPreparationModule.ClubSquad.GetCapacityDigest(
            new Domain.Shared.ClubId(clubId),
            day);
    }

    public long? SuggestReleaseCandidatePlayerId()
    {
        if (Host.ManagerModule.Queries.GetCareer().EmployedClubId is not long clubId
            || Host.TeamPreparationModule.ClubSquad is not { } squad)
        {
            return null;
        }

        var day = Host.WorldModule.TimelineStore.Timeline.CurrentDate;
        return squad.SuggestReleaseCandidatePlayerId(new Domain.Shared.ClubId(clubId), day);
    }

    public long? SuggestSaleCandidatePlayerId()
    {
        if (Host.ManagerModule.Queries.GetCareer().EmployedClubId is not long clubId
            || Host.TeamPreparationModule.ClubSquad is not { } squad)
        {
            return null;
        }

        var exitPreferred = Host.TransferModule.Queries.GetPreferredPlayerExitSaleCandidateId();
        if (exitPreferred is long exitPlayerId)
        {
            return exitPlayerId;
        }

        var day = Host.WorldModule.TimelineStore.Timeline.CurrentDate;
        return squad.SuggestSaleCandidatePlayerId(new Domain.Shared.ClubId(clubId), day);
    }

    public LeagueWorldBriefing BuildLeagueWorldBriefing()
    {
        var season = Host.CompetitionModule.Queries.GetCurrentSeason();
        if (season is null || (season.FixtureCount == 0 && season.ParticipantCount == 0))
        {
            return LeagueWorldBriefing.NoSeason();
        }

        var progress = Host.CompetitionModule.Queries.GetSeasonProgress(season.SeasonId);
        var standings = Host.CompetitionModule.Queries.GetStandings(season.SeasonId);
        var managedClubId = Host.ManagerModule.Queries.GetCareer().EmployedClubId;
        string? managedName = managedClubId is long id ? GetClubDisplayName(id) : null;

        int? managedRank = null;
        int? managedPoints = null;
        int? managedPlayed = null;
        int? managedGd = null;
        string? leaderName = null;
        int? leaderPoints = null;

        if (standings.Count > 0)
        {
            leaderName = GetClubDisplayName(standings[0].ClubId);
            leaderPoints = standings[0].Points;
            if (managedClubId is long clubId)
            {
                for (var i = 0; i < standings.Count; i++)
                {
                    if (standings[i].ClubId == clubId)
                    {
                        managedRank = i + 1;
                        managedPoints = standings[i].Points;
                        managedPlayed = standings[i].Played;
                        managedGd = standings[i].GoalDifference;
                        break;
                    }
                }
            }
        }

        var currentDay = Host.WorldModule.Queries.GetCurrentGameDate().DayNumber;
        var pending = Host.TeamPreparationModule.SelectionQueries
            .GetNextDueManagedFixture(currentDay);
        string? nextMatch = null;
        if (pending is not null)
        {
            var venue = pending.IsHome ? "Ev" : "Dep";
            var daysUntil = pending.ScheduledDayNumber - currentDay;
            var when = daysUntil switch
            {
                <= 0 => "bugün",
                1 => "yarın",
                _ => $"{daysUntil} gün sonra",
            };
            nextMatch = $"{venue} vs {GetClubDisplayName(pending.OpponentClubId)} · {when}";
        }

        return LeagueWorldBriefing.Compose(
            season.Status,
            progress?.AcceptedFixtureCount ?? 0,
            progress?.TotalFixtureCount ?? season.FixtureCount,
            season.ParticipantCount,
            managedRank,
            managedPoints,
            managedPlayed,
            managedGd,
            managedName,
            leaderName,
            leaderPoints,
            nextMatch);
    }

    public DressingRoomEchoDigest? BuildDressingRoomEcho()
    {
        var employment = Host.ManagerModule.Store.Career.ActiveEmployment;
        var season = Host.CompetitionModule.Queries.GetCurrentSeason();
        if (employment is null || season is null)
        {
            return null;
        }

        var clubNames = Host.ClubModule.Queries.GetAllClubs()
            .ToDictionary(club => club.ClubId, club => club.DisplayName);
        return DressingRoomEchoDigest.Compose(
            Host.CompetitionModule.Queries.GetSeasonFixtures(season.SeasonId),
            employment.ClubId.Value,
            employment.StartedAt.DayNumber,
            clubNames);
    }

    public PreparationBriefing BuildPreparationBriefing()
    {
        var training = GetTrainingSummary();
        var tactic = Host.TeamPreparationModule.TacticQueries.GetManagedClubPlan();
        var currentDay = Host.WorldModule.Queries.GetCurrentGameDate().DayNumber;
        var pending = Host.TeamPreparationModule.SelectionQueries
            .GetNextDueManagedFixture(currentDay);
        string? fixtureLine = null;
        int? daysUntil = null;
        if (pending is not null)
        {
            var venue = pending.IsHome ? "Ev" : "Dep";
            var opponent = GetClubDisplayName(pending.OpponentClubId);
            daysUntil = pending.ScheduledDayNumber - currentDay;
            var when = daysUntil switch
            {
                <= 0 => "bugün",
                1 => "yarın",
                _ => $"{daysUntil} gün sonra",
            };
            fixtureLine = $"{venue} vs {opponent} · {when}";
        }

        return PreparationBriefing.Compose(
            training,
            tactic,
            GetManagedTacticModifierLabel(),
            fixtureLine,
            daysUntil);
    }

    public TransferDeskBriefing BuildTransferDeskBriefing()
    {
        if (Host.ManagerModule.Queries.GetCareer().EmployedClubId is not long clubId)
        {
            return TransferDeskBriefing.Unemployed();
        }

        var window = Host.WorldModule.Queries.GetTransferWindow();
        var needs = Host.TransferModule.Queries.GetManagedClubNeeds();
        var exitNeeds = needs.OpenNeeds.Count(n =>
            n.ReasonCode.StartsWith("PlayerExit:", StringComparison.Ordinal)
            || n.KindName.Contains("ayrılma", StringComparison.OrdinalIgnoreCase));
        var targets = Host.TransferModule.Queries.GetManagedClubShortlistTargets();
        var processes = Host.TransferModule.Queries.GetManagedClubProcesses();
        var offers = Host.TransferModule.Queries.GetManagedClubOffers();
        var budget = Host.ClubModule.TransferBudget.Get(new Domain.Shared.ClubId(clubId));
        var capacity = BuildSquadCapacityDigest();
        var currentDay = Host.WorldModule.Queries.GetCurrentGameDate().DayNumber;
        var saleCandidateId = SuggestSaleCandidatePlayerId();
        var (saleName, saleDetail) = DescribeManagedPlayer(saleCandidateId);
        var (promiseExitPlayerId, promiseExitHint) = ResolvePromiseExitPressure();
        var (promiseExitName, _) = DescribeManagedPlayer(promiseExitPlayerId);

        return TransferDeskBriefing.Compose(
            window.IsOpen,
            window.StatusName,
            window.ClosesOnDayNumber,
            needs.OpenCount,
            exitNeeds,
            targets.ListedTargetCount,
            processes.ActiveCount,
            offers.PendingCount,
            budget.Available,
            budget.Spent,
            capacity.IsFull || capacity.IsOverCapacity,
            saleCandidateId,
            currentDay,
            promiseExitPlayerId,
            promiseExitHint,
            saleName,
            saleDetail,
            promiseExitName);
    }

    private (long? PlayerId, string? Hint) ResolvePromiseExitPressure()
    {
        var openTransfer = Host.InteractionModule.DecisionRequestStore.Requests
            .Where(r => r.IsOpen && r.Kind == Domain.Interaction.DecisionRequestKind.TransferRequest)
            .OrderBy(r => r.DeadlineOn.DayNumber)
            .ThenBy(r => r.DecisionRequestId.Value)
            .FirstOrDefault();
        if (openTransfer is null)
        {
            return (null, null);
        }

        var hint = Host.InteractionModule.Queries.ExplainCausality(openTransfer.DecisionRequestId);
        return (openTransfer.SubjectPlayerId.Value, hint);
    }

    public LeagueStatisticsDigest BuildLeagueStatisticsDigest()
    {
        var season = Host.CompetitionModule.Queries.GetCurrentSeason();
        if (season is null)
        {
            return LeagueStatisticsDigest.Empty();
        }

        var clubNames = Host.ClubModule.Queries.GetAllClubs()
            .ToDictionary(club => club.ClubId, club => club.DisplayName);
        return LeagueStatisticsDigest.Compose(
            Host.CompetitionModule.Queries.GetStandings(season.SeasonId),
            Host.CompetitionModule.Queries.GetSeasonFixtures(season.SeasonId),
            clubNames,
            Host.ManagerModule.Queries.GetCareer().EmployedClubId);
    }

    public ScoutTransferDigest BuildScoutTransferDigest()
    {
        var manager = Host.ManagerModule.Queries.GetCareer();
        if (manager.EmployedClubId is not long clubId)
        {
            return ScoutTransferDigest.Clear();
        }

        var timeline = Host.WorldModule.TimelineStore.Timeline;
        var managedId = new Domain.Shared.ClubId(clubId);
        var managedProfiles = MvpSquadRosterGenerator.GeneratePlayerProfiles(managedId, timeline.RootSeed);
        var managedRatings = Host.TeamPreparationModule.SquadQueries
            .GetClubSquad(clubId, timeline.RootSeed)
            .ToDictionary(player => player.SlotIndex, player => player.Rating);
        var shortlist = Host.TransferModule.ShortlistStore.GetForClub(managedId)
            .Where(entry => entry.IsActive)
            .GroupBy(entry => entry.PlayerId.Value)
            .ToDictionary(group => group.Key, group => group.Min(entry => entry.AddedOn.DayNumber));
        var listedIds = Host.TransferModule.TargetStore.GetForClub(managedId)
            .Where(target => target.IsListed)
            .Select(target => target.PlayerId.Value)
            .ToHashSet();
        var candidates = new List<ScoutCandidateSource>();

        foreach (var club in Host.ClubModule.Queries.GetAllClubs().Where(club => club.ClubId != clubId))
        {
            var sourceId = new Domain.Shared.ClubId(club.ClubId);
            var profiles = MvpSquadRosterGenerator.GeneratePlayerProfiles(sourceId, timeline.RootSeed);
            var ratings = Host.TeamPreparationModule.SquadQueries
                .GetClubSquad(club.ClubId, timeline.RootSeed)
                .ToDictionary(player => player.SlotIndex, player => player.Rating);
            for (var slot = 0; slot < profiles.Count; slot++)
            {
                var playerId = PlayerId.FromClubSlot(club.ClubId, slot);
                var career = Host.PlayerCareerModule.Store.Get(sourceId, slot);
                var rating = ratings.GetValueOrDefault(slot, career?.CurrentAbility ?? 60);
                candidates.Add(new ScoutCandidateSource(
                    playerId.Value,
                    club.ClubId,
                    club.DisplayName,
                    profiles[slot].DisplayName,
                    profiles[slot].PositionGroup,
                    profiles[slot].PositionCode,
                    rating,
                    career?.AgeYears(timeline.CurrentDate) ?? 20 + slot % 10,
                    career?.PotentialAbility ?? Math.Min(99, rating + 5),
                    shortlist.GetValueOrDefault(playerId.Value),
                    listedIds.Contains(playerId.Value)));
            }
        }

        return ScoutTransferDigest.Compose(
            GetClubDisplayName(clubId),
            timeline.CurrentDate.DayNumber,
            managedProfiles,
            managedRatings,
            candidates);
    }

    public ClubTrainingSummaryReadModel GetTrainingSummary() =>
        Host.TrainingModule.Queries.GetManagedClubSummary();

    public UiActionResult SetTacticApproach(TacticalApproach approach)
    {
        try
        {
            var clubId = Host.ManagerModule.Queries.GetCareer().EmployedClubId
                ?? throw new InvalidOperationException("Menajer kulübü yok.");
            var day = Host.WorldModule.TimelineStore.Timeline.CurrentDate;
            Host.TeamPreparationModule.TacticPlans.SetApproach(
                new Domain.Shared.ClubId(clubId),
                approach,
                day);
            var view = Host.TeamPreparationModule.TacticQueries.GetManagedClubPlan();
            var plan = Host.TeamPreparationModule.TacticPlanStore.Get(new Domain.Shared.ClubId(clubId));
            var tacticMod = MvpTacticMatchModifier.ComputeTacticModifier(plan);
            var advice = BuildPreparationBriefing().AdviceLine;
            return UiActionResult.Ok(
                $"Taktik yaklaşım: {view.ApproachName} · formasyon {view.FormationName} · maç {FormatSigned(tacticMod)}."
                + $"\nÖneri: {advice}");
        }
        catch (TeamPreparationInvariantViolationException ex)
        {
            return UiActionResult.Fail($"Taktik ayarlanamadı: {ex.Message}");
        }
        catch (Exception ex)
        {
            return UiActionResult.Fail($"Taktik hatası: {ex.Message}");
        }
    }

    public UiActionResult SetTacticFormation(Formation formation)
    {
        try
        {
            var clubId = Host.ManagerModule.Queries.GetCareer().EmployedClubId
                ?? throw new InvalidOperationException("Menajer kulübü yok.");
            var day = Host.WorldModule.TimelineStore.Timeline.CurrentDate;
            Host.TeamPreparationModule.TacticPlans.SetFormation(
                new Domain.Shared.ClubId(clubId),
                formation,
                day);
            var view = Host.TeamPreparationModule.TacticQueries.GetManagedClubPlan();
            var plan = Host.TeamPreparationModule.TacticPlanStore.Get(new Domain.Shared.ClubId(clubId));
            var tacticMod = MvpTacticMatchModifier.ComputeTacticModifier(plan);
            var advice = BuildPreparationBriefing().AdviceLine;
            return UiActionResult.Ok(
                $"Formasyon: {view.FormationName} · yaklaşım {view.ApproachName} · maç {FormatSigned(tacticMod)}."
                + $"\nÖneri: {advice}");
        }
        catch (TeamPreparationInvariantViolationException ex)
        {
            return UiActionResult.Fail($"Formasyon ayarlanamadı: {ex.Message}");
        }
        catch (Exception ex)
        {
            return UiActionResult.Fail($"Formasyon hatası: {ex.Message}");
        }
    }

    private static string FormatSigned(int value) =>
        value > 0 ? $"+{value}" : value.ToString();

    public string GetManagedTacticModifierLabel()
    {
        var clubId = Host.ManagerModule.Queries.GetCareer().EmployedClubId;
        if (clubId is null)
        {
            return "±0";
        }

        var plan = Host.TeamPreparationModule.TacticPlanStore.Get(new Domain.Shared.ClubId(clubId.Value));
        return FormatSigned(MvpTacticMatchModifier.ComputeTacticModifier(plan));
    }

    public UiActionResult RefreshTransferNeedSuggestions()
    {
        try
        {
            var clubId = Host.ManagerModule.Queries.GetCareer().EmployedClubId
                ?? throw new InvalidOperationException("Menajer kulübü yok.");
            var day = Host.WorldModule.TimelineStore.Timeline.CurrentDate;
            var refreshed = Host.TransferModule.Needs.RefreshSuggestions(
                new Domain.Shared.ClubId(clubId),
                day);
            var summary = Host.TransferModule.Queries.GetManagedClubNeeds();
            return UiActionResult.Ok(
                refreshed.Count == 0
                    ? $"Transfer ihtiyacı önerisi yok · açık {summary.OpenCount}."
                    : $"Transfer ihtiyacı güncellendi: {refreshed.Count} kayıt · açık {summary.OpenCount}.");
        }
        catch (TransferInvariantViolationException ex)
        {
            return UiActionResult.Fail($"Transfer ihtiyacı güncellenemedi: {ex.Message}");
        }
        catch (Exception ex)
        {
            return UiActionResult.Fail($"Transfer ihtiyacı hatası: {ex.Message}");
        }
    }

    public UiActionResult DeclarePositionGapNeed()
    {
        try
        {
            var clubId = Host.ManagerModule.Queries.GetCareer().EmployedClubId
                ?? throw new InvalidOperationException("Menajer kulübü yok.");
            var day = Host.WorldModule.TimelineStore.Timeline.CurrentDate;
            var need = Host.TransferModule.Needs.Declare(
                new Domain.Shared.ClubId(clubId),
                TransferNeedKind.PositionGap,
                priority: 4,
                "ManualPositionGap",
                day);
            return UiActionResult.Ok(
                $"Transfer ihtiyacı: #{need.NeedId.Value} pozisyon açığı (öncelik {need.Priority}).");
        }
        catch (TransferInvariantViolationException ex)
        {
            return UiActionResult.Fail($"İhtiyaç tanımlanamadı: {ex.Message}");
        }
        catch (Exception ex)
        {
            return UiActionResult.Fail($"İhtiyaç hatası: {ex.Message}");
        }
    }

    public UiActionResult CloseOldestOpenTransferNeed()
    {
        try
        {
            var summary = Host.TransferModule.Queries.GetManagedClubNeeds();
            var oldest = summary.OpenNeeds.OrderBy(n => n.NeedId).FirstOrDefault()
                ?? throw new InvalidOperationException("Kapatılacak açık ihtiyaç yok.");
            var day = Host.WorldModule.TimelineStore.Timeline.CurrentDate;
            Host.TransferModule.Needs.Close(new TransferNeedId(oldest.NeedId), day);
            return UiActionResult.Ok($"Transfer ihtiyacı kapatıldı: #{oldest.NeedId} ({oldest.KindName}).");
        }
        catch (TransferInvariantViolationException ex)
        {
            return UiActionResult.Fail($"İhtiyaç kapatılamadı: {ex.Message}");
        }
        catch (Exception ex)
        {
            return UiActionResult.Fail($"İhtiyaç kapatma hatası: {ex.Message}");
        }
    }

    public UiActionResult SuggestTransferTarget()
    {
        var candidate = BuildScoutTransferDigest().Candidates
            .FirstOrDefault(player => !player.IsListedTarget);
        return candidate is null
            ? UiActionResult.Fail("Scout listesinde eklenecek yeni aday yok.")
            : AddScoutCandidateToShortlist(candidate.PlayerId);
    }

    public UiActionResult AddScoutCandidateToShortlist(long playerId)
    {
        try
        {
            var clubId = Host.ManagerModule.Queries.GetCareer().EmployedClubId
                ?? throw new InvalidOperationException("Menajer kulübü yok.");
            var day = Host.WorldModule.TimelineStore.Timeline.CurrentDate;
            var id = new Domain.Shared.ClubId(clubId);
            var scout = BuildScoutTransferDigest();
            var candidate = scout.Candidates.FirstOrDefault(player => player.PlayerId == playerId)
                ?? throw new InvalidOperationException("Seçili futbolcu güncel scout adayları arasında değil.");
            if (Host.TransferModule.Queries.GetManagedClubNeeds().OpenCount == 0)
            {
                Host.TransferModule.Needs.Declare(
                    id,
                    TransferNeedKind.PositionGap,
                    priority: 4,
                    $"ScoutPosition:{scout.NeedPositionCode}",
                    day);
            }

            var need = Host.TransferModule.Queries.GetManagedClubNeeds().OpenNeeds
                .OrderByDescending(item => item.Priority)
                .ThenBy(item => item.NeedId)
                .First();
            var player = new PlayerId(playerId);
            var entry = Host.TransferModule.ShortlistTargets.AddToShortlist(
                id,
                player,
                new TransferNeedId(need.NeedId),
                priority: 4,
                day);
            var target = Host.TransferModule.ShortlistTargets.AddTransferTarget(
                new TransferNeedId(need.NeedId),
                player,
                entry.EntryId,
                day);
            return UiActionResult.Ok(
                $"Scout hedefi listelendi: {candidate.DisplayName} · {candidate.PositionCode}"
                + $" · {candidate.ClubName} · bilgi %{candidate.KnowledgePercent}"
                + $" · hedef #{target.TargetId.Value} · ihtiyaç #{target.NeedId.Value}.");
        }
        catch (TransferInvariantViolationException ex)
        {
            return UiActionResult.Fail($"Hedef eklenemedi: {ex.Message}");
        }
        catch (Exception ex)
        {
            return UiActionResult.Fail($"Hedef hatası: {ex.Message}");
        }
    }

    public UiActionResult DropOldestListedTransferTarget()
    {
        try
        {
            var listed = Host.TransferModule.Queries.GetManagedClubShortlistTargets().ListedTargets
                .OrderBy(t => t.TargetId)
                .FirstOrDefault()
                ?? throw new InvalidOperationException("Düşürülecek hedef yok.");
            var day = Host.WorldModule.TimelineStore.Timeline.CurrentDate;
            Host.TransferModule.ShortlistTargets.DropTransferTarget(
                new TransferTargetId(listed.TargetId),
                day);
            return UiActionResult.Ok(
                $"Hedef düşürüldü: {GetPlayerDisplayName(listed.PlayerId)}.");
        }
        catch (TransferInvariantViolationException ex)
        {
            return UiActionResult.Fail($"Hedef düşürülemedi: {ex.Message}");
        }
        catch (Exception ex)
        {
            return UiActionResult.Fail($"Hedef düşürme hatası: {ex.Message}");
        }
    }

    public UiActionResult OpenTransferProcessFromOldestTarget()
    {
        try
        {
            var clubId = Host.ManagerModule.Queries.GetCareer().EmployedClubId
                ?? throw new InvalidOperationException("Menajer kulübü yok.");
            var day = Host.WorldModule.TimelineStore.Timeline.CurrentDate;
            var id = new Domain.Shared.ClubId(clubId);
            if (Host.TransferModule.Queries.GetManagedClubShortlistTargets().ListedTargetCount == 0)
            {
                SuggestTransferTarget();
            }

            var process = Host.TransferModule.Processes.OpenOldestListedTargetForClub(id, day);
            return UiActionResult.Ok(
                $"Süreç açıldı: {GetPlayerDisplayName(process.PlayerId.Value)} (değerlendirmede).");
        }
        catch (TransferInvariantViolationException ex)
        {
            return UiActionResult.Fail($"Süreç açılamadı: {ex.Message}");
        }
        catch (Exception ex)
        {
            return UiActionResult.Fail($"Süreç hatası: {ex.Message}");
        }
    }

    public UiActionResult WithdrawOldestActiveTransferProcess()
    {
        try
        {
            var active = Host.TransferModule.Queries.GetManagedClubProcesses().ActiveProcesses
                .OrderBy(p => p.ProcessId)
                .FirstOrDefault()
                ?? throw new InvalidOperationException("Geri çekilecek aktif süreç yok.");
            var day = Host.WorldModule.TimelineStore.Timeline.CurrentDate;
            Host.TransferModule.Processes.Withdraw(new TransferProcessId(active.ProcessId), day);
            return UiActionResult.Ok("Süreç geri çekildi.");
        }
        catch (TransferInvariantViolationException ex)
        {
            return UiActionResult.Fail($"Süreç geri çekilemedi: {ex.Message}");
        }
        catch (Exception ex)
        {
            return UiActionResult.Fail($"Süreç geri çekme hatası: {ex.Message}");
        }
    }

    public UiActionResult RequestSportingApprovalForOldestProcess()
    {
        try
        {
            var processId = ResolveOldestActiveProcessId();
            var updated = Host.TransferModule.Processes.RequestSportingApproval(
                new TransferProcessId(processId));
            return UiActionResult.Ok(
                $"Sportif onay istendi: {TranslateProcessStatus(updated.Status)}.");
        }
        catch (TransferInvariantViolationException ex)
        {
            return UiActionResult.Fail($"Sportif onay istenemedi: {ex.Message}");
        }
        catch (Exception ex)
        {
            return UiActionResult.Fail($"Sportif onay hatası: {ex.Message}");
        }
    }

    public UiActionResult GrantSportingApprovalForOldestPendingProcess()
    {
        try
        {
            var pending = Host.TransferModule.Queries.GetManagedClubProcesses().ActiveProcesses
                .Where(p => p.StatusCode == (int)TransferProcessStatus.SportingApprovalPending)
                .OrderBy(p => p.ProcessId)
                .FirstOrDefault()
                ?? throw new InvalidOperationException("Onay bekleyen süreç yok.");
            var updated = Host.TransferModule.Processes.GrantSportingApproval(
                new TransferProcessId(pending.ProcessId));
            return UiActionResult.Ok(
                $"Sportif onay verildi: {GetPlayerDisplayName(updated.PlayerId.Value)}.");
        }
        catch (TransferInvariantViolationException ex)
        {
            return UiActionResult.Fail($"Sportif onay verilemedi: {ex.Message}");
        }
        catch (Exception ex)
        {
            return UiActionResult.Fail($"Sportif onay hatası: {ex.Message}");
        }
    }

    public UiActionResult RejectSportingApprovalForOldestPendingProcess()
    {
        try
        {
            var pending = Host.TransferModule.Queries.GetManagedClubProcesses().ActiveProcesses
                .Where(p => p.StatusCode == (int)TransferProcessStatus.SportingApprovalPending)
                .OrderBy(p => p.ProcessId)
                .FirstOrDefault()
                ?? throw new InvalidOperationException("Reddedilecek onay bekleyen süreç yok.");
            var day = Host.WorldModule.TimelineStore.Timeline.CurrentDate;
            var updated = Host.TransferModule.Processes.RejectSportingApproval(
                new TransferProcessId(pending.ProcessId),
                "SportingRejected",
                day);
            return UiActionResult.Ok("Sportif red uygulandı.");
        }
        catch (TransferInvariantViolationException ex)
        {
            return UiActionResult.Fail($"Sportif red başarısız: {ex.Message}");
        }
        catch (Exception ex)
        {
            return UiActionResult.Fail($"Sportif red hatası: {ex.Message}");
        }
    }

    public UiActionResult RequestFinancialApprovalForOldestProcess()
    {
        try
        {
            var process = Host.TransferModule.Queries.GetManagedClubProcesses().ActiveProcesses
                .Where(p => p.StatusCode == (int)TransferProcessStatus.PlayerAgreementReached)
                .OrderBy(p => p.ProcessId)
                .FirstOrDefault()
                ?? throw new InvalidOperationException("Oyuncu anlaşmalı süreç yok.");
            var updated = Host.TransferModule.Processes.RequestFinancialApproval(
                new TransferProcessId(process.ProcessId));
            return UiActionResult.Ok(
                $"Mali onay istendi: {TranslateProcessStatus(updated.Status)}.");
        }
        catch (TransferInvariantViolationException ex)
        {
            return UiActionResult.Fail($"Mali onay istenemedi: {ex.Message}");
        }
        catch (Exception ex)
        {
            return UiActionResult.Fail($"Mali onay hatası: {ex.Message}");
        }
    }

    public UiActionResult GrantFinancialApprovalForOldestPendingProcess()
    {
        try
        {
            var pending = Host.TransferModule.Queries.GetManagedClubProcesses().ActiveProcesses
                .Where(p => p.StatusCode == (int)TransferProcessStatus.FinancialApprovalPending)
                .OrderBy(p => p.ProcessId)
                .FirstOrDefault()
                ?? throw new InvalidOperationException("Mali onay bekleyen süreç yok.");
            var updated = Host.TransferModule.Processes.GrantFinancialApproval(
                new TransferProcessId(pending.ProcessId));
            return UiActionResult.Ok(
                $"Mali onay verildi: {GetPlayerDisplayName(updated.PlayerId.Value)}.");
        }
        catch (TransferInvariantViolationException ex)
        {
            return UiActionResult.Fail($"Mali onay verilemedi: {ex.Message}");
        }
        catch (Exception ex)
        {
            return UiActionResult.Fail($"Mali onay hatası: {ex.Message}");
        }
    }

    public UiActionResult RejectFinancialApprovalForOldestPendingProcess()
    {
        try
        {
            var pending = Host.TransferModule.Queries.GetManagedClubProcesses().ActiveProcesses
                .Where(p => p.StatusCode == (int)TransferProcessStatus.FinancialApprovalPending)
                .OrderBy(p => p.ProcessId)
                .FirstOrDefault()
                ?? throw new InvalidOperationException("Reddedilecek mali onay bekleyen süreç yok.");
            var day = Host.WorldModule.TimelineStore.Timeline.CurrentDate;
            var updated = Host.TransferModule.Processes.RejectFinancialApproval(
                new TransferProcessId(pending.ProcessId),
                "FinancialRejected",
                day);
            return UiActionResult.Ok("Mali red uygulandı.");
        }
        catch (TransferInvariantViolationException ex)
        {
            return UiActionResult.Fail($"Mali red başarısız: {ex.Message}");
        }
        catch (Exception ex)
        {
            return UiActionResult.Fail($"Mali red hatası: {ex.Message}");
        }
    }

    public UiActionResult CompleteOldestFinanciallyApprovedProcess()
    {
        try
        {
            var process = Host.TransferModule.Queries.GetManagedClubProcesses().ActiveProcesses
                .Where(p =>
                    p.StatusCode is (int)TransferProcessStatus.FinancialApproved
                        or (int)TransferProcessStatus.CompletionPending)
                .OrderBy(p => p.ProcessId)
                .FirstOrDefault()
                ?? throw new InvalidOperationException("Tamamlanacak mali onaylı süreç yok.");
            var day = Host.WorldModule.TimelineStore.Timeline.CurrentDate;
            var updated = Host.TransferModule.Completion.Complete(
                new TransferProcessId(process.ProcessId),
                day);
            return UiActionResult.Ok(
                $"Transfer tamamlandı: {GetPlayerDisplayName(updated.PlayerId.Value)}"
                + $" → {GetClubDisplayName(updated.BuyingClubId.Value)}.");
        }
        catch (TransferInvariantViolationException ex)
        {
            return UiActionResult.Fail($"Transfer tamamlanamadı: {ex.Message}");
        }
        catch (Exception ex)
        {
            return UiActionResult.Fail($"Transfer tamamlama hatası: {ex.Message}");
        }
    }

    /// <summary>
    /// Transfer masası birincil CTA — en eski aktif süreçte bir adım ilerler.
    /// </summary>
    public UiActionResult AdvanceOldestTransferStep()
    {
        try
        {
            var offers = Host.TransferModule.Queries.GetManagedClubOffers();
            if (offers.PendingCount > 0)
            {
                return AcceptPendingClubOffer();
            }

            var proposals = Host.TransferModule.Queries.GetManagedContractProposals();
            if (proposals.PendingCount > 0)
            {
                return AcceptPendingContractProposal();
            }

            var process = Host.TransferModule.Queries.GetManagedClubProcesses().ActiveProcesses
                .OrderBy(p => p.ProcessId)
                .FirstOrDefault()
                ?? throw new InvalidOperationException("İlerletilecek aktif süreç yok.");

            return ((TransferProcessStatus)process.StatusCode) switch
            {
                TransferProcessStatus.UnderEvaluation =>
                    RequestSportingApprovalForOldestProcess(),
                TransferProcessStatus.SportingApprovalPending =>
                    GrantSportingApprovalForOldestPendingProcess(),
                TransferProcessStatus.SportingApproved
                    or TransferProcessStatus.ClubNegotiation
                    when !process.IsFreeAgent =>
                    SubmitDefaultClubOffer(),
                TransferProcessStatus.SportingApproved when process.IsFreeAgent =>
                    SubmitDefaultContractProposal(),
                TransferProcessStatus.ClubAgreementReached
                    or TransferProcessStatus.PlayerNegotiation =>
                    SubmitDefaultContractProposal(),
                TransferProcessStatus.PlayerAgreementReached =>
                    RequestFinancialApprovalForOldestProcess(),
                TransferProcessStatus.FinancialApprovalPending =>
                    GrantFinancialApprovalForOldestPendingProcess(),
                TransferProcessStatus.FinancialApproved
                    or TransferProcessStatus.CompletionPending =>
                    CompleteOldestFinanciallyApprovedProcess(),
                _ => UiActionResult.Fail(
                    $"Bu süreç durumunda otomatik adım yok ({process.StatusName}). Müzakere kartlarını aç."),
            };
        }
        catch (Exception ex)
        {
            return UiActionResult.Fail($"Süreç ilerletilemedi: {ex.Message}");
        }
    }

    public UiActionResult OpenTransferWindow()
    {
        try
        {
            var result = Host.WorldModule.OpenTransferWindow.Handle(
                new Application.WorldCalendar.Commands.OpenTransferWindowCommand(
                    Guid.NewGuid(),
                    ClosesOnDayNumber: null));
            return UiActionResult.Ok(
                $"Transfer penceresi açıldı ({GameDate.ToDisplayDateString(result.OpenedOnDayNumber)})"
                + $" · AI transfer: {result.AiTransferCompletedCount}.");
        }
        catch (Exception ex)
        {
            return UiActionResult.Fail($"Pencere açılamadı: {ex.Message}");
        }
    }

    public UiActionResult CloseTransferWindow()
    {
        try
        {
            var closeResult = Host.WorldModule.CloseTransferWindow.Handle(
                new Application.WorldCalendar.Commands.CloseTransferWindowCommand(Guid.NewGuid()));
            return UiActionResult.Ok(
                $"Transfer penceresi kapatıldı (expire: {closeResult.ExpiredProcessCount}, taşınan: {closeResult.CarriedProcessCount}).");
        }
        catch (Exception ex)
        {
            return UiActionResult.Fail($"Pencere kapatılamadı: {ex.Message}");
        }
    }

    private long ResolveOldestActiveProcessId()
    {
        var active = Host.TransferModule.Queries.GetManagedClubProcesses().ActiveProcesses
            .OrderBy(p => p.ProcessId)
            .FirstOrDefault()
            ?? throw new InvalidOperationException("Aktif transfer süreci yok.");
        return active.ProcessId;
    }

    public UiActionResult SubmitDefaultClubOffer()
    {
        try
        {
            var process = ResolveProcessForClubOffer();
            var day = Host.WorldModule.TimelineStore.Timeline.CurrentDate;
            var offer = Host.TransferModule.ClubOffers.SubmitClubOffer(
                new TransferProcessId(process.ProcessId),
                offeredFee: 5_000_000,
                day);
            return UiActionResult.Ok(
                $"Kulüp teklifi: #{offer.OfferId.Value} tur {offer.Round} · ücret {offer.OfferedFee}.");
        }
        catch (TransferInvariantViolationException ex)
        {
            return UiActionResult.Fail($"Teklif sunulamadı: {ex.Message}");
        }
        catch (Exception ex)
        {
            return UiActionResult.Fail($"Teklif hatası: {ex.Message}");
        }
    }

    public UiActionResult AcceptPendingClubOffer()
    {
        try
        {
            var process = ResolveProcessForClubOffer(requireNegotiation: true);
            var offer = Host.TransferModule.ClubOffers.AcceptPendingOffer(
                new TransferProcessId(process.ProcessId));
            return UiActionResult.Ok(
                $"Teklif kabul: #{offer.OfferId.Value} · ücret {offer.OfferedFee} · kulüp anlaşması.");
        }
        catch (TransferInvariantViolationException ex)
        {
            return UiActionResult.Fail($"Teklif kabul edilemedi: {ex.Message}");
        }
        catch (Exception ex)
        {
            return UiActionResult.Fail($"Teklif kabul hatası: {ex.Message}");
        }
    }

    public UiActionResult RejectPendingClubOffer()
    {
        try
        {
            var process = ResolveProcessForClubOffer(requireNegotiation: true);
            var offer = Host.TransferModule.ClubOffers.RejectPendingOffer(
                new TransferProcessId(process.ProcessId));
            return UiActionResult.Ok($"Teklif reddedildi: #{offer.OfferId.Value}.");
        }
        catch (TransferInvariantViolationException ex)
        {
            return UiActionResult.Fail($"Teklif reddedilemedi: {ex.Message}");
        }
        catch (Exception ex)
        {
            return UiActionResult.Fail($"Teklif ret hatası: {ex.Message}");
        }
    }

    public UiActionResult CounterPendingClubOffer()
    {
        try
        {
            var process = ResolveProcessForClubOffer(requireNegotiation: true);
            var day = Host.WorldModule.TimelineStore.Timeline.CurrentDate;
            var pendingFee = Host.TransferModule.OfferStore.GetForProcess(
                    new TransferProcessId(process.ProcessId))
                .LastOrDefault(o => o.IsPending)?.OfferedFee ?? 5_000_000;
            var offer = Host.TransferModule.ClubOffers.CounterPendingOffer(
                new TransferProcessId(process.ProcessId),
                offeredFee: pendingFee + 1_000_000,
                day);
            return UiActionResult.Ok(
                $"Karşı teklif: #{offer.OfferId.Value} tur {offer.Round} · ücret {offer.OfferedFee}.");
        }
        catch (TransferInvariantViolationException ex)
        {
            return UiActionResult.Fail($"Karşı teklif başarısız: {ex.Message}");
        }
        catch (Exception ex)
        {
            return UiActionResult.Fail($"Karşı teklif hatası: {ex.Message}");
        }
    }

    private Application.Transfer.Queries.TransferProcessLineReadModel ResolveProcessForClubOffer(
        bool requireNegotiation = false)
    {
        var active = Host.TransferModule.Queries.GetManagedClubProcesses().ActiveProcesses
            .Where(p => !p.IsFreeAgent);
        var process = (requireNegotiation
                ? active.Where(p => p.StatusCode == (int)TransferProcessStatus.ClubNegotiation)
                : active.Where(p =>
                    p.StatusCode is (int)TransferProcessStatus.SportingApproved
                        or (int)TransferProcessStatus.ClubNegotiation))
            .OrderBy(p => p.ProcessId)
            .FirstOrDefault()
            ?? throw new InvalidOperationException(
                requireNegotiation
                    ? "Müzakeredeki süreç yok."
                    : "Sportif onaylı veya müzakeredeki süreç yok.");
        return process;
    }

    public UiActionResult SubmitDefaultContractProposal()
    {
        try
        {
            var process = ResolveProcessForContractProposal();
            var day = Host.WorldModule.TimelineStore.Timeline.CurrentDate;
            var proposal = Host.TransferModule.ContractProposals.SubmitContractProposal(
                new TransferProcessId(process.ProcessId),
                weeklyWage: 25_000,
                contractYears: 3,
                day);
            return UiActionResult.Ok(
                $"Sözleşme teklifi: #{proposal.ProposalId.Value} tur {proposal.Round}"
                + $" · maaş {proposal.WeeklyWage} × {proposal.ContractYears} yıl.");
        }
        catch (TransferInvariantViolationException ex)
        {
            return UiActionResult.Fail($"Sözleşme teklifi sunulamadı: {ex.Message}");
        }
        catch (Exception ex)
        {
            return UiActionResult.Fail($"Sözleşme teklifi hatası: {ex.Message}");
        }
    }

    public UiActionResult AcceptPendingContractProposal()
    {
        try
        {
            var process = ResolveProcessForContractProposal(requireNegotiation: true);
            var proposal = Host.TransferModule.ContractProposals.AcceptPendingProposal(
                new TransferProcessId(process.ProcessId));
            return UiActionResult.Ok(
                $"Sözleşme kabul: #{proposal.ProposalId.Value}"
                + $" · maaş {proposal.WeeklyWage} × {proposal.ContractYears} yıl.");
        }
        catch (TransferInvariantViolationException ex)
        {
            return UiActionResult.Fail($"Sözleşme kabul edilemedi: {ex.Message}");
        }
        catch (Exception ex)
        {
            return UiActionResult.Fail($"Sözleşme kabul hatası: {ex.Message}");
        }
    }

    public UiActionResult RejectPendingContractProposal()
    {
        try
        {
            var process = ResolveProcessForContractProposal(requireNegotiation: true);
            var proposal = Host.TransferModule.ContractProposals.RejectPendingProposal(
                new TransferProcessId(process.ProcessId));
            return UiActionResult.Ok($"Sözleşme reddedildi: #{proposal.ProposalId.Value}.");
        }
        catch (TransferInvariantViolationException ex)
        {
            return UiActionResult.Fail($"Sözleşme reddedilemedi: {ex.Message}");
        }
        catch (Exception ex)
        {
            return UiActionResult.Fail($"Sözleşme ret hatası: {ex.Message}");
        }
    }

    public UiActionResult CounterPendingContractProposal()
    {
        try
        {
            var process = ResolveProcessForContractProposal(requireNegotiation: true);
            var day = Host.WorldModule.TimelineStore.Timeline.CurrentDate;
            var pending = Host.TransferModule.ProposalStore.GetForProcess(
                    new TransferProcessId(process.ProcessId))
                .LastOrDefault(p => p.IsPending);
            var wage = (pending?.WeeklyWage ?? 25_000) + 5_000;
            var years = pending?.ContractYears ?? 3;
            var proposal = Host.TransferModule.ContractProposals.CounterPendingProposal(
                new TransferProcessId(process.ProcessId),
                weeklyWage: wage,
                contractYears: years,
                day);
            return UiActionResult.Ok(
                $"Karşı sözleşme: #{proposal.ProposalId.Value} tur {proposal.Round}"
                + $" · maaş {proposal.WeeklyWage} × {proposal.ContractYears} yıl.");
        }
        catch (TransferInvariantViolationException ex)
        {
            return UiActionResult.Fail($"Karşı sözleşme başarısız: {ex.Message}");
        }
        catch (Exception ex)
        {
            return UiActionResult.Fail($"Karşı sözleşme hatası: {ex.Message}");
        }
    }

    private Application.Transfer.Queries.TransferProcessLineReadModel ResolveProcessForContractProposal(
        bool requireNegotiation = false)
    {
        var active = Host.TransferModule.Queries.GetManagedClubProcesses().ActiveProcesses;
        var process = (requireNegotiation
                ? active.Where(p => p.StatusCode == (int)TransferProcessStatus.PlayerNegotiation)
                : active.Where(p =>
                    p.StatusCode == (int)TransferProcessStatus.PlayerNegotiation
                    || p.StatusCode == (int)TransferProcessStatus.ClubAgreementReached
                    || (p.IsFreeAgent
                        && p.StatusCode == (int)TransferProcessStatus.SportingApproved)))
            .OrderBy(p => p.ProcessId)
            .FirstOrDefault()
            ?? throw new InvalidOperationException(
                requireNegotiation
                    ? "Oyuncu müzakeresindeki süreç yok."
                    : "Sözleşme teklifi için uygun süreç yok.");
        return process;
    }

    private static string TranslateProcessStatus(TransferProcessStatus status) =>
        status switch
        {
            TransferProcessStatus.UnderEvaluation => "Değerlendirmede",
            TransferProcessStatus.SportingApprovalPending => "Sportif onay bekliyor",
            TransferProcessStatus.SportingApproved => "Sportif onaylı",
            TransferProcessStatus.ClubNegotiation => "Kulüp müzakeresi",
            TransferProcessStatus.ClubAgreementReached => "Kulüp anlaşması",
            TransferProcessStatus.PlayerNegotiation => "Oyuncu müzakeresi",
            TransferProcessStatus.PlayerAgreementReached => "Oyuncu anlaşması",
            TransferProcessStatus.FinancialApprovalPending => "Mali onay bekliyor",
            TransferProcessStatus.FinancialApproved => "Mali onaylı",
            TransferProcessStatus.CompletionPending => "Tamamlanıyor",
            TransferProcessStatus.Completed => "Tamamlandı",
            TransferProcessStatus.Rejected => "Reddedildi",
            TransferProcessStatus.Withdrawn => "Geri çekildi",
            TransferProcessStatus.Failed => "Başarısız",
            TransferProcessStatus.Archived => "Arşiv",
            _ => status.ToString(),
        };

    public UiActionResult SetWeeklyTraining(TrainingIntensity intensity)
    {
        _trainingIntensity = intensity;
        return ApplyWeeklyTrainingPlan();
    }

    public UiActionResult SetWeeklyTrainingFocus(TrainingFocus focus)
    {
        _trainingFocus = focus;
        return ApplyWeeklyTrainingPlan();
    }

    public UiActionResult SetWeeklyTrainingRest(RestApproach rest)
    {
        _trainingRest = rest;
        return ApplyWeeklyTrainingPlan();
    }

    public UiActionResult ApplySuggestedPreparationPlan()
    {
        var suggestion = BuildPreparationBriefing().Suggestion;
        if (suggestion is null)
        {
            return UiActionResult.Fail("Uygulanacak hazırlık önerisi yok — Hazırlık Masası'na bak.");
        }

        _trainingIntensity = suggestion.Intensity;
        _trainingFocus = suggestion.Focus;
        _trainingRest = suggestion.Rest;
        return ApplyWeeklyTrainingPlan();
    }

    private UiActionResult ApplyWeeklyTrainingPlan()
    {
        try
        {
            var injuredBefore = SnapshotInjuredPlayerNames();
            var result = Host.TrainingModule.SetWeeklyPlan.Handle(
                new SetWeeklyTrainingPlanCommand(
                    Guid.NewGuid(),
                    (int)_trainingFocus,
                    (int)_trainingIntensity,
                    (int)_trainingRest));
            UpdateInjuryClearedCelebration(injuredBefore, SnapshotInjuredPlayerNames());

            var injuryText = result.InjuredSlotCount > 0
                ? $" · sakat {result.InjuredSlotCount}"
                : string.Empty;
            var invalidatedText = result.InvalidatedSelectionCount > 0
                ? $" · kadro onayı düştü ({result.InvalidatedSelectionCount})"
                : string.Empty;
            var advice = BuildPreparationBriefing().AdviceLine;
            var appliedLine =
                $"Antrenman uygulandı ({FormatTrainingFocus(_trainingFocus)}/{FormatTrainingIntensity(_trainingIntensity)}/{FormatRestApproach(_trainingRest)}):"
                + $" yorgunluk {result.AverageFatigue}, fitness {result.AverageFitness}{injuryText}{invalidatedText}."
                + $"\nÖneri: {advice}";

            if (_pendingInjuryClearedNames is { Count: > 0 })
            {
                var cleared = BuildInjuryClearedOffice();
                return UiActionResult.Ok(
                    cleared.Headline + "\n" + cleared.AdviceLine + "\n" + appliedLine,
                    narrativeBridgeLine: cleared.ToDisplayText(),
                    nextFocusCode: cleared.NextFocusCode);
            }

            if (_trainingFocus == TrainingFocus.Recovery)
            {
                var training = GetTrainingSummary();
                var day = Host.WorldModule.Queries.GetCurrentGameDate().DayNumber;
                var pending = Host.TeamPreparationModule.SelectionQueries
                    .GetNextDueManagedFixture(day);
                var confirmation = PostMatchOfficeDigest.AfterRecoveryApplied(
                    training.InjuredNames,
                    hasDueUnapprovedMatch: pending is { IsApproved: false },
                    hasDuePlayableMatch: pending is { IsApproved: true },
                    hasInjuryPressure: training.InjuredSlotCount > 0);
                return UiActionResult.Ok(
                    confirmation.Headline + "\n" + confirmation.AdviceLine + "\n" + appliedLine,
                    narrativeBridgeLine: confirmation.ToDisplayText(),
                    nextFocusCode: confirmation.NextFocusCode);
            }

            return UiActionResult.Ok(appliedLine);
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

    private static string FormatTrainingFocus(TrainingFocus focus) =>
        focus switch
        {
            TrainingFocus.General => "Genel",
            TrainingFocus.Fitness => "Kondisyon",
            TrainingFocus.Recovery => "Toparlanma",
            _ => focus.ToString(),
        };

    private static string FormatTrainingIntensity(TrainingIntensity intensity) =>
        intensity switch
        {
            TrainingIntensity.Low => "Hafif",
            TrainingIntensity.Medium => "Orta",
            TrainingIntensity.High => "Yoğun",
            _ => intensity.ToString(),
        };

    private static string FormatRestApproach(RestApproach rest) =>
        rest switch
        {
            RestApproach.Light => "Az dinlenme",
            RestApproach.Normal => "Normal dinlenme",
            RestApproach.Heavy => "Bol dinlenme",
            _ => rest.ToString(),
        };

    public MatchHalfTimeDigest BuildManagedHalfTimeDigest()
    {
        var competition = Host.CompetitionModule;
        var playHandler = competition.PlayFixtureMatch
            ?? throw new InvalidOperationException("Maç oynatma servisi bağlı değil.");
        var season = competition.Queries.GetCurrentSeason()
            ?? throw new InvalidOperationException("Aktif sezon yok.");
        var currentDay = Host.WorldModule.Queries.GetCurrentGameDate().DayNumber;
        var pending = Host.TeamPreparationModule.SelectionQueries
            .GetNextDueManagedFixture(currentDay);
        if (pending is null || !pending.IsApproved)
        {
            return MatchHalfTimeDigest.None();
        }

        var preview = playHandler.PreviewHalfTime(
            season.SeasonId,
            pending.FixtureId,
            currentDay);
        var momentLines = preview.FirstHalfMoments
            .Take(4)
            .Select(moment => FormatKeyMomentLine(
                moment,
                moment.IsHomeSide ? "Ev" : "Dep"))
            .ToArray();
        var digest = MatchHalfTimeDigest.Compose(
            preview.HomeClubName,
            preview.AwayClubName,
            preview.HomeGoals,
            preview.AwayGoals,
            preview.ManagedIsHome,
            momentLines);
        var strip = BuildMatchDayLineupStrip();
        if (strip.OutPlayers.Count > 0)
        {
            var outs = string.Join(
                ", ",
                strip.OutPlayers.Take(2).Select(c => c.DisplayName));
            return digest with
            {
                BeatLines = digest.BeatLines
                    .Append($"Sakat dışarıda: {outs} — değişiklik düşün.")
                    .ToArray(),
            };
        }

        return digest;
    }

    public PlayMatchesUiResult PlayDueMatches(
        int managedSecondHalfDelta = 0,
        MatchHalfTimeDigest? halfTime = null,
        string? halfTimeDecisionLabel = null,
        string? halfTimeSubstitutionLabel = null)
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
                    Array.Empty<string>(),
                    Narrative: MatchNightNarrative.Failure(
                        "Önce kendi maçın için kadroyu onayla."));
            }

            var injuredBefore = SnapshotInjuredPlayerNames();
            var formBefore = BuildDressingRoomEcho();
            var opponentBefore = pendingSelection is not null
                ? BuildOpponentDossier()
                : null;
            var kickoffBriefing = CaptureKickoffBriefing(currentDay, pendingSelection);
            var matchupPlan = pendingSelection is not null
                ? BuildMatchupPlan()
                : null;
            var kickoffLines = kickoffBriefing.ToKickoffBridgeLines().ToList();
            if (halfTime is { HasManagedMatch: true })
            {
                kickoffLines.Add($"Devre arası: {halfTime.Scoreline}");
            }

            if (!string.IsNullOrWhiteSpace(halfTimeDecisionLabel))
            {
                kickoffLines.Add(halfTimeDecisionLabel);
            }

            if (!string.IsNullOrWhiteSpace(halfTimeSubstitutionLabel))
            {
                kickoffLines.Add(halfTimeSubstitutionLabel);
            }

            // Maç sonrası sakatlık invalidate etmeden önce XI köprüsünü yakala.
            var lineupBridge = pendingSelection is not null
                ? BuildMatchDayLineupStrip()
                : MatchDayLineupStrip.Clear();

            var enteredWithPromiseRisk = kickoffBriefing.HasPromiseRisk;

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
                    Array.Empty<string>(),
                    Narrative: MatchNightNarrative.Failure(
                        "Bu gece sahada maç yok — tarihi ilerlet."));
            }

            var lines = new List<string>(dueFixtures.Length);
            var consequenceLines = new List<string>();
            var keyMomentLines = new List<string>();
            var invalidatedTotal = 0;

            string? heroScoreline = null;
            int heroHomeGoals = 0;
            int heroAwayGoals = 0;
            var heroManagedIsHome = true;
            var hasManaged = false;
            string? heroTacticNote = null;
            var beatLines = new List<string>();
            var afterWhistle = new List<string>();
            var otherScores = new List<string>();
            MatchReportDigest? heroReport = null;
            TechnicalAreaDigest? technicalArea = null;
            MatchupPlanOutcomeDigest? matchupPlanOutcome = null;

            var managedClubId = Host.ManagerModule.Queries.GetCareer().EmployedClubId;

            var beforeStandings = competition.Queries.GetStandings(season.SeasonId);
            string? beforeLeaderName = beforeStandings.Count > 0
                ? GetClubDisplayName(beforeStandings[0].ClubId)
                : null;
            var beforeManagedRank = FindStandingRank(beforeStandings, managedClubId);

            foreach (var fixture in dueFixtures)
            {
                var isManagedFixture = managedClubId is long clubId
                    && (fixture.HomeClubId == clubId || fixture.AwayClubId == clubId);
                var result = playHandler.Handle(
                    new PlayFixtureMatchCommand(
                        Guid.NewGuid(),
                        season.SeasonId,
                        fixture.FixtureId,
                        currentDay,
                        ManagedSecondHalfDelta: isManagedFixture ? managedSecondHalfDelta : 0,
                        ForcedHalfTimeHomeGoals: isManagedFixture && halfTime is { HasManagedMatch: true }
                            ? halfTime.HomeGoals
                            : null,
                        ForcedHalfTimeAwayGoals: isManagedFixture && halfTime is { HasManagedMatch: true }
                            ? halfTime.AwayGoals
                            : null));

                var home = GetClubDisplayName(fixture.HomeClubId);
                var away = GetClubDisplayName(fixture.AwayClubId);
                var scoreline = $"{home} {result.HomeGoals}-{result.AwayGoals} {away}";
                var matchImpact = new[]
                    {
                        result.ManagedTacticModifier is int tacticMod
                            ? $"taktik {FormatSigned(tacticMod)}"
                            : null,
                        result.ManagedLineupRoleModifier is int lineupMod
                            ? $"kadro {FormatSigned(lineupMod)}"
                            : null,
                    }
                    .Where(note => note is not null)
                    .ToArray();
                lines.Add(matchImpact.Length == 0
                    ? scoreline
                    : $"{scoreline} · {string.Join(" · ", matchImpact)}");
                invalidatedTotal += result.InvalidatedSelectionCount;
                consequenceLines.AddRange(FormatMatchConsequences(result, home, away));
                keyMomentLines.AddRange(FormatMatchKeyMoments(result, home, away));

                var isManaged = result.Consequences is { IsManagedMatch: true }
                    && managedClubId is long managedId
                    && (fixture.HomeClubId == managedId || fixture.AwayClubId == managedId);

                if (isManaged && !hasManaged)
                {
                    hasManaged = true;
                    heroScoreline = scoreline;
                    heroHomeGoals = result.HomeGoals;
                    heroAwayGoals = result.AwayGoals;
                    heroManagedIsHome = fixture.HomeClubId == managedClubId;
                    heroTacticNote = matchImpact.Length == 0
                        ? null
                        : string.Join(" · ", matchImpact);
                    var halfTimeDecisionMoment = MatchHalfTimeDigest
                        .FormatDecisionKeyMoment(halfTimeDecisionLabel);
                    var halfTimeSubstitutionMoment = SelectionAutoSwapWarning
                        .FormatHalfTimeKeyMomentFromBridge(halfTimeSubstitutionLabel);
                    var secondHalfExtras = new List<string>();
                    if (!string.IsNullOrWhiteSpace(halfTimeDecisionMoment))
                    {
                        secondHalfExtras.Add(halfTimeDecisionMoment);
                        keyMomentLines.Add($"{scoreline} · {halfTimeDecisionMoment}");
                    }

                    if (!string.IsNullOrWhiteSpace(halfTimeSubstitutionMoment))
                    {
                        secondHalfExtras.Add(halfTimeSubstitutionMoment);
                        keyMomentLines.Add($"{scoreline} · {halfTimeSubstitutionMoment}");
                    }

                    beatLines.AddRange(FormatMatchMomentBeatsByHalf(
                        result,
                        heroManagedIsHome,
                        secondHalfExtras));

                    afterWhistle.AddRange(FormatMatchAfterWhistle(result));
                    if (kickoffBriefing.HasCleanReturn)
                    {
                        var cleanMargin = heroManagedIsHome
                            ? result.HomeGoals - result.AwayGoals
                            : result.AwayGoals - result.HomeGoals;
                        var cleanBeat = PostMatchOfficeDigest.FormatCleanReturnVerdictBeat(
                            lineupBridge,
                            cleanMargin);
                        if (!string.IsNullOrWhiteSpace(cleanBeat))
                        {
                            afterWhistle.Insert(0, cleanBeat);
                            _pendingWeekStoryClosure = cleanBeat;
                            _weekStoryClosureDismissOnNextAdvance = false;
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(halfTimeSubstitutionLabel))
                    {
                        afterWhistle.Insert(0, halfTimeSubstitutionLabel);
                    }

                    if (!string.IsNullOrWhiteSpace(halfTimeDecisionLabel))
                    {
                        afterWhistle.Insert(0, halfTimeDecisionLabel);
                    }

                    heroReport = MatchReportDigest.Compose(
                        result,
                        home,
                        away,
                        halfTimeDecisionLabel,
                        halfTimeSubstitutionLabel);
                    technicalArea = TechnicalAreaDigest.Compose(
                        halfTime,
                        result.HomeGoals,
                        result.AwayGoals,
                        managedSecondHalfDelta);
                    matchupPlanOutcome = matchupPlan is not null
                        ? MatchupPlanOutcomeDigest.Compose(
                            matchupPlan,
                            halfTime,
                            result,
                            heroManagedIsHome)
                        : null;
                    if (matchupPlanOutcome is not null)
                    {
                        afterWhistle.Insert(0, matchupPlanOutcome.SummaryLine);
                        RememberMatchupPlanOutcome(
                            currentDay,
                            heroManagedIsHome ? away : home,
                            matchupPlan!,
                            matchupPlanOutcome);
                    }
                }
                else
                {
                    otherScores.Add(scoreline);
                }
            }

            if (invalidatedTotal > 0)
            {
                consequenceLines.Add($"Kadro onayı düştü ({invalidatedTotal}).");
                afterWhistle.Add($"Kadro onayı düştü ({invalidatedTotal}).");
            }

            if (!hasManaged && lines.Count > 0)
            {
                heroScoreline = lines[0].Split(" · ")[0];
                otherScores = lines.Skip(1).Select(l => l.Split(" · ")[0]).ToList();
            }

            var afterStandings = competition.Queries.GetStandings(season.SeasonId);
            string? afterLeaderName = afterStandings.Count > 0
                ? GetClubDisplayName(afterStandings[0].ClubId)
                : null;
            int? afterLeaderPoints = afterStandings.Count > 0
                ? afterStandings[0].Points
                : null;
            var afterManagedRank = FindStandingRank(afterStandings, managedClubId);
            var relegationZone = afterStandings.Count >= 3
                ? afterStandings
                    .Skip(Math.Max(0, afterStandings.Count - 2))
                    .Select(entry => GetClubDisplayName(entry.ClubId))
                    .ToArray()
                : Array.Empty<string>();
            var nextPending = Host.TeamPreparationModule.SelectionQueries
                .GetNextDueManagedFixture(currentDay);
            var roundup = LeagueRoundupDigest.Compose(
                beforeLeaderName,
                afterLeaderName,
                afterLeaderPoints,
                beforeManagedRank,
                afterManagedRank,
                relegationZone,
                managedClubId is long employedId ? GetClubDisplayName(employedId) : null,
                otherScores,
                nextPending is not null
                    ? GetClubDisplayName(nextPending.OpponentClubId)
                    : null);

            int? managedMargin = null;
            if (hasManaged)
            {
                managedMargin = heroManagedIsHome
                    ? heroHomeGoals - heroAwayGoals
                    : heroAwayGoals - heroHomeGoals;
            }

            var stadium = hasManaged
                ? StadiumAtmosphereDigest.Compose(
                    heroManagedIsHome,
                    beforeManagedRank,
                    season.ParticipantCount)
                : null;

            var dressingRoom = CaptainReactionDigest.Compose(
                managedMargin,
                afterWhistle.Any(line =>
                    line.Contains("işten çıkardı", StringComparison.OrdinalIgnoreCase)));
            var formStreakVerdict = FormStreakVerdictDigest.Compose(
                formBefore?.MomentumCode,
                managedMargin,
                formBefore?.MomentumLength ?? 0,
                opponentBefore?.WinningStreakLength ?? 0,
                opponentBefore?.LosingStreakLength ?? 0);

            var narrative = MatchNightNarrative.Compose(
                heroScoreline ?? "—",
                heroHomeGoals,
                heroAwayGoals,
                heroManagedIsHome,
                hasManaged,
                heroTacticNote,
                currentDay,
                beatLines,
                afterWhistle,
                otherScores,
                hasManaged ? kickoffLines : Array.Empty<string>(),
                hasManaged && enteredWithPromiseRisk,
                hasManaged ? lineupBridge : null,
                stadium);

            var invalidatedNote = invalidatedTotal > 0
                ? $" · kadro onayı düştü ({invalidatedTotal})"
                : string.Empty;
            UpdateInjuryClearedCelebration(injuredBefore, SnapshotInjuredPlayerNames());
            if (hasManaged && !kickoffBriefing.HasInjuryPressure)
            {
                _pendingCleanXiBridgeNames = null;
            }

            return new PlayMatchesUiResult(
                true,
                $"{lines.Count} maç oynandı ({GameDate.FromDayNumber(currentDay).ToIsoDateString()}){invalidatedNote}.",
                lines,
                consequenceLines,
                keyMomentLines,
                narrative,
                heroReport,
                roundup,
                dressingRoom,
                technicalArea,
                matchupPlanOutcome,
                formStreakVerdict);
        }
        catch (TeamPreparationInvariantViolationException ex)
        {
            return new PlayMatchesUiResult(
                false,
                $"Kadro engeli: {ex.Message}",
                Array.Empty<string>(),
                Narrative: MatchNightNarrative.Failure($"Kadro engeli: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return new PlayMatchesUiResult(
                false,
                $"Maç oynatma hatası: {ex.Message}",
                Array.Empty<string>(),
                Narrative: MatchNightNarrative.Failure($"Maç oynatılamadı: {ex.Message}"));
        }
    }

    private static IEnumerable<string> FormatMatchMomentBeatsByHalf(
        PlayFixtureMatchResult result,
        bool managedIsHome,
        IReadOnlyList<string>? secondHalfExtras = null)
    {
        var firstHalf = new List<string>();
        var secondHalf = new List<string>();
        var crowd = CrowdReactionDigest.Compose(managedIsHome, result.KeyMoments);
        var reactionsByMinute = crowd.MomentBeats.ToLookup(beat => beat.Minute);
        foreach (var moment in result.KeyMoments ?? Array.Empty<MatchKeyMomentReadModel>())
        {
            var side = moment.IsHomeSide ? "Ev" : "Dep";
            var line = FormatKeyMomentLine(moment, side);
            var half = moment.Minute <= MvpFixtureMatchSimulator.HalfTimeMinute
                ? firstHalf
                : secondHalf;
            half.Add(line);
            half.AddRange(reactionsByMinute[moment.Minute].Select(beat => beat.Line));
        }

        firstHalf.Add(crowd.HalfTimeBeat);

        foreach (var beat in MatchNightNarrative.ComposeHalfSegmentedBeats(
            firstHalf,
            secondHalf,
            secondHalfExtras))
        {
            yield return beat;
        }
    }

    private static IEnumerable<string> FormatMatchAfterWhistle(PlayFixtureMatchResult result)
    {
        if (result.Consequences is not { IsManagedMatch: true } c)
        {
            yield break;
        }

        if (c.BoardConfidenceDelta is int delta && c.BoardConfidenceAfter is int after)
        {
            var risk = c.BoardRiskBand switch
            {
                nameof(EmploymentRiskBand.Secure) => "Güvenli",
                nameof(EmploymentRiskBand.Stable) => "Stabil",
                nameof(EmploymentRiskBand.UnderReview) => "İncelemede",
                nameof(EmploymentRiskBand.Critical) => "Kritik",
                _ => c.BoardRiskBand ?? "-",
            };
            yield return $"Yönetim güveni {FormatSigned(delta)} → {after} ({risk})";
        }

        if (c.ManagerDismissed)
        {
            yield return "Yönetim seni işten çıkardı.";
        }

        if (c.NewlyInjuredSlots.Count > 0)
        {
            var named = result.KeyMoments?
                .Where(moment =>
                    string.Equals(moment.Kind, nameof(MatchKeyMomentKind.Injury), StringComparison.Ordinal))
                .Select(moment => FormatPlayerRef(moment.PrimaryPlayerName, moment.PrimarySlotIndex))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            yield return named is { Length: > 0 }
                ? $"Sakatlık: {string.Join(", ", named)}"
                : $"Sakatlık: slot {string.Join(", ", c.NewlyInjuredSlots)}";
        }

        if (c.PressQuestionOpened)
        {
            yield return "Basın sorusu açıldı.";
        }

        if (c.PlayingTimeDemandOpened)
        {
            yield return "Forma süresi talebi açıldı.";
        }

        if (c.BoardDemandOpened)
        {
            yield return "Yönetim talebi açıldı.";
        }

        if (c.DisciplineOpened)
        {
            var named = result.KeyMoments?
                .Where(moment =>
                    string.Equals(moment.Kind, nameof(MatchKeyMomentKind.RedCard), StringComparison.Ordinal))
                .Select(moment => FormatPlayerRef(moment.PrimaryPlayerName, moment.PrimarySlotIndex))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            yield return named is { Length: > 0 }
                ? $"Disiplin görüşmesi açıldı — {named[0]} kırmızı gördü."
                : "Disiplin görüşmesi açıldı.";
        }
    }

    private static IEnumerable<string> FormatMatchKeyMoments(
        PlayFixtureMatchResult result,
        string home,
        string away)
    {
        if (result.Consequences is not { IsManagedMatch: true })
        {
            yield break;
        }

        if (result.KeyMoments is null || result.KeyMoments.Count == 0)
        {
            yield break;
        }

        var header = $"{home} {result.HomeGoals}-{result.AwayGoals} {away}";
        foreach (var moment in result.KeyMoments)
        {
            var side = moment.IsHomeSide ? "Ev" : "Dep";
            yield return $"{header} · {FormatKeyMomentLine(moment, side)}";
        }
    }

    private static string FormatKeyMomentLine(MatchKeyMomentReadModel moment, string side)
    {
        var primary = FormatPlayerRef(moment.PrimaryPlayerName, moment.PrimarySlotIndex);
        return moment.Kind switch
        {
            nameof(MatchKeyMomentKind.Goal) when moment.AssistSlotIndex is int assist =>
                $"{moment.Minute}' {side} gol · {primary} (asist {FormatPlayerRef(moment.AssistPlayerName, assist)})",
            nameof(MatchKeyMomentKind.Goal) =>
                $"{moment.Minute}' {side} gol · {primary}",
            nameof(MatchKeyMomentKind.YellowCard) =>
                $"{moment.Minute}' {side} sarı kart · {primary}",
            nameof(MatchKeyMomentKind.RedCard) =>
                $"{moment.Minute}' {side} kırmızı kart · {primary}",
            nameof(MatchKeyMomentKind.Injury) =>
                $"{moment.Minute}' {side} sakatlık · {primary}",
            _ => $"{moment.Minute}' {side} {moment.Kind} · {primary}",
        };
    }

    private static string FormatPlayerRef(string? playerName, int slotIndex) =>
        string.IsNullOrWhiteSpace(playerName) ? $"slot {slotIndex}" : playerName;

    private static IEnumerable<string> FormatMatchConsequences(
        PlayFixtureMatchResult result,
        string home,
        string away)
    {
        if (result.Consequences is not { IsManagedMatch: true } c)
        {
            yield break;
        }

        var header = $"{home} {result.HomeGoals}-{result.AwayGoals} {away}";
        if (c.BoardConfidenceDelta is int delta && c.BoardConfidenceAfter is int after)
        {
            var risk = c.BoardRiskBand switch
            {
                nameof(EmploymentRiskBand.Secure) => "Güvenli",
                nameof(EmploymentRiskBand.Stable) => "Stabil",
                nameof(EmploymentRiskBand.UnderReview) => "İncelemede",
                nameof(EmploymentRiskBand.Critical) => "Kritik",
                _ => c.BoardRiskBand ?? "-",
            };
            yield return $"{header} · yönetim güveni {FormatSigned(delta)} → {after} ({risk})";
        }

        if (c.ManagerDismissed)
        {
            yield return $"{header} · yönetim seni işten çıkardı.";
        }

        if (c.NewlyInjuredSlots.Count > 0)
        {
            var named = result.KeyMoments?
                .Where(moment =>
                    string.Equals(moment.Kind, nameof(MatchKeyMomentKind.Injury), StringComparison.Ordinal))
                .Select(moment => FormatPlayerRef(moment.PrimaryPlayerName, moment.PrimarySlotIndex))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            yield return named is { Length: > 0 }
                ? $"{header} · sakatlık: {string.Join(", ", named)}"
                : $"{header} · sakatlık: slot {string.Join(", ", c.NewlyInjuredSlots)}";
        }

        if (c.PressQuestionOpened)
        {
            yield return $"{header} · basın sorusu açıldı.";
        }

        if (c.PlayingTimeDemandOpened)
        {
            yield return c.PlayingTimeDemandPlayerId is long pid
                ? $"{header} · forma süresi talebi açıldı (#{pid})."
                : $"{header} · forma süresi talebi açıldı.";
        }

        if (c.BoardDemandOpened)
        {
            yield return $"{header} · yönetim talebi açıldı.";
        }

        if (c.DisciplineOpened)
        {
            var named = result.KeyMoments?
                .Where(moment =>
                    string.Equals(moment.Kind, nameof(MatchKeyMomentKind.RedCard), StringComparison.Ordinal))
                .Select(moment => FormatPlayerRef(moment.PrimaryPlayerName, moment.PrimarySlotIndex))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            yield return named is { Length: > 0 }
                ? $"{header} · disiplin görüşmesi açıldı — {named[0]} kırmızı gördü."
                : $"{header} · disiplin görüşmesi açıldı.";
        }
    }

    public UiActionResult AdvanceDays(int dayCount)
    {
        try
        {
            var injuredBefore = SnapshotInjuredPlayerNames();
            var world = Host.WorldModule;
            var current = world.Queries.GetCurrentGameDate();
            var moodBefore = BuildWeekMood();
            var noteBefore = OfficeCalmNote.Resolve(moodBefore.MoodCode, current.DayNumber);
            var result = world.AdvanceSimulationTime.Handle(
                new AdvanceSimulationTimeCommand(Guid.NewGuid(), current.DayNumber + dayCount));

            if (result.WasBlocked)
            {
                var blocked = FormatBlockers(result.Blockers.Select(b =>
                    (b.SourceContext, b.DescriptionCode)));
                return UiActionResult.Fail(
                    TimeAdvanceDigest.Blocked(blocked).ToStatusMessage());
            }

            var day = Host.WorldModule.TimelineStore.Timeline.CurrentDate;
            Host.TeamPreparationModule.ClubSquad?.SyncClubs(result.ContractExpiryAffectedClubIds, day);
            if (Host.ManagerModule.Queries.GetCareer().EmployedClubId is long clubId)
            {
                var id = new ClubId(clubId);
                Host.PlayerCareerModule.Development.EnsureClub(
                    id,
                    Host.WorldModule.TimelineStore.Timeline.RootSeed,
                    day);
                Host.TeamPreparationModule.ClubSquad?.SyncFromActiveContracts(id, day);
                Host.TeamPreparationModule.TacticPlans.EnsureDefault(id, day);
            }

            RecoverManagedInjuriesToCurrentDate();
            UpdateInjuryClearedCelebration(injuredBefore, SnapshotInjuredPlayerNames());

            var nextHint = BuildNextMatchHint(day.DayNumber);
            var digest = TimeAdvanceDigest.Compose(result, dayCount, nextHint);
            if (_pendingInjuryClearedNames is { Count: > 0 })
            {
                var office = BuildInjuryClearedOffice();
                return UiActionResult.Ok(
                    digest.ToStatusMessage() + "\n" + office.Headline,
                    digest,
                    narrativeBridgeLine: office.ToDisplayText(),
                    nextFocusCode: office.NextFocusCode);
            }

            var moodAfter = BuildWeekMood();
            var tempoOffice = PostMatchOfficeDigest.AfterMoodTempoShift(
                moodBefore.MoodCode,
                moodAfter);
            if (tempoOffice is not null)
            {
                return UiActionResult.Ok(
                    digest.ToStatusMessage() + "\n" + tempoOffice.Headline,
                    digest,
                    narrativeBridgeLine: tempoOffice.ToDisplayText(),
                    nextFocusCode: tempoOffice.NextFocusCode);
            }

            var noteAfter = OfficeCalmNote.Resolve(moodAfter.MoodCode, day.DayNumber);
            var noteConfirm = OfficeCalmNote.ToAdvanceConfirmation(noteBefore, noteAfter);
            var message = digest.ToStatusMessage();
            if (!string.IsNullOrWhiteSpace(noteConfirm))
            {
                message += "\n" + noteConfirm;
            }

            if (!string.IsNullOrWhiteSpace(noteAfter) && !string.IsNullOrWhiteSpace(noteConfirm))
            {
                var office = PostMatchOfficeDigest.AfterCalmNoteAdvance(noteAfter, noteBefore);
                return UiActionResult.Ok(
                    message,
                    digest,
                    narrativeBridgeLine: office.ToDisplayText(),
                    nextFocusCode: office.NextFocusCode);
            }

            return UiActionResult.Ok(message, digest);
        }
        catch (TeamPreparationInvariantViolationException ex)
        {
            return UiActionResult.Fail($"Gün ilerletilemedi (kadro): {ex.Message}");
        }
        catch (Exception ex)
        {
            return UiActionResult.Fail($"Gün ilerletilemedi: {ex.Message}");
        }
    }

    private PreMatchBriefing CaptureKickoffBriefing(
        int currentDayNumber,
        ManagedFixtureSelectionStatusReadModel? pending) =>
        ComposePreMatchBriefing(currentDayNumber, pending);

    private PreMatchBriefing ComposePreMatchBriefing(
        int currentDayNumber,
        ManagedFixtureSelectionStatusReadModel? pending)
    {
        if (pending is null)
        {
            return PreMatchBriefing.Clear();
        }

        var tension = Host.TeamPreparationModule.PromiseTension
            .GetForNextDueMatch(currentDayNumber);
        var tactic = Host.TeamPreparationModule.TacticQueries.GetManagedClubPlan();
        var training = GetTrainingSummary();
        var briefing = PreMatchBriefing.Compose(
            pending,
            GetClubDisplayName(pending.OpponentClubId),
            currentDayNumber,
            tactic.FormationName,
            tactic.ApproachName,
            training.HasPlan ? training.AverageFatigue : null,
            training.HasPlan ? training.AverageFitness : null,
            training.InjuredSlotCount,
            tension,
            training.InjuredNames,
            BuildAutoSwapWarningLines(pending.ManagedClubId),
            cleanReturnNames: ResolveCleanXiBridgeNames(training.InjuredSlotCount));
        var notebook = BuildTechnicalDirectorNotebook();
        var beats = notebook.HasHistory
            ? briefing.BeatLines.Concat(notebook.BeatLines).ToList()
            : briefing.BeatLines.ToList();
        var matchupPlan = BuildMatchupPlan();
        var patternWarning = RepeatedPatternWarningDigest.Compose(
            matchupPlan,
            _matchupPlanHistory);
        if (patternWarning.HasWarning)
        {
            beats.Add(patternWarning.WarningLine);
        }

        return briefing with { BeatLines = beats };
    }

    public PlayerManagementDigest BuildPlayerManagementDigest()
    {
        var manager = Host.ManagerModule.Queries.GetCareer();
        if (manager.EmployedClubId is not long clubId)
        {
            return PlayerManagementDigest.Clear();
        }

        var timeline = Host.WorldModule.TimelineStore.Timeline;
        var id = new Domain.Shared.ClubId(clubId);
        Host.TeamPreparationModule.ClubSquad?.SyncFromActiveContracts(id, timeline.CurrentDate);

        return PlayerManagementDigest.Compose(
            id,
            manager.ManagerId,
            timeline.CurrentDate,
            MvpSquadRosterGenerator.GeneratePlayerProfiles(id, timeline.RootSeed),
            Host.TeamPreparationModule.SquadQueries.GetClubSquad(clubId, timeline.RootSeed),
            Host.TeamPreparationModule.SquadStore.Get(id),
            Host.PlayerCareerModule.Store.Careers,
            Host.TrainingModule.Store.PhysicalBySlot,
            Host.ContractModule.Store.Contracts,
            Host.SocialContinuityModule.RelationshipStore.Relationships,
            Host.SocialContinuityModule.PromiseStore.Promises,
            Host.SocialContinuityModule.MemoryStore.Memories);
    }

    public CareerLegacyDigest BuildCareerLegacyDigest()
    {
        var career = Host.ManagerModule.Store.Career;
        if (career.ActiveEmployment is not { } employment)
        {
            return CareerLegacyDigest.Empty();
        }

        var timeline = Host.WorldModule.TimelineStore.Timeline;
        var clubId = employment.ClubId;
        var seasons = Host.CompetitionModule.Store.League.Seasons
            .Where(season =>
                season.PreseasonStartDate.DayNumber >= employment.StartedAt.DayNumber
                || season.Fixtures.Any(fixture =>
                    fixture.ScheduledDate.DayNumber >= employment.StartedAt.DayNumber
                    && (fixture.HomeClubId == clubId || fixture.AwayClubId == clubId)))
            .Select(season =>
            {
                var standings = season.Standings.Entries.ToArray();
                var standing = standings.FirstOrDefault(entry => entry.ClubId == clubId);
                var rank = standing is null
                    ? 0
                    : Array.FindIndex(standings, entry => entry.ClubId == clubId) + 1;
                var fixtures = season.Fixtures.Where(fixture =>
                    fixture.Status == FixtureStatus.ResultAccepted
                    && fixture.ScheduledDate.DayNumber >= employment.StartedAt.DayNumber
                    && (fixture.HomeClubId == clubId || fixture.AwayClubId == clubId))
                    .ToArray();
                var results = fixtures.Select(fixture =>
                {
                    var goalsFor = fixture.HomeClubId == clubId
                        ? fixture.HomeGoals!.Value
                        : fixture.AwayGoals!.Value;
                    var goalsAgainst = fixture.HomeClubId == clubId
                        ? fixture.AwayGoals!.Value
                        : fixture.HomeGoals!.Value;
                    return (GoalsFor: goalsFor, GoalsAgainst: goalsAgainst);
                }).ToArray();
                var won = results.Count(result => result.GoalsFor > result.GoalsAgainst);
                var drawn = results.Count(result => result.GoalsFor == result.GoalsAgainst);
                var lost = results.Length - won - drawn;
                return new CareerSeasonLegacySource(
                    season.SeasonId.Value,
                    season.Status.ToString(),
                    rank,
                    standings.Length,
                    results.Length,
                    won,
                    drawn,
                    lost,
                    won * 3 + drawn,
                    results.Sum(result => result.GoalsFor),
                    results.Sum(result => result.GoalsAgainst));
            }).ToArray();
        var squadCareers = Host.PlayerCareerModule.Store.Careers
            .Where(player => player.OriginClubId == clubId)
            .ToArray();
        var expiringLimit = timeline.CurrentDate.AddDays(365).DayNumber;
        var expiring = Host.ContractModule.Store.GetForClub(clubId).Count(contract =>
            contract.IsActiveOn(timeline.CurrentDate)
            && contract.EndDate.DayNumber <= expiringLimit);

        return CareerLegacyDigest.Compose(
            career.DisplayName,
            GetClubDisplayName(clubId.Value),
            Math.Max(0, timeline.CurrentDate.DayNumber - employment.StartedAt.DayNumber),
            career.Reputation.Value,
            employment.BoardConfidence.Value,
            squadCareers.Count(player => player.LastDevelopedOn is not null || player.DevelopmentPoints > 0),
            squadCareers.Length == 0
                ? 0
                : (int)Math.Round(squadCareers.Average(player => player.AgeYears(timeline.CurrentDate))),
            expiring,
            seasons);
    }

    public TacticPlanReadModel GetManagedTacticPlan() =>
        Host.TeamPreparationModule.TacticQueries.GetManagedClubPlan();

    public UiActionResult SetTacticPressing(PressingIntensity pressing) =>
        UpdateTeamInstruction(
            (clubId, day) => Host.TeamPreparationModule.TacticPlans.SetPressing(clubId, pressing, day),
            "Pres");

    public UiActionResult SetTacticDefensiveLine(DefensiveLine defensiveLine) =>
        UpdateTeamInstruction(
            (clubId, day) => Host.TeamPreparationModule.TacticPlans.SetDefensiveLine(
                clubId,
                defensiveLine,
                day),
            "Savunma hattı");

    public UiActionResult SetTacticPassingStyle(PassingStyle passingStyle) =>
        UpdateTeamInstruction(
            (clubId, day) => Host.TeamPreparationModule.TacticPlans.SetPassingStyle(
                clubId,
                passingStyle,
                day),
            "Pas stili");

    private UiActionResult UpdateTeamInstruction(
        Action<Domain.Shared.ClubId, Domain.WorldCalendar.GameDate> update,
        string label)
    {
        try
        {
            var clubId = Host.ManagerModule.Queries.GetCareer().EmployedClubId
                ?? throw new InvalidOperationException("Menajer kulübü yok.");
            var day = Host.WorldModule.TimelineStore.Timeline.CurrentDate;
            update(new Domain.Shared.ClubId(clubId), day);
            var view = GetManagedTacticPlan();
            var plan = Host.TeamPreparationModule.TacticPlanStore.Get(new Domain.Shared.ClubId(clubId));
            var tacticMod = MvpTacticMatchModifier.ComputeTacticModifier(plan);
            return UiActionResult.Ok(
                $"{label}: {view.PressingName} · {view.DefensiveLineName} · {view.PassingStyleName}"
                + $" · maç {FormatSigned(tacticMod)}.");
        }
        catch (TeamPreparationInvariantViolationException ex)
        {
            return UiActionResult.Fail($"Takım talimatı ayarlanamadı: {ex.Message}");
        }
        catch (Exception ex)
        {
            return UiActionResult.Fail($"Taktik hatası: {ex.Message}");
        }
    }

    private void RememberMatchupPlanOutcome(
        int dayNumber,
        string opponentName,
        MatchupPlanDigest plan,
        MatchupPlanOutcomeDigest outcome)
    {
        var entry = MatchupPlanNotebookEntry.Compose(
            dayNumber,
            opponentName,
            plan.SelectionLine,
            plan.ThreatKind,
            plan.Signal,
            outcome.Signal,
            outcome.VerdictLine);
        _matchupPlanHistory.RemoveAll(existing =>
            existing.DayNumber == entry.DayNumber
            && string.Equals(existing.OpponentName, entry.OpponentName, StringComparison.Ordinal)
            && string.Equals(existing.SelectionLine, entry.SelectionLine, StringComparison.Ordinal));
        _matchupPlanHistory.Add(entry);

        var recent = _matchupPlanHistory
            .OrderByDescending(existing => existing.DayNumber)
            .Take(MatchupPlanNotebookEntry.HistoryLimit)
            .OrderBy(existing => existing.DayNumber)
            .ToArray();
        _matchupPlanHistory.Clear();
        _matchupPlanHistory.AddRange(recent);
    }

    private IReadOnlyList<string> BuildAutoSwapWarningLines(long managedClubId)
    {
        var timeline = Host.WorldModule.TimelineStore.Timeline;
        var clubId = new Domain.Shared.ClubId(managedClubId);
        var squad = Host.TeamPreparationModule.SquadStore.Get(clubId);
        if (!Simulation.TrainingPhysicalState.MvpAvailabilityAwareSelection
                .TryPreviewPreferredStartingXi(
                    clubId,
                    timeline.CurrentDate,
                    Host.TrainingModule.Store.PhysicalBySlot,
                    squad,
                    out _,
                    out var swaps)
            || swaps.Count == 0)
        {
            return Array.Empty<string>();
        }

        var names = MvpSquadRosterGenerator.GeneratePlayerNames(clubId, timeline.RootSeed);
        return SelectionAutoSwapWarning.FormatBeatLines(swaps, names);
    }

    private string? BuildNextMatchHint(int currentDayNumber)
    {
        var pending = Host.TeamPreparationModule.SelectionQueries
            .GetNextDueManagedFixture(currentDayNumber);
        if (pending is null)
        {
            return null;
        }

        var briefing = ComposePreMatchBriefing(currentDayNumber, pending);
        return $"{briefing.FixtureLine} — {briefing.Headline}";
    }

    public PostMatchOfficeDigest BuildPostMatchOfficeReturn(PlayMatchesUiResult results)
    {
        ArgumentNullException.ThrowIfNull(results);
        var desk = BuildDecisionDeskDigest();
        var hasManaged = results.Narrative is not null
            && string.Equals(results.Narrative.BrandTitle, "Maç Gecesi", StringComparison.Ordinal);
        return PostMatchOfficeDigest.Compose(
            results.Narrative,
            desk,
            hasManaged,
            BuildTodayPulse(),
            halfTimeNoteLine: results.Report?.HalfTimeNoteLine,
            freshlyRecoveredNames: _pendingInjuryClearedNames);
    }

    private IReadOnlyList<string> SnapshotInjuredPlayerNames() =>
        GetTrainingSummary().InjuredNames.ToArray();

    private void UpdateInjuryClearedCelebration(
        IReadOnlyList<string> before,
        IReadOnlyList<string> after)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);

        if (after.Count > 0)
        {
            _pendingInjuryClearedNames = null;
            _pendingCleanXiBridgeNames = null;
            ClearWeekStoryClosure();
            return;
        }

        var recovered = before
            .Where(name => !after.Contains(name, StringComparer.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (recovered.Length > 0)
        {
            _pendingInjuryClearedNames = recovered;
            _pendingCleanXiBridgeNames = recovered;
            ClearWeekStoryClosure();
            return;
        }

        // Kutlama bir sonraki günde düşer; Temiz XI köprüsü ilk sakatsız maça kadar kalır.
        // Haftanın hikâyesi hükmü bir sabah daha Bugün'de kalsın.
        _pendingInjuryClearedNames = null;
        DismissWeekStoryClosureAfterLinger();
    }

    private void ClearWeekStoryClosure()
    {
        _pendingWeekStoryClosure = null;
        _weekStoryClosureDismissOnNextAdvance = false;
    }

    private void DismissWeekStoryClosureAfterLinger()
    {
        if (string.IsNullOrWhiteSpace(_pendingWeekStoryClosure))
        {
            return;
        }

        if (_weekStoryClosureDismissOnNextAdvance)
        {
            ClearWeekStoryClosure();
            return;
        }

        _weekStoryClosureDismissOnNextAdvance = true;
    }

    private IReadOnlyList<string>? ResolveCleanXiBridgeNames(int? injuredSlotCount = null)
    {
        if (_pendingCleanXiBridgeNames is not { Count: > 0 })
        {
            return null;
        }

        var injured = injuredSlotCount ?? GetTrainingSummary().InjuredSlotCount;
        return injured > 0 ? null : _pendingCleanXiBridgeNames;
    }

    private void RecoverManagedInjuriesToCurrentDate()
    {
        if (Host.ManagerModule.Queries.GetCareer().EmployedClubId is not long clubIdValue)
        {
            return;
        }

        var clubId = new ClubId(clubIdValue);
        var day = Host.WorldModule.TimelineStore.Timeline.CurrentDate;
        var recovered = MvpTrainingLoadApplier.RecoverClubToDate(
            clubId,
            day,
            Host.TrainingModule.Store.PhysicalBySlot);
        if (recovered.Count == 0)
        {
            return;
        }

        Host.TrainingModule.Store.ReplacePhysicalStatesForClub(clubId, recovered);
    }

    private PostMatchOfficeDigest BuildInjuryClearedOffice()
    {
        var day = Host.WorldModule.Queries.GetCurrentGameDate().DayNumber;
        var pending = Host.TeamPreparationModule.SelectionQueries
            .GetNextDueManagedFixture(day);
        return PostMatchOfficeDigest.AfterInjuriesCleared(
            _pendingInjuryClearedNames,
            hasDueUnapprovedMatch: pending is { IsApproved: false },
            hasDuePlayableMatch: pending is { IsApproved: true });
    }

    public UiActionResult CompleteSeason()
    {
        try
        {
            var competition = Host.CompetitionModule;
            var season = competition.Queries.GetCurrentSeason()
                ?? throw new InvalidOperationException("Aktif sezon yok.");

            var current = Host.WorldModule.TimelineStore.Timeline.CurrentDate;
            competition.CompleteSeason.Handle(
                new CompleteSeasonCommand(Guid.NewGuid(), season.SeasonId, current.DayNumber));

            return UiActionResult.Ok($"Sezon #{season.SeasonId} kapatıldı.");
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
            var nextSeasonId = ResolveNextSeasonId();

            // FixtureId sezonlar arası yeniden kullanılır; eski onaylar hayalet kapı açmasın.
            ClearMatchSelections();

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

            return UiActionResult.Ok(
                $"Yeni sezon #{nextSeasonId} başlatıldı · kadro onayları temizlendi.");
        }
        catch (Exception ex)
        {
            return UiActionResult.Fail($"Yeni sezon başlatılamadı: {ex.Message}");
        }
    }

    public bool CanTransitionToNextSeason()
    {
        var season = Host.CompetitionModule.Queries.GetCurrentSeason();
        if (season is null)
        {
            return false;
        }

        var progress = Host.CompetitionModule.Queries.GetSeasonProgress(season.SeasonId);
        return progress is { CanComplete: true } or { CanArchive: true };
    }

    public UiActionResult TransitionToNextSeason()
    {
        try
        {
            var competition = Host.CompetitionModule;
            var season = competition.Queries.GetCurrentSeason()
                ?? throw new InvalidOperationException("Geçiş için mevcut sezon yok.");

            var progress = competition.Queries.GetSeasonProgress(season.SeasonId)
                ?? throw new InvalidOperationException("Sezon ilerlemesi okunamadı.");

            var previousSeasonId = season.SeasonId;

            if (progress.CanComplete)
            {
                var completed = CompleteSeason();
                if (!completed.Succeeded)
                {
                    return completed;
                }
            }
            else if (!progress.CanArchive)
            {
                return UiActionResult.Fail(
                    $"Sezon henüz bitmedi ({progress.AcceptedFixtureCount}/{progress.TotalFixtureCount} maç).");
            }

            var archived = ArchiveSeason();
            if (!archived.Succeeded)
            {
                return archived;
            }

            var started = StartNewSeason();
            if (!started.Succeeded)
            {
                return started;
            }

            var nextSeasonId = Host.CompetitionModule.Queries.GetCurrentSeason()?.SeasonId;
            return UiActionResult.Ok(
                $"Sezon geçişi tamam: #{previousSeasonId} arşiv · #{nextSeasonId} aktif · kadro onayları temizlendi.");
        }
        catch (Exception ex)
        {
            return UiActionResult.Fail($"Sezon geçişi başarısız: {ex.Message}");
        }
    }

    private void ClearMatchSelections() =>
        Host.TeamPreparationModule.SelectionStore.ReplaceAll(
            Array.Empty<Domain.TeamPreparation.MatchSelection>());

    private long ResolveNextSeasonId()
    {
        var seasons = Host.CompetitionModule.Store.League.Seasons;
        if (seasons.Count == 0)
        {
            return DefaultSeasonId;
        }

        return seasons.Max(season => season.SeasonId.Value) + 1;
    }

    public SaveDeskDigest BuildSaveDeskDigest()
    {
        var path = Host.DefaultSavePath;
        var exists = File.Exists(path);
        DateTimeOffset? stamp = null;
        if (exists)
        {
            stamp = File.GetLastWriteTimeUtc(path);
        }

        var day = Host.WorldModule.Queries.GetCurrentGameDate();
        var manager = Host.ManagerModule.Queries.GetCareer();
        string? clubName = manager.EmployedClubId is long clubId
            ? GetClubDisplayName(clubId)
            : null;
        var season = Host.CompetitionModule.Queries.GetCurrentSeason();
        long? seasonId = season?.SeasonId;
        string? seasonStatus = season?.Status;
        var accepted = 0;
        var total = 0;
        if (season is not null)
        {
            var progress = Host.CompetitionModule.Queries.GetSeasonProgress(season.SeasonId);
            accepted = progress?.AcceptedFixtureCount ?? 0;
            total = progress?.TotalFixtureCount ?? season.FixtureCount;
        }

        return SaveDeskDigest.Compose(
            path,
            exists,
            stamp,
            day.DayNumber,
            day.IsoDate,
            manager.DisplayName,
            clubName,
            seasonId,
            seasonStatus,
            accepted,
            total);
    }

    public UiActionResult SaveGame()
    {
        try
        {
            var result = Host.GameSession.Save(
                Host.DefaultSavePath,
                CaptureHubNarrativeUiState());
            var desk = BuildSaveDeskDigest();
            return UiActionResult.Ok(
                $"Kayıt Masası\nKariyer diske işlendi — {GameDate.ToDisplayDateString(result.SavedDayNumber)}."
                + $"\n· {result.SavePath}"
                + $"\nÖneri: {desk.AdviceLine}");
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
                LastCareerResume = null;
                return UiActionResult.Fail(
                    "Kayıt Masası\nDiskte kayıt yok — önce Kaydet.");
            }

            var result = Host.GameSession.Load(Host.DefaultSavePath);
            ApplyHubNarrativeUiState(result.HubNarrativeUiState);
            LastCareerResume = BuildCareerResumeDigest(result.WasMigrated);
            return UiActionResult.Ok(LastCareerResume.ToStatusMessage());
        }
        catch (Exception ex)
        {
            LastCareerResume = null;
            return UiActionResult.Fail($"Yükleme hatası: {ex.Message}");
        }
    }

    public HubNarrativeUiState CaptureHubNarrativeUiState() =>
        HubNarrativeUiState.Compose(
            _pendingWeekStoryClosure,
            _weekStoryClosureDismissOnNextAdvance,
            _pendingCleanXiBridgeNames,
            _pendingInjuryClearedNames,
            _matchupPlanHistory);

    public void ApplyHubNarrativeUiState(HubNarrativeUiState? state)
    {
        state ??= HubNarrativeUiState.Empty;
        _pendingWeekStoryClosure = state.WeekStoryClosureBeat;
        _weekStoryClosureDismissOnNextAdvance = state.WeekStoryDismissOnNextAdvance;
        _pendingCleanXiBridgeNames = state.CleanXiNames.Count > 0 ? state.CleanXiNames : null;
        _pendingInjuryClearedNames = state.InjuryClearedNames.Count > 0 ? state.InjuryClearedNames : null;
        _matchupPlanHistory.Clear();
        _matchupPlanHistory.AddRange(state.MatchupPlanHistory);
    }

    public CareerResumeDigest BuildCareerResumeDigest(bool wasMigrated)
    {
        var day = Host.WorldModule.Queries.GetCurrentGameDate();
        var manager = Host.ManagerModule.Queries.GetCareer();
        string? clubName = manager.EmployedClubId is long clubId
            ? GetClubDisplayName(clubId)
            : null;
        var pulse = BuildTodayPulse();
        var weekStory = BuildWeekStory();
        var nextStep = BuildOfficeNextStep(pulse.PrimaryFocusCode, weekStory);

        return CareerResumeDigest.Compose(
            pulse,
            day.DayNumber,
            manager.DisplayName,
            clubName,
            wasMigrated,
            weekStory,
            nextStep);
    }

    public OfficeNextStep? BuildOfficeNextStep(
        string? focusCode = null,
        WeekStoryDigest? weekStory = null)
    {
        var pulseFocus = focusCode ?? BuildTodayPulse().PrimaryFocusCode;
        var story = weekStory ?? BuildWeekStory();
        var currentDay = Host.WorldModule.Queries.GetCurrentGameDate().DayNumber;
        var pending = Host.TeamPreparationModule.SelectionQueries
            .GetNextDueManagedFixture(currentDay);
        var blocker = BuildTimeAdvanceBlockerDigest();
        var prepBriefing = BuildPreparationBriefing();
        var matchBriefing = BuildNextMatchBriefing();
        return OfficeNextStepGuide.ResolveFromPulse(
            pulseFocus,
            hasDueUnapprovedMatch: pending is { IsApproved: false },
            hasDuePlayableMatch: pending is { IsApproved: true },
            canAdvanceDay: blocker.CanAdvance,
            primaryBlockerCode: blocker.PrimaryBlockerCode,
            seasonTransitionReady: CanTransitionToNextSeason(),
            seasonArchivePhase: IsSeasonArchivePhase(),
            prepSuggestion: prepBriefing.Suggestion,
            leagueNextStep: BuildLeagueWorldBriefing().NextStep,
            transferNextStep: BuildTransferDeskBriefing().NextStep,
            hasInjuryPressure: prepBriefing.HasInjuryPressure || matchBriefing.HasInjuryPressure,
            recoveryPath: BuildInjuryRecoveryPath(),
            weekStory: story,
            hasPromiseRisk: matchBriefing.HasPromiseRisk,
            promiseGuideName: matchBriefing.FirstAtRiskPromisePlayerName);
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
                $"Planlama dönemi tamamlandı ({GameDate.ToDisplayDateString(result.CompletedAtDayNumber)}).");
        }
        catch (Exception ex)
        {
            return UiActionResult.Fail($"Planlama dönemi tamamlanamadı: {ex.Message}");
        }
    }

    public string GetClubDisplayName(long clubId) =>
        Host.ClubModule.Queries.GetClub(clubId)?.DisplayName ?? $"Kulüp {clubId}";

    private static int? FindStandingRank(
        IReadOnlyList<StandingEntryReadModel> standings,
        long? managedClubId)
    {
        if (managedClubId is not long clubId)
        {
            return null;
        }

        for (var i = 0; i < standings.Count; i++)
        {
            if (standings[i].ClubId == clubId)
            {
                return i + 1;
            }
        }

        return null;
    }

    public string FormatActiveBlockerSummary() =>
        BuildTimeAdvanceBlockerDigest().ToDisplayText();

    public TimeAdvanceBlockerDigest BuildTimeAdvanceBlockerDigest()
    {
        var eligibility = Host.WorldModule.Queries.GetTimeAdvanceEligibility();
        var blockers = eligibility.Blockers
            .Select(b => (b.SourceContext, b.DescriptionCode, b.IsHardBlocker))
            .ToArray();
        return TimeAdvanceBlockerDigest.Compose(eligibility.CanAdvance, blockers);
    }

    public static string FormatBlockers(IEnumerable<(string SourceContext, string DescriptionCode)> blockers)
    {
        var parts = blockers.Select(b => TimeAdvanceBlockerDigest.Describe(b.DescriptionCode));
        return "İlerleme engellendi: " + string.Join(" · ", parts);
    }

    public static string DescribeBlocker(string sourceContext, string descriptionCode) =>
        TimeAdvanceBlockerDigest.Describe(descriptionCode);

    public static int ComputeFirstMatchdayDayNumber(int currentDayNumber) =>
        GameDate.FromDayNumber(currentDayNumber).AddDays(4).DayNumber;

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

public sealed record UiActionResult(
    bool Succeeded,
    string Message,
    TimeAdvanceDigest? Digest = null,
    string? NarrativeBridgeLine = null,
    string? NextFocusCode = null)
{
    public static UiActionResult Ok(
        string message,
        TimeAdvanceDigest? digest = null,
        string? narrativeBridgeLine = null,
        string? nextFocusCode = null) =>
        new(true, message, digest, narrativeBridgeLine, nextFocusCode);

    public static UiActionResult Fail(string message) => new(false, message);
}

public sealed record PlayMatchesUiResult(
    bool Succeeded,
    string Message,
    IReadOnlyList<string> MatchLines,
    IReadOnlyList<string>? ConsequenceLines = null,
    IReadOnlyList<string>? KeyMomentLines = null,
    MatchNightNarrative? Narrative = null,
    MatchReportDigest? Report = null,
    LeagueRoundupDigest? Roundup = null,
    CaptainReactionDigest? DressingRoom = null,
    TechnicalAreaDigest? TechnicalArea = null,
    MatchupPlanOutcomeDigest? MatchupPlanOutcome = null,
    FormStreakVerdictDigest? FormStreakVerdict = null);
