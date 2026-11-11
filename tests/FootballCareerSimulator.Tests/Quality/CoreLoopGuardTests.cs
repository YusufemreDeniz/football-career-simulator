using System.Reflection;
using System.Text.RegularExpressions;
using FootballCareerSimulator.Application.CareerWorld;
using FootballCareerSimulator.Application.ClubGovernance.Composition;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.SocialContinuity.Infrastructure;
using FootballCareerSimulator.Application.SocialContinuity.Services;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Domain.ClubGovernance;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.TrainingPhysicalState;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Infrastructure.Career;
using Microsoft.Data.Sqlite;

namespace FootballCareerSimulator.Tests.Quality;

/// <summary>
/// Çekirdek oyun döngüsünün regresyon korumaları. Bu testler 2026-11 denetiminde bulunan hata sınıflarının
/// (kayda yazılmayan durum, Presentation'ın store'a doğrudan yazması, telafinin zamanla geri alınması)
/// geri dönmesini engeller. Yeni kalıcı alan veya yeni kural eklerken buradaki ilgili testi genişlet.
/// </summary>
public sealed class CoreLoopGuardTests : IDisposable
{
    private const int Seed = 515151;
    private static readonly GameDate Opening = ProductionCareerWorldConstraints.DefaultOpeningDate;

    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        "fcs-core-guard",
        Guid.NewGuid().ToString("N"));

    public CoreLoopGuardTests() => Directory.CreateDirectory(_tempDirectory);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    // ------------------------------------------------------------------ Mimari

    [Fact]
    public void Presentation_DoesNotWriteDomainStoresDirectly()
    {
        // docs/17 §335: Presentation domain state'i doğrudan değiştiremez; yazımlar Application servislerinden geçer.
        var presentation = Path.Combine(FindRepositoryRoot(), "src", "FootballCareerSimulator.Presentation");
        var writePattern = new Regex(
            @"Store\s*\.\s*(Replace|ReplaceAll|ReplaceClub|Upsert|Remove\w*)\s*\(",
            RegexOptions.Compiled);

        var violations = Directory
            .EnumerateFiles(presentation, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}.godot{Path.DirectorySeparatorChar}"))
            .SelectMany(path => File.ReadLines(path)
                .Select((line, index) => (path, line, number: index + 1)))
            .Where(entry => writePattern.IsMatch(entry.line))
            .Select(entry => $"{Path.GetFileName(entry.path)}:{entry.number}: {entry.line.Trim()}")
            .ToArray();

        Assert.True(violations.Length == 0, "Presentation store'a doğrudan yazıyor:\n" + string.Join('\n', violations));
    }

    // ------------------------------------------------------------------ Kayıt kapsamı

    [Fact]
    public void SaveLoad_RoundTripsEveryPublicPropertyOfClubsManagerProgressionAndMemories()
    {
        var world = WorldCalendarModule.Create(Opening, rootSeed: Seed);
        var generated = ProductionCareerWorldBootstrap.Create(Seed, Opening);
        var clubs = ClubGovernanceModule.Create(generated.ClubRegistry);
        var offer = StartingCareerOfferService.Preview(Seed, StartingBackground.YouthAcademyCoach, Opening)[0];
        var manager = ManagerCareerModule.CreateFromAcceptedStartingOffer(
            Opening,
            clubs.Store,
            world.TimelineStore,
            StartingBackground.YouthAcademyCoach,
            Seed,
            offer.ClubId,
            displayName: "Deniz Arslan",
            clubSportiveStrength: offer.SportiveStrength);

        var clubId = new ClubId(offer.ClubId);
        var club = clubs.Store.Registry.GetClubOrThrow(clubId);
        if (club.FacilityLevel < Club.MaxFacilityLevel
            && club.AvailableTransferFunds >= Club.FacilityUpgradeCost(club.FacilityLevel))
        {
            clubs.TransferBudget.UpgradeFacilities(clubId);
        }

        manager.Store.Replace(manager.Store.Career.AddExperience(2_500));

        var player = new ActorRef(ActorKind.Player, 1001);
        var memories = new[]
        {
            MemoryRecord.CreateSelectionOmitted(new MemoryId(1), player, new FixtureId(1), Opening)
                .MarkResolution(MemoryResolutionStatus.PartiallyResolved, Opening.AddDays(7), "guard-partial"),
            MemoryRecord.CreateSelectionOmitted(new MemoryId(2), player, new FixtureId(2), Opening)
                .Reinforce("guard-reinforce", Opening.AddDays(3)),
        };

        var path = Path.Combine(_tempDirectory, "guard.db");
        new CareerSqlitePersistence().Save(
            path,
            world.TimelineStore.Timeline,
            new LeagueCompetition(generated.CompetitionId),
            clubs.Store.Registry,
            manager.Store.Career,
            Array.Empty<MatchSelection>(),
            Array.Empty<WeeklyTrainingPlan>(),
            Array.Empty<PlayerPhysicalState>(),
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
            Array.Empty<Promise>(),
            memories);

        var loaded = new CareerSqlitePersistence().Load(path);

        foreach (var original in clubs.Store.Registry.Clubs)
        {
            AssertSamePublicScalars(original, loaded.ClubRegistry.GetClubOrThrow(original.Id), $"Club {original.Id.Value}");
        }

        Assert.Equal(manager.Store.Career.Progression, loaded.ManagerCareer.Progression);
        foreach (var original in memories)
        {
            var roundTripped = loaded.Memories.Single(memory => memory.MemoryId == original.MemoryId);
            AssertSamePublicScalars(original, roundTripped, $"Memory {original.MemoryId.Value}");
        }
    }

    // ------------------------------------------------------------------ Redemption Memory

    [Theory]
    [InlineData(true, MemoryResolutionStatus.Resolved)]
    [InlineData(false, MemoryResolutionStatus.PartiallyResolved)]
    public void StartingAPlayer_RedeemsEarlierOmission(bool crucialMatch, MemoryResolutionStatus expected)
    {
        var store = new InMemoryMemoryStore();
        var service = new SelectionMemoryService(store);
        var playerId = new PlayerId(1005);
        service.RecordMatchday(new FixtureId(1), [], [], [playerId], Opening);
        var omitted = Assert.Single(store.Memories);
        Assert.Equal(MemoryValence.Negative, omitted.Valence);

        service.RecordMatchday(new FixtureId(2), [playerId], [], [playerId], Opening.AddDays(7), crucialMatch);

        var redeemed = store.Memories.Single(memory => memory.MemoryId == omitted.MemoryId);
        Assert.Equal(expected, redeemed.ResolutionStatus);
        Assert.True(redeemed.CurrentInfluence < omitted.CurrentInfluence);
    }

    [Fact]
    public void TimeDecay_DoesNotReviveARedeemedMemory()
    {
        var memory = MemoryRecord.CreateSelectionOmitted(
                new MemoryId(1),
                new ActorRef(ActorKind.Player, 1001),
                new FixtureId(1),
                Opening)
            .MarkResolution(MemoryResolutionStatus.Resolved, Opening.AddDays(1), "guard-resolved");

        var decayed = memory.ApplyTimeDecay(Opening.AddDays(30));

        Assert.Equal(0, decayed.CurrentInfluence);
        Assert.Equal(MemoryResolutionStatus.Resolved, decayed.ResolutionStatus);
    }

    [Fact]
    public void SameDayStart_IsNotRedeemedTwice()
    {
        var store = new InMemoryMemoryStore();
        var service = new SelectionMemoryService(store);
        var playerId = new PlayerId(1006);
        service.RecordMatchday(new FixtureId(1), [], [], [playerId], Opening);
        var day = Opening.AddDays(7);

        service.RecordMatchday(new FixtureId(2), [playerId], [], [playerId], day);
        var afterFirst = store.Memories.Single(memory => memory.Valence == MemoryValence.Negative).CurrentInfluence;
        service.ApplyStartRedemption([playerId], isCrucialMatch: false, day);
        var afterSecond = store.Memories.Single(memory => memory.Valence == MemoryValence.Negative).CurrentInfluence;

        Assert.Equal(afterFirst, afterSecond);
    }

    // ------------------------------------------------------------------ Club DNA

    [Fact]
    public void ClubDna_IsDeterministicAndBigClubsAreLessPatient()
    {
        var first = ClubDnaProfile.CreateSeeded(new ClubId(7), 82);
        var second = ClubDnaProfile.CreateSeeded(new ClubId(7), 82);
        Assert.Equal(first.ManagerialPatience, second.ManagerialPatience);
        Assert.Equal(first.YouthDevelopmentCulture, second.YouthDevelopmentCulture);

        var bigClubPatience = Enumerable.Range(1, 30)
            .Average(id => ClubDnaProfile.CreateSeeded(new ClubId(id), 85).ManagerialPatience);
        var smallClubPatience = Enumerable.Range(1, 30)
            .Average(id => ClubDnaProfile.CreateSeeded(new ClubId(id), 55).ManagerialPatience);
        Assert.True(bigClubPatience + 20 < smallClubPatience, $"big={bigClubPatience} small={smallClubPatience}");
    }

    [Fact]
    public void ClubDnaService_UsesSeededIdentityForRegisteredClubs()
    {
        var clubs = ClubGovernanceModule.CreateMvpLeague();
        var club = clubs.Store.Registry.Clubs.First();

        var profile = clubs.Dna.GetOrCreateProfile(club.Id);

        var expected = ClubDnaProfile.CreateSeeded(club.Id, club.SportiveStrength);
        Assert.Equal(expected.ManagerialPatience, profile.ManagerialPatience);
        Assert.Equal(expected.RecruitmentAggression, profile.RecruitmentAggression);
    }

    // ------------------------------------------------------------------ Sakatlıktan dönüş riski

    [Fact]
    public void ReturnRisk_RequiresAnInjuryHistory_NotJustLowFitness()
    {
        var tiredButNeverInjured = PlayerPhysicalState.Rehydrate(
            new PlayerId(1001), new ClubId(1), 0, fatigue: 60, fitness: 55);
        var returningFromInjury = PlayerPhysicalState.Rehydrate(
            new PlayerId(1002), new ClubId(1), 1, fatigue: 60, fitness: 55,
            lastInjuryReasonCode: PlayerPhysicalState.ReasonMatchLoad);
        var recoveredFitness = PlayerPhysicalState.Rehydrate(
            new PlayerId(1003), new ClubId(1), 2, fatigue: 20, fitness: 90,
            lastInjuryReasonCode: PlayerPhysicalState.ReasonMatchLoad);

        Assert.False(tiredButNeverInjured.HasElevatedReturnRisk);
        Assert.True(returningFromInjury.HasElevatedReturnRisk);
        Assert.False(recoveredFitness.HasElevatedReturnRisk);
    }

    // ------------------------------------------------------------------ Yardımcılar

    private static void AssertSamePublicScalars(object expected, object actual, string label)
    {
        foreach (var property in expected.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetIndexParameters().Length > 0)
            {
                continue;
            }

            var expectedValue = property.GetValue(expected);
            var actualValue = property.GetValue(actual);
            if (expectedValue is System.Collections.IEnumerable expectedItems and not string)
            {
                var actualItems = (System.Collections.IEnumerable?)actualValue;
                Assert.NotNull(actualItems);
                Assert.Equal(
                    expectedItems.Cast<object?>().Select(item => item?.ToString()).OrderBy(item => item),
                    actualItems!.Cast<object?>().Select(item => item?.ToString()).OrderBy(item => item));
                continue;
            }

            Assert.True(
                Equals(expectedValue, actualValue),
                $"{label}.{property.Name} kayıttan dönüşte değişti: '{expectedValue}' → '{actualValue}'. "
                + "Yeni alan eklediysen CareerSqlitePersistence, migration ve canonical hash'e de ekle.");
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "FootballCareerSimulator.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("Depo kökü (FootballCareerSimulator.slnx) bulunamadı.");
    }
}
