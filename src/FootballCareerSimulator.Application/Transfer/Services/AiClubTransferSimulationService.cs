using FootballCareerSimulator.Application.ClubGovernance.Ports;
using FootballCareerSimulator.Application.ContractRegistration.Ports;
using FootballCareerSimulator.Application.ManagerCareer.Ports;
using FootballCareerSimulator.Application.TeamPreparation.Ports;
using FootballCareerSimulator.Application.Transfer.Ports;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.Transfer;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.Transfer.Services;

/// <summary>
/// Yönetilmeyen kulüpler için pencere açıkken minimal AI transfer tick (D-140).
/// Serbest oyuncu imzası; ilişki/diyalog/fiyat formülü yok.
/// </summary>
public sealed class AiClubTransferSimulationService
{
    public const int MaxCompletionsPerTick = 1;
    public const int DefaultWeeklyWage = 15_000;
    public const int DefaultContractYears = 2;

    private readonly IClubRegistryStore _clubRegistry;
    private readonly IManagerCareerStore _managerCareerStore;
    private readonly IFreeAgentStore _freeAgentStore;
    private readonly IClubSquadStore _squadStore;
    private readonly ITransferNeedStore _needStore;
    private readonly ITransferProcessStore _processStore;
    private readonly ITransferWindowQuery _transferWindow;
    private readonly TransferNeedService _needs;
    private readonly ShortlistTargetService _shortlistTargets;
    private readonly TransferProcessService _processes;
    private readonly PlayerContractProposalService _proposals;
    private readonly TransferCompletionService _completion;

    public AiClubTransferSimulationService(
        IClubRegistryStore clubRegistry,
        IManagerCareerStore managerCareerStore,
        IFreeAgentStore freeAgentStore,
        IClubSquadStore squadStore,
        ITransferNeedStore needStore,
        ITransferProcessStore processStore,
        ITransferWindowQuery transferWindow,
        TransferNeedService needs,
        ShortlistTargetService shortlistTargets,
        TransferProcessService processes,
        PlayerContractProposalService proposals,
        TransferCompletionService completion)
    {
        _clubRegistry = clubRegistry ?? throw new ArgumentNullException(nameof(clubRegistry));
        _managerCareerStore = managerCareerStore
            ?? throw new ArgumentNullException(nameof(managerCareerStore));
        _freeAgentStore = freeAgentStore ?? throw new ArgumentNullException(nameof(freeAgentStore));
        _squadStore = squadStore ?? throw new ArgumentNullException(nameof(squadStore));
        _needStore = needStore ?? throw new ArgumentNullException(nameof(needStore));
        _processStore = processStore ?? throw new ArgumentNullException(nameof(processStore));
        _transferWindow = transferWindow ?? throw new ArgumentNullException(nameof(transferWindow));
        _needs = needs ?? throw new ArgumentNullException(nameof(needs));
        _shortlistTargets = shortlistTargets ?? throw new ArgumentNullException(nameof(shortlistTargets));
        _processes = processes ?? throw new ArgumentNullException(nameof(processes));
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
            if (TrySignFreeAgent(club.Id, day))
            {
                completed++;
            }
        }

        return new AiClubTransferTickOutcome(completed, attempted);
    }

    private bool TrySignFreeAgent(ClubId buyingClubId, GameDate day)
    {
        var squadCount = _squadStore.Get(buyingClubId)?.Members.Count ?? 0;
        if (squadCount >= ClubSquad.MaxMembers)
        {
            return false;
        }

        var freeAgent = _freeAgentStore.FreeAgents
            .OrderBy(fa => fa.PlayerId.Value)
            .Select(fa => fa.PlayerId)
            .FirstOrDefault(playerId =>
                !_processStore.Processes.Any(p => p.IsActive && p.PlayerId.Value == playerId.Value));

        if (freeAgent.Value == 0)
        {
            return false;
        }

        try
        {
            const TransferActingParty actor = TransferActingParty.SimulatedClub;
            _needs.Declare(
                buyingClubId,
                TransferNeedKind.PositionGap,
                priority: 2,
                "AiFreeAgentSigning",
                day);

            var needId = _needStore.GetForClub(buyingClubId)
                .Where(n => n.IsOpen)
                .OrderBy(n => n.NeedId.Value)
                .Select(n => n.NeedId)
                .First();

            var entry = _shortlistTargets.AddToShortlist(
                buyingClubId,
                freeAgent,
                needId,
                priority: 2,
                day);
            _shortlistTargets.AddTransferTarget(needId, freeAgent, entry.EntryId, day);

            var process = _processes.OpenOldestListedTargetForClub(buyingClubId, day);
            if (!process.IsFreeAgent)
            {
                return false;
            }

            _processes.RequestSportingApproval(process.ProcessId, actor);
            _processes.GrantSportingApproval(process.ProcessId, actor);
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
}

public sealed record AiClubTransferTickOutcome(int CompletedCount, int AttemptedClubCount);
