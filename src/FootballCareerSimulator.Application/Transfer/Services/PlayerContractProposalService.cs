using FootballCareerSimulator.Application.ClubGovernance.Ports;
using FootballCareerSimulator.Application.CareerHub.Queries;
using FootballCareerSimulator.Application.ClubGovernance.Services;
using FootballCareerSimulator.Application.ManagerCareer.Ports;
using FootballCareerSimulator.Application.PlayerCareer.Ports;
using FootballCareerSimulator.Application.Transfer.Infrastructure;
using FootballCareerSimulator.Application.Transfer.Ports;
using FootballCareerSimulator.Domain.ClubGovernance;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.Transfer;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.Transfer.Services;

/// <summary>
/// Player Contract Proposal iskeleti: teklif / karşı teklif / kabul / ret + maaş rezervasyonu.
/// </summary>
public sealed class PlayerContractProposalService
{
    private readonly IPlayerContractProposalStore _proposalStore;
    private readonly ITransferProcessStore _processStore;
    private readonly IManagerCareerStore _managerCareerStore;
    private readonly ITransferWindowQuery _transferWindow;
    private readonly ClubWageBudgetService? _wageBudget;
    private readonly IPlayerCareerStore? _playerCareers;
    private readonly Func<int> _gameSeed;
    private readonly IClubRegistryStore? _clubRegistry;

    /// <summary>
    /// Menajer pazarlık becerisinin ücret talebine etkisi (simülatör denge kuralı, native değildir):
    /// beceri 50'nin üstündeki her puan için %0,3 indirim, en çok %15. Yüzde birimiyle, negatif değer indirimdir.
    /// </summary>
    public static double ManagerNegotiationWagePercentage(int negotiationSkill) =>
        -Math.Clamp((negotiationSkill - 50) * 0.3, 0.0, 15.0);

    public PlayerContractProposalService(
        IPlayerContractProposalStore proposalStore,
        ITransferProcessStore processStore,
        IManagerCareerStore managerCareerStore,
        ITransferWindowQuery? transferWindow = null,
        ClubWageBudgetService? wageBudget = null,
        IPlayerCareerStore? playerCareers = null,
        int gameSeed = 0,
        IClubRegistryStore? clubRegistry = null,
        Func<int>? gameSeedProvider = null)
    {
        _proposalStore = proposalStore ?? throw new ArgumentNullException(nameof(proposalStore));
        _processStore = processStore ?? throw new ArgumentNullException(nameof(processStore));
        _managerCareerStore = managerCareerStore
            ?? throw new ArgumentNullException(nameof(managerCareerStore));
        _transferWindow = transferWindow ?? AlwaysOpenTransferWindowQuery.Instance;
        _wageBudget = wageBudget;
        _playerCareers = playerCareers;
        // Kayıt yüklendiğinde değişen oyun tohumu her talep hesabında güncel okunur.
        _gameSeed = gameSeedProvider ?? (() => gameSeed);
        _clubRegistry = clubRegistry;
    }

    public PlayerContractProposal SubmitContractProposal(
        TransferProcessId processId,
        int weeklyWage,
        int contractYears,
        GameDate day,
        TransferActingParty actor = TransferActingParty.HumanManager)
    {
        EnsureTransferWindowOpen();
        var process = RequireProcess(processId);
        EnsureActor(process.BuyingClubId, actor);

        if (process.Status is TransferProcessStatus.ClubAgreementReached
            or TransferProcessStatus.SportingApproved)
        {
            process = process.EnterPlayerNegotiation();
            _processStore.Upsert(process);
        }
        else if (process.Status != TransferProcessStatus.PlayerNegotiation)
        {
            throw new TransferInvariantViolationException(
                "Contract proposal requires club agreement (or sporting approval for free agents) "
                + "or an open player negotiation.");
        }

        if (_proposalStore.GetForProcess(processId).Any(p => p.IsPending))
        {
            throw new TransferInvariantViolationException(
                "A pending contract proposal already exists; accept, reject, or counter it first.");
        }

        var round = _proposalStore.GetForProcess(processId).Select(p => p.Round).DefaultIfEmpty(0).Max() + 1;
        var maxId = _proposalStore.Proposals.Select(p => p.ProposalId.Value).DefaultIfEmpty(0).Max();
        var proposal = PlayerContractProposal.Submit(
            new PlayerContractProposalId(maxId + 1),
            processId,
            round,
            weeklyWage,
            contractYears,
            day);
        _proposalStore.Upsert(proposal);
        return proposal;
    }

    public PlayerContractProposal AcceptPendingProposal(
        TransferProcessId processId,
        TransferActingParty actor = TransferActingParty.HumanManager)
    {
        var process = RequireProcess(processId);
        EnsureActor(process.BuyingClubId, actor);
        if (!process.IsInPlayerNegotiation)
        {
            throw new TransferInvariantViolationException("No active player negotiation to accept.");
        }

        var pending = RequirePending(processId);
        EnsurePlayerWouldAccept(process, pending);
        ReserveWage(process.BuyingClubId, pending.WeeklyWage, pending.SubmittedOn);
        var accepted = pending.Accept();
        _proposalStore.Upsert(accepted);
        _processStore.Upsert(process.ReachPlayerAgreement());
        return accepted;
    }

    public PlayerContractProposal RejectPendingProposal(
        TransferProcessId processId,
        TransferActingParty actor = TransferActingParty.HumanManager)
    {
        var process = RequireProcess(processId);
        EnsureActor(process.BuyingClubId, actor);
        if (!process.IsInPlayerNegotiation)
        {
            throw new TransferInvariantViolationException("No active player negotiation to reject.");
        }

        var pending = RequirePending(processId);
        var rejected = pending.Reject();
        _proposalStore.Upsert(rejected);
        return rejected;
    }

    public PlayerContractProposal CounterPendingProposal(
        TransferProcessId processId,
        int weeklyWage,
        int contractYears,
        GameDate day,
        TransferActingParty actor = TransferActingParty.HumanManager)
    {
        EnsureTransferWindowOpen();
        var process = RequireProcess(processId);
        EnsureActor(process.BuyingClubId, actor);
        if (!process.IsInPlayerNegotiation)
        {
            throw new TransferInvariantViolationException("No active player negotiation to counter.");
        }

        var pending = RequirePending(processId);
        _proposalStore.Upsert(pending.Supersede());

        var round = pending.Round + 1;
        var maxId = _proposalStore.Proposals.Select(p => p.ProposalId.Value).DefaultIfEmpty(0).Max();
        var counter = PlayerContractProposal.Submit(
            new PlayerContractProposalId(maxId + 1),
            processId,
            round,
            weeklyWage,
            contractYears,
            day);
        _proposalStore.Upsert(counter);
        return counter;
    }

    public int ResolveAcceptableWeeklyWage(TransferProcessId processId, GameDate day)
    {
        var process = RequireProcess(processId);
        var demand = TryBuildDemand(process, day, PlayerWageBalanceProfile.Default.SuggestedContractYears);
        return demand?.WeeklyWage ?? AiClubTransferSimulationService.DefaultWeeklyWage;
    }

    private void EnsurePlayerWouldAccept(TransferProcess process, PlayerContractProposal pending)
    {
        // Sabır, aynı süreçteki önceki reddedilmiş teklifler sırayla yeniden değerlendirilerek kurulur;
        // böylece sabır/hakaret durumu kayıtlı teklif geçmişinden deterministik olarak türetilir.
        var state = ContractNegotiationState.Start(isTransferWanted: false, hasActiveConcerns: false);
        var previous = _proposalStore.GetForProcess(process.ProcessId)
            .Where(proposal => proposal.Status == PlayerContractProposalStatus.Rejected
                               && proposal.Round < pending.Round)
            .OrderBy(proposal => proposal.Round)
            .ToArray();
        foreach (var rejected in previous)
        {
            var priorDemand = TryBuildDemand(process, rejected.SubmittedOn, rejected.ContractYears);
            if (priorDemand is null || state.IsTerminated)
            {
                break;
            }

            state = ContractOfferEvaluator.Evaluate(
                priorDemand,
                ToOffer(rejected),
                state,
                profile: ContractNegotiationProfile.Sm25NativeParity).State;
        }

        if (state.IsTerminated)
        {
            _proposalStore.Upsert(pending.Reject());
            throw new TransferInvariantViolationException(
                "Oyuncunun sabrı tükendi; bu sözleşme görüşmesi kapandı.");
        }

        var demand = TryBuildDemand(process, pending.SubmittedOn, pending.ContractYears);
        if (demand is null)
        {
            return;
        }

        var evaluation = ContractOfferEvaluator.Evaluate(
            demand,
            ToOffer(pending),
            state,
            profile: ContractNegotiationProfile.Sm25NativeParity);
        if (evaluation.Accepted)
        {
            return;
        }

        _proposalStore.Upsert(pending.Reject());
        var message = DescribeRejection(evaluation.Reason);
        if (evaluation.State.IsTerminated && evaluation.Reason != ContractNegotiationRejectionReason.Insulted)
        {
            message += " Oyuncunun sabrı tükendi; görüşme kapandı.";
        }
        else if (evaluation.Reason == ContractNegotiationRejectionReason.Wages && demand.RequestedWeeklyWage > 0)
        {
            message += $" Yeni talep: haftalık {demand.RequestedWeeklyWage:N0}.";
        }

        throw new TransferInvariantViolationException(message);
    }

    private static ContractOffer ToOffer(PlayerContractProposal proposal) =>
        new(
            proposal.WeeklyWage,
            SigningFee: 0,
            proposal.ContractYears,
            ContractSquadStatus.FirstTeam);

    private PlayerContractDemand? TryBuildDemand(TransferProcess process, GameDate day, int contractYears)
    {
        var career = _playerCareers?.Careers.FirstOrDefault(candidate => candidate.Id == process.PlayerId);
        if (career is null)
        {
            return null;
        }

        var age = career.AgeYears(day);
        var buyingClubStrength = ResolveBuyingClubStrength(process, career.CurrentAbility);
        var wage = PlayerWageModel.EvaluateContractBasis(
            career.CurrentAbility,
            career.PotentialAbility,
            age,
            buyingClubStrength,
            Math.Clamp(contractYears, 1, 5),
            process.IsFreeAgent);

        // Menajer pazarlık becerisi yalnız kullanıcının kulübü için uygulanır; AI kulüpleri nötr talep görür.
        var managerCareer = _managerCareerStore.Career;
        var isTargetUserClub = managerCareer.ActiveEmployment?.ClubId == process.BuyingClubId;
        var managerWagePercentage = isTargetUserClub
            ? ManagerNegotiationWagePercentage(managerCareer.Progression.Skills.NegotiationSkill)
            : 0.0;

        return PlayerContractDemandModel.Generate(new PlayerContractDemandInput(
            PlayerId: unchecked((int)process.PlayerId.Value),
            GameSeed: _gameSeed(),
            BaseAcceptableWeeklyWage: Math.Max(1, wage.WeeklyWage),
            PlayerValue: 0,
            Age: age,
            PlayerRating: career.CurrentAbility,
            TargetClubAverageRating: buyingClubStrength,
            DesiredContractYears: 3,
            MinimumContractYears: 1,
            DesiredSquadStatus: ContractSquadStatus.FirstTeam,
            IsFreeAgent: process.IsFreeAgent,
            IsTargetUserClub: isTargetUserClub,
            ClubId: unchecked((int)process.BuyingClubId.Value),
            ManagerWageTraitPercentage: managerWagePercentage));
    }

    private int ResolveBuyingClubStrength(TransferProcess process, int fallback)
    {
        var club = _clubRegistry?.Registry.Clubs.FirstOrDefault(candidate => candidate.Id == process.BuyingClubId);
        return club?.SportiveStrength ?? fallback;
    }

    private static string DescribeRejection(ContractNegotiationRejectionReason reason) =>
        reason switch
        {
            ContractNegotiationRejectionReason.Wages =>
                "Oyuncu haftalık ücreti kabul etmedi. Teklifi yükselt.",
            ContractNegotiationRejectionReason.Insulted =>
                "Teklif hakaret sayıldı; görüşme kapandı.",
            ContractNegotiationRejectionReason.SquadStatus =>
                "Oyuncu önerilen kadro rolünü kabul etmedi.",
            ContractNegotiationRejectionReason.ContractTooShort =>
                "Oyuncu daha uzun sözleşme istiyor.",
            ContractNegotiationRejectionReason.ContractTooLong =>
                "Oyuncu bu kadar uzun sözleşmeyi kabul etmedi.",
            ContractNegotiationRejectionReason.SigningFee =>
                "Oyuncu imza parasını yetersiz buldu.",
            ContractNegotiationRejectionReason.Bonuses =>
                "Oyuncu bonus maddesini yetersiz buldu.",
            _ => "Oyuncu sözleşmeyi reddetti."
        };

    private PlayerContractProposal RequirePending(TransferProcessId processId) =>
        _proposalStore.GetForProcess(processId).LastOrDefault(p => p.IsPending)
        ?? throw new TransferInvariantViolationException(
            $"No pending contract proposal for process #{processId.Value}.");

    private TransferProcess RequireProcess(TransferProcessId processId) =>
        _processStore.Get(processId)
        ?? throw new TransferInvariantViolationException($"Transfer process #{processId.Value} not found.");

    private void EnsureActor(ClubId buyingClubId, TransferActingParty actor) =>
        TransferActorGuard.EnsureBuyingClubActor(
            _managerCareerStore,
            buyingClubId,
            actor,
            "Only the employed manager of the buying club can manage contract proposals.");

    private void ReserveWage(ClubId clubId, int weeklyWage, GameDate day)
    {
        if (_wageBudget is null || weeklyWage <= 0)
        {
            return;
        }

        try
        {
            _wageBudget.Reserve(clubId, weeklyWage, day);
        }
        catch (ClubGovernanceInvariantViolationException ex)
        {
            throw new TransferInvariantViolationException(ex.Message);
        }
    }

    private void EnsureTransferWindowOpen()
    {
        if (!_transferWindow.IsOpen)
        {
            throw new TransferInvariantViolationException(
                "Transfer window is closed; cannot submit a contract proposal.");
        }
    }
}
