using FootballCareerSimulator.Application.ClubGovernance.Ports;
using FootballCareerSimulator.Application.ClubGovernance.Services;
using FootballCareerSimulator.Application.ContractRegistration.Ports;
using FootballCareerSimulator.Application.ManagerCareer.Ports;
using FootballCareerSimulator.Application.TeamPreparation.Ports;
using FootballCareerSimulator.Application.Transfer.Ports;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.Transfer;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.Transfer.Services;

/// <summary>
/// Yönetilmeyen kulüpler için pencere açıkken minimal AI transfer tick (D-140).
/// Önce serbest oyuncu, yoksa kulüpler arası satış; ilişki/diyalog/fiyat formülü yok.
/// </summary>
public sealed class AiClubTransferSimulationService
{
    public const int MaxCompletionsPerTick = 1;
    public const int DefaultWeeklyWage = 15_000;
    public const int DefaultContractYears = 2;
    public const int DefaultClubTransferFee = 500_000;
    public const int MinSellerActiveContracts = 2;

    private readonly IClubRegistryStore _clubRegistry;
    private readonly IManagerCareerStore _managerCareerStore;
    private readonly IFreeAgentStore _freeAgentStore;
    private readonly IContractStore _contractStore;
    private readonly IClubSquadStore _squadStore;
    private readonly ITransferNeedStore _needStore;
    private readonly ITransferProcessStore _processStore;
    private readonly ITransferWindowQuery _transferWindow;
    private readonly ClubTransferBudgetService? _transferBudget;
    private readonly TransferNeedService _needs;
    private readonly ShortlistTargetService _shortlistTargets;
    private readonly TransferProcessService _processes;
    private readonly ClubOfferService _clubOffers;
    private readonly PlayerContractProposalService _proposals;
    private readonly TransferCompletionService _completion;

    public AiClubTransferSimulationService(
        IClubRegistryStore clubRegistry,
        IManagerCareerStore managerCareerStore,
        IFreeAgentStore freeAgentStore,
        IContractStore contractStore,
        IClubSquadStore squadStore,
        ITransferNeedStore needStore,
        ITransferProcessStore processStore,
        ITransferWindowQuery transferWindow,
        ClubTransferBudgetService? transferBudget,
        TransferNeedService needs,
        ShortlistTargetService shortlistTargets,
        TransferProcessService processes,
        ClubOfferService clubOffers,
        PlayerContractProposalService proposals,
        TransferCompletionService completion)
    {
        _clubRegistry = clubRegistry ?? throw new ArgumentNullException(nameof(clubRegistry));
        _managerCareerStore = managerCareerStore
            ?? throw new ArgumentNullException(nameof(managerCareerStore));
        _freeAgentStore = freeAgentStore ?? throw new ArgumentNullException(nameof(freeAgentStore));
        _contractStore = contractStore ?? throw new ArgumentNullException(nameof(contractStore));
        _squadStore = squadStore ?? throw new ArgumentNullException(nameof(squadStore));
        _needStore = needStore ?? throw new ArgumentNullException(nameof(needStore));
        _processStore = processStore ?? throw new ArgumentNullException(nameof(processStore));
        _transferWindow = transferWindow ?? throw new ArgumentNullException(nameof(transferWindow));
        _transferBudget = transferBudget;
        _needs = needs ?? throw new ArgumentNullException(nameof(needs));
        _shortlistTargets = shortlistTargets ?? throw new ArgumentNullException(nameof(shortlistTargets));
        _processes = processes ?? throw new ArgumentNullException(nameof(processes));
        _clubOffers = clubOffers ?? throw new ArgumentNullException(nameof(clubOffers));
        _proposals = proposals ?? throw new ArgumentNullException(nameof(proposals));
        _completion = completion ?? throw new ArgumentNullException(nameof(completion));
    }

    public AiClubTransferTickOutcome RunWindowTick(GameDate day, int worldSeed)
    {
        if (!_transferWindow.IsOpen)
        {
            return new AiClubTransferTickOutcome(CompletedCount: 0, AttemptedClubCount: 0);
        }

        var managedClubId = _managerCareerStore.Career.ActiveEmployment?.ClubId;
        var candidates = _clubRegistry.Registry.Clubs
            .Where(club => managedClubId is null || club.Id.Value != managedClubId.Value.Value)
            .OrderBy(club => club.Id.Value)
            .ToArray();

        if (candidates.Length == 0)
        {
            return new AiClubTransferTickOutcome(0, 0);
        }

        var start = Math.Abs(worldSeed) % candidates.Length;
        var completed = 0;
        var attempted = 0;

        for (var offset = 0; offset < candidates.Length && completed < MaxCompletionsPerTick; offset++)
        {
            var club = candidates[(start + offset) % candidates.Length];
            attempted++;
            if (TrySignFreeAgent(club.Id, day)
                || TryBuyFromUnmanagedClub(club.Id, day, worldSeed, managedClubId))
            {
                completed++;
            }
        }

        return new AiClubTransferTickOutcome(completed, attempted);
    }

    private bool TrySignFreeAgent(ClubId buyingClubId, GameDate day)
    {
        if (!HasSquadSpace(buyingClubId))
        {
            return false;
        }

        var freeAgent = _freeAgentStore.FreeAgents
            .OrderBy(fa => fa.PlayerId.Value)
            .Select(fa => fa.PlayerId)
            .FirstOrDefault(playerId => !HasActiveProcessForPlayer(playerId));

        if (freeAgent.Value == 0)
        {
            return false;
        }

        return TryCompletePipeline(
            buyingClubId,
            freeAgent,
            day,
            clubTransferFee: null,
            reasonCode: "AiFreeAgentSigning");
    }

    private bool TryBuyFromUnmanagedClub(
        ClubId buyingClubId,
        GameDate day,
        int worldSeed,
        ClubId? managedClubId)
    {
        if (!HasSquadSpace(buyingClubId))
        {
            return false;
        }

        if (_transferBudget is not null
            && _transferBudget.Get(buyingClubId).Available < DefaultClubTransferFee)
        {
            return false;
        }

        var sellers = _clubRegistry.Registry.Clubs
            .Where(club => club.Id.Value != buyingClubId.Value)
            .Where(club => managedClubId is null || club.Id.Value != managedClubId.Value.Value)
            .Where(club =>
                _contractStore.GetForClub(club.Id).Count(c => c.IsActiveOn(day)) >= MinSellerActiveContracts)
            .OrderBy(club => club.Id.Value)
            .ToArray();

        if (sellers.Length == 0)
        {
            return false;
        }

        var seller = sellers[Math.Abs(worldSeed + (int)buyingClubId.Value) % sellers.Length];
        var playerId = _contractStore.GetForClub(seller.Id)
            .Where(c => c.IsActiveOn(day))
            .Select(c => c.PlayerId)
            .Where(id => !HasActiveProcessForPlayer(id))
            .OrderBy(id => id.Value)
            .FirstOrDefault();

        if (playerId.Value == 0)
        {
            return false;
        }

        return TryCompletePipeline(
            buyingClubId,
            playerId,
            day,
            clubTransferFee: DefaultClubTransferFee,
            reasonCode: "AiClubToClubSigning");
    }

    private bool TryCompletePipeline(
        ClubId buyingClubId,
        PlayerId playerId,
        GameDate day,
        int? clubTransferFee,
        string reasonCode)
    {
        try
        {
            const TransferActingParty actor = TransferActingParty.SimulatedClub;
            _needs.Declare(
                buyingClubId,
                TransferNeedKind.PositionGap,
                priority: 2,
                reasonCode,
                day);

            var needId = _needStore.GetForClub(buyingClubId)
                .Where(n => n.IsOpen)
                .OrderBy(n => n.NeedId.Value)
                .Select(n => n.NeedId)
                .First();

            var entry = _shortlistTargets.AddToShortlist(
                buyingClubId,
                playerId,
                needId,
                priority: 2,
                day);
            _shortlistTargets.AddTransferTarget(needId, playerId, entry.EntryId, day);

            var process = _processes.OpenOldestListedTargetForClub(buyingClubId, day);
            var expectsFreeAgent = clubTransferFee is null;
            if (process.IsFreeAgent != expectsFreeAgent)
            {
                return false;
            }

            _processes.RequestSportingApproval(process.ProcessId, actor);
            _processes.GrantSportingApproval(process.ProcessId, actor);

            if (clubTransferFee is { } fee)
            {
                _clubOffers.SubmitClubOffer(process.ProcessId, fee, day, actor);
                _clubOffers.AcceptPendingOffer(process.ProcessId, actor);
            }

            _proposals.SubmitContractProposal(
                process.ProcessId,
                DefaultWeeklyWage,
                DefaultContractYears,
                day,
                actor);
            _proposals.AcceptPendingProposal(process.ProcessId, actor);
            _processes.RequestFinancialApproval(process.ProcessId, actor);
            _processes.GrantFinancialApproval(process.ProcessId, actor);
            var completed = _completion.Complete(process.ProcessId, day, actor);
            return completed.Status is TransferProcessStatus.Archived or TransferProcessStatus.Completed;
        }
        catch (Exception ex) when (ex is TransferInvariantViolationException
            or InvalidOperationException
            or ArgumentException)
        {
            return false;
        }
    }

    private bool HasSquadSpace(ClubId buyingClubId)
    {
        var squadCount = _squadStore.Get(buyingClubId)?.Members.Count ?? 0;
        return squadCount < ClubSquad.MaxMembers;
    }

    private bool HasActiveProcessForPlayer(PlayerId playerId) =>
        _processStore.Processes.Any(p => p.IsActive && p.PlayerId.Value == playerId.Value);
}

public sealed record AiClubTransferTickOutcome(int CompletedCount, int AttemptedClubCount);
