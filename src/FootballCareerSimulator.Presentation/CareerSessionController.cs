using FootballCareerSimulator.Application.Competition.Commands;
using FootballCareerSimulator.Application.Competition.Composition;
using FootballCareerSimulator.Application.ManagerCareer.Commands;
using FootballCareerSimulator.Application.TeamPreparation.Commands;
using FootballCareerSimulator.Application.TrainingPhysicalState.Commands;
using FootballCareerSimulator.Application.TrainingPhysicalState.Queries;
using FootballCareerSimulator.Application.WorldCalendar.Commands;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.ContractRegistration;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.TrainingPhysicalState;
using FootballCareerSimulator.Domain.Transfer;
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
            var result = Host.ContractModule.Registration.SignFreeAgentToLastClub(
                new PlayerId(signable.PlayerId),
                id,
                day);
            Host.TeamPreparationModule.ClubSquad?.SyncFromActiveContracts(id, day);

            return UiActionResult.Ok(
                $"Serbest oyuncu imzalandı: #{result.PlayerId}"
                + $" · ücret {result.WeeklyWage}"
                + $" · bitiş gün {result.EndDayNumber}.");
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
            return UiActionResult.Ok(
                $"Taktik yaklaşım: {view.ApproachName} · formasyon {view.FormationName}.");
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
            var plan = Host.TeamPreparationModule.TacticQueries.GetManagedClubPlan();
            return UiActionResult.Ok($"Formasyon: {plan.FormationName} · yaklaşım {plan.ApproachName}.");
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
        try
        {
            var clubId = Host.ManagerModule.Queries.GetCareer().EmployedClubId
                ?? throw new InvalidOperationException("Menajer kulübü yok.");
            var day = Host.WorldModule.TimelineStore.Timeline.CurrentDate;
            var id = new Domain.Shared.ClubId(clubId);
            if (Host.TransferModule.Queries.GetManagedClubNeeds().OpenCount == 0)
            {
                Host.TransferModule.Needs.Declare(
                    id,
                    TransferNeedKind.PositionGap,
                    priority: 3,
                    "AutoForTarget",
                    day);
            }

            var target = Host.TransferModule.ShortlistTargets.SuggestAndListTargetForOldestOpenNeed(id, day);
            return UiActionResult.Ok(
                $"Hedef listelendi: #{target.TargetId.Value} oyuncu {target.PlayerId.Value}"
                + $" · ihtiyaç #{target.NeedId.Value}.");
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
                $"Hedef düşürüldü: #{listed.TargetId} (oyuncu {listed.PlayerId}).");
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
                $"Süreç açıldı: #{process.ProcessId.Value} · hedef #{process.TargetId.Value}"
                + $" · oyuncu {process.PlayerId.Value} (değerlendirmede).");
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
            return UiActionResult.Ok($"Süreç geri çekildi: #{active.ProcessId}.");
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
                $"Sportif onay istendi: süreç #{updated.ProcessId.Value} · {TranslateProcessStatus(updated.Status)}.");
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
                $"Sportif onay verildi: süreç #{updated.ProcessId.Value} · oyuncu {updated.PlayerId.Value}.");
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
            return UiActionResult.Ok($"Sportif red: süreç #{updated.ProcessId.Value}.");
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
                $"Mali onay istendi: süreç #{updated.ProcessId.Value} · {TranslateProcessStatus(updated.Status)}.");
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
                $"Mali onay verildi: süreç #{updated.ProcessId.Value} · oyuncu {updated.PlayerId.Value}.");
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
            return UiActionResult.Ok($"Mali red: süreç #{updated.ProcessId.Value}.");
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
                $"Transfer tamamlandı: süreç #{updated.ProcessId.Value}"
                + $" · oyuncu {updated.PlayerId.Value} → kulüp {updated.BuyingClubId.Value}"
                + $" · {TranslateProcessStatus(updated.Status)}.");
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

    public UiActionResult OpenTransferWindow()
    {
        try
        {
            var result = Host.WorldModule.OpenTransferWindow.Handle(
                new Application.WorldCalendar.Commands.OpenTransferWindowCommand(
                    Guid.NewGuid(),
                    ClosesOnDayNumber: null));
            var timeline = Host.WorldModule.TimelineStore.Timeline;
            var ai = Host.TransferModule.AiSimulation.RunWindowTick(
                timeline.CurrentDate,
                timeline.RootSeed);
            return UiActionResult.Ok(
                $"Transfer penceresi açıldı (gün {result.OpenedOnDayNumber})"
                + $" · AI transfer: {ai.CompletedCount}.");
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
            _ = Host.WorldModule.CloseTransferWindow.Handle(
                new Application.WorldCalendar.Commands.CloseTransferWindowCommand(Guid.NewGuid()));
            var outcome = Host.TransferModule.WindowClose.ApplyWindowClosed(
                Host.WorldModule.TimelineStore.Timeline.CurrentDate);
            return UiActionResult.Ok(
                $"Transfer penceresi kapatıldı (expire: {outcome.ExpiredCount}, taşınan: {outcome.CarriedCount}).");
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

        var day = Host.WorldModule.TimelineStore.Timeline.CurrentDate;
        var declined = Host.PlayerCareerModule.Development.ApplyDueAging(day);
        var expiry = Host.ContractModule.Registration.ExpireDueContracts(day);
        Host.TeamPreparationModule.ClubSquad?.SyncClubs(expiry.AffectedClubIds, day);
        if (Host.ManagerModule.Queries.GetCareer().EmployedClubId is long clubId)
        {
            var id = new Domain.Shared.ClubId(clubId);
            Host.PlayerCareerModule.Development.EnsureClub(
                id,
                Host.WorldModule.TimelineStore.Timeline.RootSeed,
                day);
            Host.TeamPreparationModule.ClubSquad?.SyncFromActiveContracts(id, day);
            Host.TeamPreparationModule.TacticPlans.EnsureDefault(id, day);
        }

        var extras = new List<string>();
        if (declined > 0)
        {
            extras.Add($"yaşlanma: {declined}");
        }

        if (expiry.ExpiredCount > 0)
        {
            extras.Add($"sözleşme bitti: {expiry.ExpiredCount}");
            extras.Add($"serbest: {expiry.FreeAgentPlayerIds.Count}");
        }

        var suffix = extras.Count == 0 ? string.Empty : " · " + string.Join(" · ", extras);
        return UiActionResult.Ok(
            $"Tarih ilerledi: gün {result.PreviousDayNumber} → {result.NewDayNumber}{suffix}.");
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
