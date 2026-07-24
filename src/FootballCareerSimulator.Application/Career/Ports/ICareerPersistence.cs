namespace FootballCareerSimulator.Application.Career.Ports;

using FootballCareerSimulator.Domain.ClubGovernance;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.ContractRegistration;
using FootballCareerSimulator.Domain.Interaction;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.TrainingPhysicalState;
using FootballCareerSimulator.Domain.Transfer;
using FootballCareerSimulator.Domain.WorldCalendar;
using PlayerCareerAggregate = FootballCareerSimulator.Domain.PlayerCareer.PlayerCareer;

public interface ICareerPersistence
{
    void Save(
        string filePath,
        WorldTimeline timeline,
        LeagueCompetition league,
        LeagueClubRegistry clubRegistry,
        ManagerCareer managerCareer,
        IReadOnlyList<MatchSelection> matchSelections,
        IReadOnlyList<WeeklyTrainingPlan> trainingPlans,
        IReadOnlyList<PlayerPhysicalState> physicalStates,
        IReadOnlyList<PlayerCareerAggregate> playerCareers,
        IReadOnlyList<PlayerContract> contracts,
        IReadOnlyList<ClubSquad> clubSquads,
        IReadOnlyList<PlayerFreeAgency> freeAgents,
        IReadOnlyList<TacticPlan> tacticPlans,
        IReadOnlyList<TransferNeed> transferNeeds,
        IReadOnlyList<ShortlistEntry> shortlistEntries,
        IReadOnlyList<TransferTarget> transferTargets,
        IReadOnlyList<TransferProcess> transferProcesses,
        IReadOnlyList<ClubOffer> clubOffers,
        IReadOnlyList<PlayerContractProposal> contractProposals,
        IReadOnlyList<Promise> promises,
        IReadOnlyList<MemoryRecord> memories,
        IReadOnlyList<RelationshipRecord>? relationships = null,
        IReadOnlyList<DecisionRequest>? decisionRequests = null);

    CareerLoadResult Load(string filePath);
}

public sealed record CareerLoadResult(
    WorldTimeline Timeline,
    LeagueCompetition League,
    LeagueClubRegistry ClubRegistry,
    ManagerCareer ManagerCareer,
    IReadOnlyList<MatchSelection> MatchSelections,
    IReadOnlyList<WeeklyTrainingPlan> TrainingPlans,
    IReadOnlyList<PlayerPhysicalState> PhysicalStates,
    IReadOnlyList<PlayerCareerAggregate> PlayerCareers,
    IReadOnlyList<PlayerContract> Contracts,
    IReadOnlyList<ClubSquad> ClubSquads,
    IReadOnlyList<PlayerFreeAgency> FreeAgents,
    IReadOnlyList<TacticPlan> TacticPlans,
    IReadOnlyList<TransferNeed> TransferNeeds,
    IReadOnlyList<ShortlistEntry> ShortlistEntries,
    IReadOnlyList<TransferTarget> TransferTargets,
    IReadOnlyList<TransferProcess> TransferProcesses,
    IReadOnlyList<ClubOffer> ClubOffers,
    IReadOnlyList<PlayerContractProposal> ContractProposals,
    IReadOnlyList<Promise> Promises,
    IReadOnlyList<MemoryRecord> Memories,
    IReadOnlyList<RelationshipRecord> Relationships,
    IReadOnlyList<DecisionRequest> DecisionRequests,
    int SchemaVersion,
    bool WasMigrated);
