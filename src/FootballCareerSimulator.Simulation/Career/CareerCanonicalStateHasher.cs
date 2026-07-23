using System.Security.Cryptography;
using System.Text;
using FootballCareerSimulator.Domain.ClubGovernance;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.ContractRegistration;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.TrainingPhysicalState;
using FootballCareerSimulator.Domain.WorldCalendar;
using ManagerCareerState = FootballCareerSimulator.Domain.ManagerCareer.ManagerCareer;
using PlayerCareerAggregate = FootballCareerSimulator.Domain.PlayerCareer.PlayerCareer;
using FootballCareerSimulator.Simulation.ClubGovernance;
using FootballCareerSimulator.Simulation.Competition;
using FootballCareerSimulator.Simulation.ContractRegistration;
using FootballCareerSimulator.Simulation.ManagerCareer;
using FootballCareerSimulator.Simulation.PlayerCareer;
using FootballCareerSimulator.Simulation.TeamPreparation;
using FootballCareerSimulator.Simulation.TrainingPhysicalState;
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
            Array.Empty<PlayerContract>());

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
            Array.Empty<PlayerContract>());

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
            Array.Empty<PlayerContract>());

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
            Array.Empty<PlayerContract>());

    public static string ComputeHash(
        WorldTimeline timeline,
        LeagueCompetition league,
        LeagueClubRegistry clubRegistry,
        ManagerCareerState managerCareer,
        IReadOnlyList<MatchSelection> matchSelections,
        IReadOnlyList<WeeklyTrainingPlan> trainingPlans,
        IReadOnlyList<PlayerPhysicalState> physicalStates,
        IReadOnlyList<PlayerCareerAggregate> playerCareers,
        IReadOnlyList<PlayerContract> contracts)
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
            ContractRegistrationCanonicalStateHasher.BuildCanonicalText(contracts));

        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonicalText));
        return Convert.ToHexString(hashBytes);
    }
}
