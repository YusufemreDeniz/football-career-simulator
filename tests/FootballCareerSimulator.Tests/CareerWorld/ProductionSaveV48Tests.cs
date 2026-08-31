using FootballCareerSimulator.Application.CareerWorld;
using FootballCareerSimulator.Application.ClubGovernance.Composition;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.TeamPreparation.Infrastructure;
using FootballCareerSimulator.Application.TeamPreparation.Ports;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Infrastructure.Career;
using Microsoft.Data.Sqlite;

namespace FootballCareerSimulator.Tests.CareerWorld;

public sealed class ProductionSaveV48Tests : IDisposable
{
    private const int Seed = 848484;
    private static readonly GameDate Opening = ProductionCareerWorldConstraints.DefaultOpeningDate;

    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        "fcs-v48-save",
        Guid.NewGuid().ToString("N"));

    public ProductionSaveV48Tests() => Directory.CreateDirectory(_tempDirectory);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void SaveLoad_PreservesStartingBackgroundAndLineupTemplate()
    {
        var (path, clubId, startingSlots, benchSlots) = SaveCareerWithTemplate();

        var loaded = new CareerSqlitePersistence().Load(path);
        Assert.Equal(48, loaded.SchemaVersion);
        Assert.False(loaded.WasMigrated);
        Assert.Equal(StartingBackground.TacticalSpecialist, loaded.ManagerCareer.StartingBackground);
        Assert.Equal("Aylin Kaya", loaded.ManagerCareer.DisplayName);
        var template = Assert.Single(loaded.LineupTemplates!);
        Assert.Equal(clubId, template.ClubId);
        Assert.Equal(startingSlots, template.StartingSlotIndices);
        Assert.Equal(benchSlots, template.BenchSlotIndices);
    }

    [Fact]
    public void Load_V47Save_MigratesBackgroundFromReasonCode()
    {
        var (path, _, _, _) = SaveCareerWithTemplate();
        DowngradeToV47(path);

        var loaded = new CareerSqlitePersistence().Load(path);
        Assert.True(loaded.WasMigrated);
        Assert.Equal(48, loaded.SchemaVersion);
        Assert.Equal(StartingBackground.TacticalSpecialist, loaded.ManagerCareer.StartingBackground);
        Assert.Equal("StartBackground:TacticalSpecialist", loaded.ManagerCareer.LastReputationReasonCode);
        Assert.Empty(loaded.LineupTemplates ?? []);
        Assert.True(File.Exists(path + ".bak"));
    }

    private (string Path, ClubId ClubId, int[] StartingSlots, int[] BenchSlots) SaveCareerWithTemplate()
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

        var startingSlots = Enumerable.Range(1, MatchSelection.StartingXiSize).ToArray();
        var benchSlots = Enumerable.Range(12, MatchSelection.MaxBenchSize).ToArray();
        var selections = new InMemoryMatchSelectionStore();
        selections.Upsert(MatchSelection.Approve(
            new FixtureId(1),
            new ClubId(chosen.ClubId),
            startingSlots,
            benchSlots));
        selections.RemoveForFixture(new FixtureId(1));

        var path = Path.Combine(_tempDirectory, Guid.NewGuid().ToString("N") + ".db");
        new CareerSqlitePersistence().Save(
            path,
            world.TimelineStore.Timeline,
            new LeagueCompetition(generated.CompetitionId),
            generated.ClubRegistry,
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
            lineupTemplates: selections.LineupTemplates);

        return (path, new ClubId(chosen.ClubId), startingSlots, benchSlots);
    }

    private static void DowngradeToV47(string path)
    {
        SqliteConnection.ClearAllPools();
        using var connection = new SqliteConnection($"Data Source={path}");
        connection.Open();
        using var dropColumn = connection.CreateCommand();
        dropColumn.CommandText = "ALTER TABLE ManagerCareerState DROP COLUMN StartingBackground;";
        dropColumn.ExecuteNonQuery();
        using var dropTemplates = connection.CreateCommand();
        dropTemplates.CommandText = "DROP TABLE IF EXISTS ClubLineupTemplateState;";
        dropTemplates.ExecuteNonQuery();
        using var setVersion = connection.CreateCommand();
        setVersion.CommandText = "UPDATE ProductionSaveManifest SET SchemaVersion = 47;";
        setVersion.ExecuteNonQuery();
    }
}
