using FootballCareerSimulator.Application.WorldCalendar.Ports;
using FootballCareerSimulator.Domain.ClubGovernance;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.TrainingPhysicalState;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Domain.ContractRegistration;
using FootballCareerSimulator.Domain.Transfer;
using PlayerCareerAggregate = FootballCareerSimulator.Domain.PlayerCareer.PlayerCareer;
using FootballCareerSimulator.Infrastructure.Career;

namespace FootballCareerSimulator.Infrastructure.WorldCalendar;

public sealed class WorldCalendarSqlitePersistence : IWorldCalendarPersistence
{
    private readonly CareerSqlitePersistence _careerPersistence = new();

    public void Save(string filePath, WorldTimeline timeline)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(timeline);

        var managerCareer = ManagerCareer.StartNewCareerForClubStrength(
            new ManagerId(1),
            "Teknik Direktör",
            new ClubId(1),
            timeline.CurrentDate,
            clubSportiveStrength: 50);

        _careerPersistence.Save(
            filePath,
            timeline,
            new LeagueCompetition(new CompetitionId(1)),
            LeagueClubRegistry.CreateMvpLeague(),
            managerCareer,
            Array.Empty<MatchSelection>(),
            Array.Empty<WeeklyTrainingPlan>(),
            Array.Empty<PlayerPhysicalState>(),
            Array.Empty<PlayerCareerAggregate>(),
            Array.Empty<PlayerContract>(),
            Array.Empty<ClubSquad>(),
            Array.Empty<PlayerFreeAgency>(),
            Array.Empty<TacticPlan>(),
            Array.Empty<TransferNeed>(),
            Array.Empty<ShortlistEntry>(),
            Array.Empty<TransferTarget>(),
            Array.Empty<TransferProcess>(),
            Array.Empty<ClubOffer>(),
            Array.Empty<PlayerContractProposal>());
    }

    public WorldCalendarLoadResult Load(string filePath)
    {
        var loaded = _careerPersistence.Load(filePath);
        return new WorldCalendarLoadResult(
            loaded.Timeline,
            loaded.SchemaVersion,
            loaded.WasMigrated);
    }
}
