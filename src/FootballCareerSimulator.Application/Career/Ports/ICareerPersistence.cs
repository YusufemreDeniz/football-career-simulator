namespace FootballCareerSimulator.Application.Career.Ports;

using FootballCareerSimulator.Domain.ClubGovernance;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.TrainingPhysicalState;
using FootballCareerSimulator.Domain.WorldCalendar;

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
        IReadOnlyList<PlayerPhysicalState> physicalStates);

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
    int SchemaVersion,
    bool WasMigrated);
