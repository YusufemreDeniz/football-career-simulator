using FootballCareerSimulator.Application.Career.Services;
using FootballCareerSimulator.Application.CareerHub.Queries;
using FootballCareerSimulator.Application.ClubGovernance.Composition;
using FootballCareerSimulator.Application.Competition.Composition;
using FootballCareerSimulator.Application.Competition.Infrastructure;
using FootballCareerSimulator.Application.ContractRegistration.Composition;
using FootballCareerSimulator.Application.Interaction.Composition;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.PlayerCareer.Composition;
using FootballCareerSimulator.Application.PlayerCareer.Infrastructure;
using FootballCareerSimulator.Application.SocialContinuity.Composition;
using FootballCareerSimulator.Application.TeamPreparation.Composition;
using FootballCareerSimulator.Application.TeamPreparation.Infrastructure;
using FootballCareerSimulator.Application.TrainingPhysicalState.Composition;
using FootballCareerSimulator.Application.TrainingPhysicalState.Infrastructure;
using FootballCareerSimulator.Application.Transfer.Composition;
using FootballCareerSimulator.Application.WorldCalendar.Commands;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Infrastructure.Career;

namespace FootballCareerSimulator.Tests.CareerHub;

public sealed class SaveDeskDigestTests
{
    [Fact]
    public void NoSave_AsksForFirstCheckpoint()
    {
        var digest = SaveDeskDigest.Compose(
            savePath: @"C:\saves\career_save.db",
            saveExists: false,
            saveLastWriteUtc: null,
            currentDayNumber: 12,
            currentIsoDate: "2026-08-12",
            managerDisplayName: "Yusuf",
            clubDisplayName: "Home FC",
            seasonId: 1,
            seasonStatus: "Active",
            acceptedFixtureCount: 4,
            totalFixtureCount: 30);

        Assert.False(digest.SaveExists);
        Assert.Contains("Henüz kayıt yok", digest.Headline, StringComparison.Ordinal);
        Assert.Contains(digest.BeatLines, b => b.Contains("Home FC", StringComparison.Ordinal));
        Assert.Contains(digest.BeatLines, b => b.Contains("Diskte kayıt yok", StringComparison.Ordinal));
        Assert.Contains("Öneri:", digest.ToDisplayText(), StringComparison.Ordinal);
        Assert.Contains("İlk Kaydet", digest.AdviceLine, StringComparison.Ordinal);
    }

    [Fact]
    public void ExistingSave_ShowsDiskStampAndOverwriteHint()
    {
        var stamp = new DateTimeOffset(2026, 8, 10, 14, 30, 0, TimeSpan.Zero);
        var digest = SaveDeskDigest.Compose(
            @"C:\saves\career_save.db",
            saveExists: true,
            saveLastWriteUtc: stamp,
            currentDayNumber: 20,
            currentIsoDate: "2026-08-20",
            managerDisplayName: "Yusuf",
            clubDisplayName: null,
            seasonId: null,
            seasonStatus: null,
            acceptedFixtureCount: 0,
            totalFixtureCount: 0);

        Assert.True(digest.SaveExists);
        Assert.Contains("Kayıt mevcut", digest.Headline, StringComparison.Ordinal);
        Assert.Contains(digest.BeatLines, b => b.Contains("kulüpsüz", StringComparison.Ordinal));
        Assert.Contains(digest.BeatLines, b => b.StartsWith("Diskteki kayıt:", StringComparison.Ordinal));
        Assert.Contains("üzerine yazabilir", digest.Headline, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Telemetry_PreservesSlotSeedTimestampAndFileSize()
    {
        var stamp = new DateTimeOffset(2026, 9, 9, 16, 45, 0, TimeSpan.Zero);
        var digest = SaveDeskDigest.Compose(
            savePath: @"C:\saves\career_save.db",
            saveExists: true,
            saveLastWriteUtc: stamp,
            currentDayNumber: 30,
            currentIsoDate: "2026-09-09",
            managerDisplayName: "Yusuf",
            clubDisplayName: "Home FC",
            seasonId: 1,
            seasonStatus: "Active",
            acceptedFixtureCount: 8,
            totalFixtureCount: 34,
            saveSizeBytes: 2_621_440,
            activeSlot: "career_slot_01",
            worldSeed: 741852,
            rollingAutosaveCount: 3,
            latestAutosaveWriteUtc: stamp,
            lastLoadRecoveredFromBackup: true);

        Assert.Equal(stamp, digest.SaveLastWriteUtc);
        Assert.Equal(2_621_440, digest.SaveSizeBytes);
        Assert.Equal("career_slot_01", digest.ActiveSlot);
        Assert.Equal(741852, digest.WorldSeed);
        Assert.Equal(3, digest.RollingAutosaveCount);
        Assert.True(digest.LastLoadRecoveredFromBackup);
        Assert.Contains("Yedekten kurtarıldı", digest.Headline, StringComparison.Ordinal);
        Assert.Contains(digest.BeatLines, b => b.Contains("3/3 slot hazır", StringComparison.Ordinal));
    }

    [Fact]
    public void RollingAutosaveAndRecovery_RecoversWhenPrimarySaveIsCorrupted()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "fcs-autosave-recovery", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var savePath = Path.Combine(tempDir, "career_save.db");
            var (world, session) = CreateSession();

            // 1. İlk kayıt -> hem ana dosya hem autosave1 oluşur
            var save1 = session.Save(savePath);
            Assert.True(save1.Succeeded);
            Assert.Equal(1, save1.RollingAutosaveCount);

            // 2. Gün ilerlet ve tekrar kaydet -> autosave1 ve autosave2 oluşur
            var targetDay = world.TimelineStore.Timeline.CurrentDate.AddDays(4);
            world.AdvanceSimulationTime.Handle(new AdvanceSimulationTimeCommand(Guid.NewGuid(), targetDay.DayNumber));
            var save2 = session.Save(savePath);
            Assert.True(save2.Succeeded);
            Assert.Equal(2, save2.RollingAutosaveCount);

            // 3. Kritik geçiş autosave çağrısı -> 3 slot tamamlanır
            var auto = session.SaveRollingAutosave(savePath);
            Assert.True(auto.Succeeded);
            Assert.Equal(3, auto.RollingAutosaveCount);

            // 4. Ana kaydı boz ve yeni bir oturum üzerinden Load çağır
            File.WriteAllText(savePath, "CORRUPTED_SQLITE_HEADER_DATA");

            var (recoveryWorld, recoverySession) = CreateSession();
            var loadResult = recoverySession.Load(savePath);

            Assert.True(loadResult.Succeeded);
            Assert.True(loadResult.RecoveredFromBackup);
            Assert.NotNull(loadResult.RecoverySourcePath);
            Assert.Equal(targetDay.DayNumber, recoveryWorld.TimelineStore.Timeline.CurrentDate.DayNumber);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, recursive: true); } catch { }
            }
        }
    }

    private static (WorldCalendarModule World, CareerGameSessionService Session) CreateSession()
    {
        var startDay = GameDate.FromCalendarDate(2026, 7, 1);
        var world = WorldCalendarModule.Create(startDay, rootSeed: 42);
        var clubs = ClubGovernanceModule.CreateMvpLeague();
        var manager = ManagerCareerModule.CreateForCareer(
            startDay,
            clubs.Store,
            world.TimelineStore,
            startingClubId: 1);
        var trainingStore = new InMemoryTrainingPhysicalStateStore();
        var playerStore = new InMemoryPlayerCareerStore();
        var contracts = ContractRegistrationModule.Create(
            playerStore,
            manager.Store,
            world.TimelineStore,
            trainingStore: trainingStore);
        var players = PlayerCareerModule.Create(
            manager.Store,
            world.TimelineStore,
            trainingStore,
            playerStore,
            contracts.Registration);
        var competitionStore = new InMemoryLeagueCompetitionStore(
            new LeagueCompetition(new CompetitionId(1)));
        var selectionStore = new InMemoryMatchSelectionStore();
        var team = TeamPreparationModule.Create(
            competitionStore,
            manager.Store,
            selectionStore,
            trainingStore,
            world.TimelineStore,
            contracts.Store,
            playerStore);
        var training = TrainingPhysicalStateModule.Create(
            manager.Store,
            world.TimelineStore,
            trainingStore,
            players.Development,
            team.ClubSquad,
            selectionStore);
        var social = SocialContinuityModule.Create();
        var transfer = TransferModule.Create(
            contracts.Store,
            team.SquadStore,
            manager.Store,
            contracts.Registration,
            team.ClubSquad!,
            transferWindow: world.TransferWindowQuery,
            transferBudget: clubs.TransferBudget,
            wageBudget: clubs.WageBudget,
            clubRegistry: clubs.Store,
            freeAgentStore: contracts.FreeAgentStore);
        var interaction = InteractionModule.Create(
            manager.Store,
            social.PlayingTime,
            relationships: social.RelationshipEvaluation,
            decisionMemory: social.DecisionMemory,
            promiseStore: social.PromiseStore,
            startingOpportunity: social.StartingOpportunity,
            transferNeeds: transfer.Needs,
            relationshipStore: social.RelationshipStore,
            memoryStore: social.MemoryStore);
        var competition = CompetitionModule.CreateNewLeague();
        var session = new CareerGameSessionService(
            world.TimelineStore,
            competition.Store,
            clubs.Store,
            manager.Store,
            selectionStore,
            team.SquadStore,
            team.TacticPlanStore,
            transfer.NeedStore,
            transfer.ShortlistStore,
            transfer.TargetStore,
            transfer.ProcessStore,
            transfer.OfferStore,
            transfer.ProposalStore,
            social.PromiseStore,
            social.MemoryStore,
            social.RelationshipStore,
            interaction.DecisionRequestStore,
            interaction.DialogueSessionStore,
            interaction.DisciplinaryActionStore,
            trainingStore,
            playerStore,
            contracts.Store,
            contracts.FreeAgentStore,
            new CareerSqlitePersistence(),
            [world.AdvanceSimulationTime, training.IdempotencyReset]);
        return (world, session);
    }
}
