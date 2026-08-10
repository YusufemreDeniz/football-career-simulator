using FootballCareerSimulator.Domain.TeamPreparation;

namespace FootballCareerSimulator.Application.TeamPreparation.Queries;

public sealed record TacticPlanReadModel(
    long? ClubId,
    string FormationName,
    string ApproachName,
    int LastUpdatedDayNumber,
    string PressingName = "Dengeli pres",
    string DefensiveLineName = "Standart hat",
    string PassingStyleName = "Dengeli pas",
    Formation Formation = Formation.F442,
    TacticalApproach Approach = TacticalApproach.Balanced,
    PressingIntensity Pressing = PressingIntensity.Balanced,
    DefensiveLine DefensiveLine = DefensiveLine.Standard,
    PassingStyle PassingStyle = PassingStyle.Balanced);
