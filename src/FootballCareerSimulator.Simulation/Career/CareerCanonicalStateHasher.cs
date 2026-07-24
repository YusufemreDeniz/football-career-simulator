using System.Security.Cryptography;
using System.Text;
using FootballCareerSimulator.Domain.ClubGovernance;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.ContractRegistration;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.TrainingPhysicalState;
using FootballCareerSimulator.Domain.Interaction;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.Transfer;
using FootballCareerSimulator.Domain.WorldCalendar;
using ManagerCareerState = FootballCareerSimulator.Domain.ManagerCareer.ManagerCareer;
using PlayerCareerAggregate = FootballCareerSimulator.Domain.PlayerCareer.PlayerCareer;
using FootballCareerSimulator.Simulation.ClubGovernance;
using FootballCareerSimulator.Simulation.Competition;
using FootballCareerSimulator.Simulation.ContractRegistration;
using FootballCareerSimulator.Simulation.Interaction;
using FootballCareerSimulator.Simulation.ManagerCareer;
using FootballCareerSimulator.Simulation.PlayerCareer;
using FootballCareerSimulator.Simulation.SocialContinuity;
using FootballCareerSimulator.Simulation.TeamPreparation;
using FootballCareerSimulator.Simulation.TrainingPhysicalState;
using FootballCareerSimulator.Simulation.Transfer;
using FootballCareerSimulator.Simulation.WorldCalendar;

namespace FootballCareerSimulator.Simulation.Career;

public static class CareerCanonicalStateHasher
{
    public static string ComputeHash(
        WorldTimeline timeline,
        LeagueCompetition league,
        LeagueClubRegistry clubRegistry,
        ManagerCareerState managerCareer) =>
        ComputeHash(
            timeline,
            league,
            clubRegistry,
            managerCareer,
            Array.Empty<MatchSelection>(),
            Array.Empty<WeeklyTrainingPlan>(),
            Array.Empty<PlayerPhysicalState>(),
            Array.Empty<PlayerCareerAggregate>(),
            Array.Empty<PlayerContract>(),
            Array.Empty<ClubSquad>(),
            Array.Empty<PlayerFreeAgency>());

    public static string ComputeHash(
        WorldTimeline timeline,
        LeagueCompetition league,
        LeagueClubRegistry clubRegistry,
        ManagerCareerState managerCareer,
        IReadOnlyList<MatchSelection> matchSelections) =>
        ComputeHash(
            timeline,
            league,
            clubRegistry,
            managerCareer,
            matchSelections,
            Array.Empty<WeeklyTrainingPlan>(),
            Array.Empty<PlayerPhysicalState>(),
            Array.Empty<PlayerCareerAggregate>(),
            Array.Empty<PlayerContract>(),
            Array.Empty<ClubSquad>(),
            Array.Empty<PlayerFreeAgency>());

    public static string ComputeHash(
        WorldTimeline timeline,
        LeagueCompetition league,
        LeagueClubRegistry clubRegistry,
        ManagerCareerState managerCareer,
        IReadOnlyList<MatchSelection> matchSelections,
        IReadOnlyList<WeeklyTrainingPlan> trainingPlans,
        IReadOnlyList<PlayerPhysicalState> physicalStates) =>
        ComputeHash(
            timeline,
            league,
            clubRegistry,
            managerCareer,
            matchSelections,
            trainingPlans,
            physicalStates,
            Array.Empty<PlayerCareerAggregate>(),
            Array.Empty<PlayerContract>(),
            Array.Empty<ClubSquad>(),
            Array.Empty<PlayerFreeAgency>());

    public static string ComputeHash(
        WorldTimeline timeline,
        LeagueCompetition league,
        LeagueClubRegistry clubRegistry,
        ManagerCareerState managerCareer,
        IReadOnlyList<MatchSelection> matchSelections,
        IReadOnlyList<WeeklyTrainingPlan> trainingPlans,
        IReadOnlyList<PlayerPhysicalState> physicalStates,
        IReadOnlyList<PlayerCareerAggregate> playerCareers) =>
        ComputeHash(
            timeline,
            league,
            clubRegistry,
            managerCareer,
            matchSelections,
            trainingPlans,
            physicalStates,
            playerCareers,
            Array.Empty<PlayerContract>(),
            Array.Empty<ClubSquad>(),
            Array.Empty<PlayerFreeAgency>());

    public static string ComputeHash(
        WorldTimeline timeline,
        LeagueCompetition league,
        LeagueClubRegistry clubRegistry,
        ManagerCareerState managerCareer,
        IReadOnlyList<MatchSelection> matchSelections,
        IReadOnlyList<WeeklyTrainingPlan> trainingPlans,
        IReadOnlyList<PlayerPhysicalState> physicalStates,
        IReadOnlyList<PlayerCareerAggregate> playerCareers,
        IReadOnlyList<PlayerContract> contracts) =>
        ComputeHash(
            timeline,
            league,
            clubRegistry,
            managerCareer,
            matchSelections,
            trainingPlans,
            physicalStates,
            playerCareers,
            contracts,
            Array.Empty<ClubSquad>(),
            Array.Empty<PlayerFreeAgency>());

    public static string ComputeHash(
        WorldTimeline timeline,
        LeagueCompetition league,
        LeagueClubRegistry clubRegistry,
        ManagerCareerState managerCareer,
        IReadOnlyList<MatchSelection> matchSelections,
        IReadOnlyList<WeeklyTrainingPlan> trainingPlans,
        IReadOnlyList<PlayerPhysicalState> physicalStates,
        IReadOnlyList<PlayerCareerAggregate> playerCareers,
        IReadOnlyList<PlayerContract> contracts,
        IReadOnlyList<ClubSquad> clubSquads) =>
        ComputeHash(
            timeline,
            league,
            clubRegistry,
            managerCareer,
            matchSelections,
            trainingPlans,
            physicalStates,
            playerCareers,
            contracts,
            clubSquads,
            Array.Empty<PlayerFreeAgency>());

    public static string ComputeHash(
        WorldTimeline timeline,
        LeagueCompetition league,
        LeagueClubRegistry clubRegistry,
        ManagerCareerState managerCareer,
        IReadOnlyList<MatchSelection> matchSelections,
        IReadOnlyList<WeeklyTrainingPlan> trainingPlans,
        IReadOnlyList<PlayerPhysicalState> physicalStates,
        IReadOnlyList<PlayerCareerAggregate> playerCareers,
        IReadOnlyList<PlayerContract> contracts,
        IReadOnlyList<ClubSquad> clubSquads,
        IReadOnlyList<PlayerFreeAgency> freeAgents) =>
        ComputeHash(
            timeline,
            league,
            clubRegistry,
            managerCareer,
            matchSelections,
            trainingPlans,
            physicalStates,
            playerCareers,
            contracts,
            clubSquads,
            freeAgents,
            Array.Empty<TacticPlan>(),
            Array.Empty<TransferNeed>());

    public static string ComputeHash(
        WorldTimeline timeline,
        LeagueCompetition league,
        LeagueClubRegistry clubRegistry,
        ManagerCareerState managerCareer,
        IReadOnlyList<MatchSelection> matchSelections,
        IReadOnlyList<WeeklyTrainingPlan> trainingPlans,
        IReadOnlyList<PlayerPhysicalState> physicalStates,
        IReadOnlyList<PlayerCareerAggregate> playerCareers,
        IReadOnlyList<PlayerContract> contracts,
        IReadOnlyList<ClubSquad> clubSquads,
        IReadOnlyList<PlayerFreeAgency> freeAgents,
        IReadOnlyList<TacticPlan> tacticPlans) =>
        ComputeHash(
            timeline,
            league,
            clubRegistry,
            managerCareer,
            matchSelections,
            trainingPlans,
            physicalStates,
            playerCareers,
            contracts,
            clubSquads,
            freeAgents,
            tacticPlans,
            Array.Empty<TransferNeed>());

    public static string ComputeHash(
        WorldTimeline timeline,
        LeagueCompetition league,
        LeagueClubRegistry clubRegistry,
        ManagerCareerState managerCareer,
        IReadOnlyList<MatchSelection> matchSelections,
        IReadOnlyList<WeeklyTrainingPlan> trainingPlans,
        IReadOnlyList<PlayerPhysicalState> physicalStates,
        IReadOnlyList<PlayerCareerAggregate> playerCareers,
        IReadOnlyList<PlayerContract> contracts,
        IReadOnlyList<ClubSquad> clubSquads,
        IReadOnlyList<PlayerFreeAgency> freeAgents,
        IReadOnlyList<TacticPlan> tacticPlans,
        IReadOnlyList<TransferNeed> transferNeeds) =>
        ComputeHash(
            timeline,
            league,
            clubRegistry,
            managerCareer,
            matchSelections,
            trainingPlans,
            physicalStates,
            playerCareers,
            contracts,
            clubSquads,
            freeAgents,
            tacticPlans,
            transferNeeds,
            Array.Empty<ShortlistEntry>(),
            Array.Empty<TransferTarget>());

    public static string ComputeHash(
        WorldTimeline timeline,
        LeagueCompetition league,
        LeagueClubRegistry clubRegistry,
        ManagerCareerState managerCareer,
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
        IReadOnlyList<TransferTarget> transferTargets) =>
        ComputeHash(
            timeline,
            league,
            clubRegistry,
            managerCareer,
            matchSelections,
            trainingPlans,
            physicalStates,
            playerCareers,
            contracts,
            clubSquads,
            freeAgents,
            tacticPlans,
            transferNeeds,
            shortlistEntries,
            transferTargets,
            Array.Empty<TransferProcess>());

    public static string ComputeHash(
        WorldTimeline timeline,
        LeagueCompetition league,
        LeagueClubRegistry clubRegistry,
        ManagerCareerState managerCareer,
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
        IReadOnlyList<TransferProcess> transferProcesses) =>
        ComputeHash(
            timeline,
            league,
            clubRegistry,
            managerCareer,
            matchSelections,
            trainingPlans,
            physicalStates,
            playerCareers,
            contracts,
            clubSquads,
            freeAgents,
            tacticPlans,
            transferNeeds,
            shortlistEntries,
            transferTargets,
            transferProcesses,
            Array.Empty<ClubOffer>());

    public static string ComputeHash(
        WorldTimeline timeline,
        LeagueCompetition league,
        LeagueClubRegistry clubRegistry,
        ManagerCareerState managerCareer,
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
        IReadOnlyList<ClubOffer> clubOffers) =>
        ComputeHash(
            timeline,
            league,
            clubRegistry,
            managerCareer,
            matchSelections,
            trainingPlans,
            physicalStates,
            playerCareers,
            contracts,
            clubSquads,
            freeAgents,
            tacticPlans,
            transferNeeds,
            shortlistEntries,
            transferTargets,
            transferProcesses,
            clubOffers,
            Array.Empty<PlayerContractProposal>());

    public static string ComputeHash(
        WorldTimeline timeline,
        LeagueCompetition league,
        LeagueClubRegistry clubRegistry,
        ManagerCareerState managerCareer,
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
        IReadOnlyList<PlayerContractProposal> contractProposals) =>
        ComputeHash(
            timeline,
            league,
            clubRegistry,
            managerCareer,
            matchSelections,
            trainingPlans,
            physicalStates,
            playerCareers,
            contracts,
            clubSquads,
            freeAgents,
            tacticPlans,
            transferNeeds,
            shortlistEntries,
            transferTargets,
            transferProcesses,
            clubOffers,
            contractProposals,
            Array.Empty<Promise>());

    public static string ComputeHash(
        WorldTimeline timeline,
        LeagueCompetition league,
        LeagueClubRegistry clubRegistry,
        ManagerCareerState managerCareer,
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
        IReadOnlyList<Promise> promises) =>
        ComputeHash(
            timeline,
            league,
            clubRegistry,
            managerCareer,
            matchSelections,
            trainingPlans,
            physicalStates,
            playerCareers,
            contracts,
            clubSquads,
            freeAgents,
            tacticPlans,
            transferNeeds,
            shortlistEntries,
            transferTargets,
            transferProcesses,
            clubOffers,
            contractProposals,
            promises,
            Array.Empty<MemoryRecord>());

    public static string ComputeHash(
        WorldTimeline timeline,
        LeagueCompetition league,
        LeagueClubRegistry clubRegistry,
        ManagerCareerState managerCareer,
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
        IReadOnlyList<DecisionRequest>? decisionRequests = null)
    {
        ArgumentNullException.ThrowIfNull(timeline);
        ArgumentNullException.ThrowIfNull(league);
        ArgumentNullException.ThrowIfNull(clubRegistry);
        ArgumentNullException.ThrowIfNull(managerCareer);
        ArgumentNullException.ThrowIfNull(matchSelections);
        ArgumentNullException.ThrowIfNull(trainingPlans);
        ArgumentNullException.ThrowIfNull(physicalStates);
        ArgumentNullException.ThrowIfNull(playerCareers);
        ArgumentNullException.ThrowIfNull(contracts);
        ArgumentNullException.ThrowIfNull(clubSquads);
        ArgumentNullException.ThrowIfNull(freeAgents);
        ArgumentNullException.ThrowIfNull(tacticPlans);
        ArgumentNullException.ThrowIfNull(transferNeeds);
        ArgumentNullException.ThrowIfNull(shortlistEntries);
        ArgumentNullException.ThrowIfNull(transferTargets);
        ArgumentNullException.ThrowIfNull(transferProcesses);
        ArgumentNullException.ThrowIfNull(clubOffers);
        ArgumentNullException.ThrowIfNull(contractProposals);
        ArgumentNullException.ThrowIfNull(promises);
        ArgumentNullException.ThrowIfNull(memories);
        relationships ??= Array.Empty<RelationshipRecord>();
        decisionRequests ??= Array.Empty<DecisionRequest>();

        var canonicalText = string.Concat(
            WorldTimelineCanonicalStateHasher.BuildCanonicalText(timeline),
            "|",
            CompetitionCanonicalStateHasher.BuildCanonicalText(league),
            "|",
            ClubRegistryCanonicalStateHasher.BuildCanonicalText(clubRegistry),
            "|",
            ManagerCareerCanonicalStateHasher.BuildCanonicalText(managerCareer),
            "|",
            MatchSelectionCanonicalStateHasher.BuildCanonicalText(matchSelections),
            "|",
            TrainingPhysicalStateCanonicalStateHasher.BuildCanonicalText(trainingPlans, physicalStates),
            "|",
            PlayerCareerCanonicalStateHasher.BuildCanonicalText(playerCareers),
            "|",
            ContractRegistrationCanonicalStateHasher.BuildCanonicalText(contracts),
            "|",
            ClubSquadCanonicalStateHasher.BuildCanonicalText(clubSquads),
            "|",
            FreeAgencyCanonicalStateHasher.BuildCanonicalText(freeAgents),
            "|",
            TacticPlanCanonicalStateHasher.BuildCanonicalText(tacticPlans),
            "|",
            TransferNeedCanonicalStateHasher.BuildCanonicalText(transferNeeds),
            "|",
            ShortlistTargetCanonicalStateHasher.BuildCanonicalText(shortlistEntries, transferTargets),
            "|",
            TransferProcessCanonicalStateHasher.BuildCanonicalText(transferProcesses),
            "|",
            ClubOfferCanonicalStateHasher.BuildCanonicalText(clubOffers),
            "|",
            PlayerContractProposalCanonicalStateHasher.BuildCanonicalText(contractProposals),
            "|",
            PromiseCanonicalStateHasher.BuildCanonicalText(promises),
            "|",
            MemoryCanonicalStateHasher.BuildCanonicalText(memories),
            "|",
            RelationshipCanonicalStateHasher.BuildCanonicalText(relationships),
            "|",
            DecisionRequestCanonicalStateHasher.BuildCanonicalText(decisionRequests));

        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonicalText));
        return Convert.ToHexString(hashBytes);
    }
}
