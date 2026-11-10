using FootballCareerSimulator.Application.CareerWorld;
using FootballCareerSimulator.Application.ClubGovernance.Composition;
using FootballCareerSimulator.Application.Continental;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Domain.ClubGovernance;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Infrastructure.Career;
using Microsoft.Data.Sqlite;

namespace FootballCareerSimulator.Tests.CareerWorld;

/// <summary>
/// V50: kulüp tesis seviyesi, menajer ilerlemesi ve kıtasal turnuva özeti kayda yazılır.
/// </summary>
public sealed class ProductionSaveV50Tests : IDisposable
{
    private const int Seed = 505050;
    private static readonly GameDate Opening = ProductionCareerWorldConstraints.DefaultOpeningDate;

    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        "fcs-v50-save",
        Guid.NewGuid().ToString("N"));

    public ProductionSaveV50Tests() => Directory.CreateDirectory(_tempDirectory);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void SaveLoad_PreservesFacilityLevelProgressionAndContinentalRecord()
    {
        var scenario = SaveScenario();

        var loaded = new CareerSqlitePersistence().Load(scenario.Path);

        Assert.Equal(51, loaded.SchemaVersion);
        Assert.False(loaded.WasMigrated);

        var club = loaded.ClubRegistry.GetClubOrThrow(scenario.ClubId);
        Assert.Equal(scenario.FacilityLevel, club.FacilityLevel);
        Assert.Equal(scenario.SpentTransferFunds, club.SpentTransferFunds);

        var progression = loaded.ManagerCareer.Progression;
        Assert.Equal(scenario.Progression.Level, progression.Level);
        Assert.Equal(scenario.Progression.ExperiencePoints, progression.ExperiencePoints);
        Assert.Equal(scenario.Progression.Skills, progression.Skills);

        var continental = Assert.IsType<ContinentalTournamentRecord>(loaded.ContinentalTournament);
        Assert.Equal(scenario.Continental.SeasonStartYear, continental.SeasonStartYear);
        Assert.Equal(scenario.Continental.ManagedClubId, continental.ManagedClubId);
        Assert.Equal(scenario.Continental.ManagedClubStrength, continental.ManagedClubStrength);
        Assert.Equal(scenario.Continental.ManagedResults, continental.ManagedResults);
    }

    [Fact]
    public void Load_V49Save_MigratesFacilityToStrengthDefaultAndProgressionToInitial()
    {
        var scenario = SaveScenario();
        DowngradeToV49(scenario.Path);

        var loaded = new CareerSqlitePersistence().Load(scenario.Path);

        Assert.True(loaded.WasMigrated);
        Assert.Equal(51, loaded.SchemaVersion);
        var club = loaded.ClubRegistry.GetClubOrThrow(scenario.ClubId);
        Assert.Equal(Club.DefaultFacilityLevel(club.SportiveStrength), club.FacilityLevel);
        Assert.Equal(1, loaded.ManagerCareer.Progression.Level);
        Assert.Equal(0, loaded.ManagerCareer.Progression.ExperiencePoints);
        Assert.Equal(
            ManagerSkillProfile.ForBackground(StartingBackground.TacticalSpecialist),
            loaded.ManagerCareer.Progression.Skills);
        Assert.Null(loaded.ContinentalTournament);
    }

    private sealed record Scenario(
        string Path,
        ClubId ClubId,
        int FacilityLevel,
        int SpentTransferFunds,
        ManagerProgression Progression,
        ContinentalTournamentRecord Continental);

    private Scenario SaveScenario()
    {
        var world = WorldCalendarModule.Create(Opening, rootSeed: Seed);
        var generated = ProductionCareerWorldBootstrap.Create(Seed, Opening);
        var clubs = ClubGovernanceModule.Create(generated.ClubRegistry);
        var offers = StartingCareerOfferService.Preview(Seed, StartingBackground.TacticalSpecialist, Opening);
        var chosen = offers[0];
        var manager = ManagerCareerModule.CreateFromAcceptedStartingOffer(
            Opening,
            clubs.Store,
            world.TimelineStore,
            StartingBackground.TacticalSpecialist,
            Seed,
            chosen.ClubId,
            displayName: "Aylin Kaya",
            clubSportiveStrength: chosen.SportiveStrength);

        var clubId = new ClubId(chosen.ClubId);
        var before = clubs.Store.Registry.GetClubOrThrow(clubId);
        if (before.FacilityLevel < Club.MaxFacilityLevel
            && before.AvailableTransferFunds >= Club.FacilityUpgradeCost(before.FacilityLevel))
        {
            clubs.TransferBudget.UpgradeFacilities(clubId);
        }

        var upgraded = clubs.Store.Registry.GetClubOrThrow(clubId);
        manager.Experience.AwardContinentalMatch(managedGoals: 3, opponentGoals: 0);
        manager.Store.Replace(manager.Store.Career.AddExperience(1_000));
        var progression = manager.Store.Career.Progression;
        Assert.True(progression.Level > 1);

        var continental = new ContinentalTournamentRecord(
            2026,
            chosen.ClubId,
            chosen.SportiveStrength,
            [new ContinentalManagedResult(3, 2, 1), new ContinentalManagedResult(7, 0, 0)]);

        var path = System.IO.Path.Combine(_tempDirectory, Guid.NewGuid().ToString("N") + ".db");
        new CareerSqlitePersistence().Save(
            path,
            world.TimelineStore.Timeline,
            new LeagueCompetition(generated.CompetitionId),
            clubs.Store.Registry,
            manager.Store.Career,
            Array.Empty<MatchSelection>(),
            Array.Empty<Domain.TrainingPhysicalState.WeeklyTrainingPlan>(),
            Array.Empty<Domain.TrainingPhysicalState.PlayerPhysicalState>(),
            Array.Empty<Domain.PlayerCareer.PlayerCareer>(),
            Array.Empty<Domain.ContractRegistration.PlayerContract>(),
            Array.Empty<ClubSquad>(),
            Array.Empty<Domain.ContractRegistration.PlayerFreeAgency>(),
            Array.Empty<TacticPlan>(),
            Array.Empty<Domain.Transfer.TransferNeed>(),
            Array.Empty<Domain.Transfer.ShortlistEntry>(),
            Array.Empty<Domain.Transfer.TransferTarget>(),
            Array.Empty<Domain.Transfer.TransferProcess>(),
            Array.Empty<Domain.Transfer.ClubOffer>(),
            Array.Empty<Domain.Transfer.PlayerContractProposal>(),
            Array.Empty<Domain.SocialContinuity.Promise>(),
            Array.Empty<Domain.SocialContinuity.MemoryRecord>(),
            continentalTournament: continental);

        return new Scenario(
            path,
            clubId,
            upgraded.FacilityLevel,
            upgraded.SpentTransferFunds,
            progression,
            continental);
    }

    private static void DowngradeToV49(string path)
    {
        SqliteConnection.ClearAllPools();
        using var connection = new SqliteConnection($"Data Source={path}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            ALTER TABLE ClubState DROP COLUMN FacilityLevel;
            ALTER TABLE ManagerCareerState DROP COLUMN ManagerLevel;
            ALTER TABLE ManagerCareerState DROP COLUMN ManagerExperiencePoints;
            ALTER TABLE ManagerCareerState DROP COLUMN SkillTacticalAcumen;
            ALTER TABLE ManagerCareerState DROP COLUMN SkillYouthDevelopment;
            ALTER TABLE ManagerCareerState DROP COLUMN SkillManManagement;
            ALTER TABLE ManagerCareerState DROP COLUMN SkillNegotiation;
            DROP TABLE IF EXISTS ContinentalManagedResultState;
            DROP TABLE IF EXISTS ContinentalTournamentState;
            UPDATE ProductionSaveManifest SET SchemaVersion = 49;
            """;
        command.ExecuteNonQuery();
    }
}
