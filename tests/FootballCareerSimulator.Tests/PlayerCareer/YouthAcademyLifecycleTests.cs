using FootballCareerSimulator.Application.ClubGovernance.Composition;
using FootballCareerSimulator.Application.Competition.Infrastructure;
using FootballCareerSimulator.Application.ContractRegistration.Infrastructure;
using FootballCareerSimulator.Application.Interaction.Infrastructure;
using FootballCareerSimulator.Application.ManagerCareer.Composition;
using FootballCareerSimulator.Application.PlayerCareer.Infrastructure;
using FootballCareerSimulator.Application.PlayerCareer.Services;
using FootballCareerSimulator.Application.TeamPreparation.Infrastructure;
using FootballCareerSimulator.Application.WorldCalendar.Composition;
using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Simulation.PlayerCareer;

namespace FootballCareerSimulator.Tests.PlayerCareer;

public sealed class YouthAcademyLifecycleTests
{
    private static readonly GameDate IntakeDay = GameDate.FromCalendarDate(2026, 7, 1);

    [Fact]
    public void DevelopmentProjection_IsDeterministicSeasonalAndPotentialCapped()
    {
        var candidate = Assert.Single(MvpYouthAcademyIntakeGenerator.Generate(
            new ClubId(1),
            new SeasonId(3),
            rootSeed: 912,
            sportiveStrength: 95,
            rngVersion: "1").Take(1));

        var first = MvpYouthAcademyDevelopmentProjector.Project(candidate, 3, 912, "1");
        var repeated = MvpYouthAcademyDevelopmentProjector.Project(candidate, 3, 912, "1");
        var longTerm = MvpYouthAcademyDevelopmentProjector.Project(candidate, 30, 912, "1");

        Assert.Equal(first, repeated);
        Assert.Equal(candidate.Age + 3, first.Age);
        Assert.True(first.CurrentAbility > candidate.CurrentAbility);
        Assert.True(first.IsPromotionEligible);
        Assert.Equal(candidate.PotentialAbility, longTerm.CurrentAbility);
        Assert.Throws<NotSupportedException>(() =>
            MvpYouthAcademyDevelopmentProjector.Project(candidate, 1, 912, "2"));
    }

    [Fact]
    public void AcceptedCandidate_DevelopsAcrossSeasonAndPromotesWithYouthContract()
    {
        var context = CreateContext();
        var accepted = context.Intake.GetManagedClubIntake()!.Candidates[0];
        context.Intake.AcceptManagedCandidate(accepted.PlayerId);

        AdvanceToNextSeason(context);
        var academy = context.Lifecycle.GetManagedAcademy()!;
        var player = Assert.Single(academy.Players);

        Assert.Equal(YouthAcademyLifecycleStatus.PromotionEligible, player.Status);
        Assert.Equal(1, player.CompletedAcademySeasons);
        Assert.True(player.CurrentAbility > accepted.CurrentAbility);

        var promoted = context.Lifecycle.PromoteManagedCandidate(player.PlayerId);
        var promotedAgain = context.Lifecycle.PromoteManagedCandidate(player.PlayerId);

        Assert.Equal(promoted, promotedAgain);
        Assert.Equal(Domain.TeamPreparation.MatchSelection.MinSquadSlot, promoted.SquadSlot);
        Assert.Equal(Math.Max(500, promoted.CurrentAbility * 80), promoted.WeeklyWage);
        Assert.Contains(context.Careers.Careers, career => career.Id.Value == player.PlayerId);
        Assert.NotNull(context.Contracts.GetActiveForPlayer(
            new PlayerId(player.PlayerId),
            context.World.TimelineStore.Timeline.CurrentDate));
        Assert.True(context.Squads.Get(new ClubId(1))!.ContainsPlayer(new PlayerId(player.PlayerId)));
        Assert.Equal(
            YouthAcademyLifecycleStatus.PromotedToFirstTeam,
            context.Lifecycle.GetManagedAcademy()!.Players.Single().Status);
    }

    [Fact]
    public void RejectedAndPendingCandidates_DoNotEnterAcademyLifecycle()
    {
        var context = CreateContext();
        var intake = context.Intake.GetManagedClubIntake()!;
        context.Intake.RejectManagedCandidate(intake.Candidates[0].PlayerId);
        context.Intake.AcceptManagedCandidate(intake.Candidates[1].PlayerId);

        var academy = context.Lifecycle.GetManagedAcademy()!;

        var player = Assert.Single(academy.Players);
        Assert.Equal(intake.Candidates[1].PlayerId, player.PlayerId);
        Assert.Equal(YouthAcademyLifecycleStatus.Developing, player.Status);
        Assert.Throws<YouthAcademyLifecycleException>(() =>
            context.Lifecycle.PromoteManagedCandidate(player.PlayerId));
    }

    private static AcademyLifecycleContext CreateContext()
    {
        var world = WorldCalendarModule.Create(IntakeDay, rootSeed: 912);
        var clubs = ClubGovernanceModule.CreateMvpLeague();
        var club = clubs.Store.Registry.GetClubOrThrow(new ClubId(1));
        var manager = ManagerCareerModule.CreateNewCareer(
            IntakeDay,
            startingClubId: club.Id.Value,
            clubSportiveStrength: club.SportiveStrength);
        var league = new LeagueCompetition(new CompetitionId(1));
        var season = league.CreateSeason(new SeasonId(3), IntakeDay);
        foreach (var participant in clubs.Store.Registry.Clubs)
        {
            season.RegisterParticipant(participant.Id);
        }

        league.StartSeason(season.SeasonId, IntakeDay);
        var competition = new InMemoryLeagueCompetitionStore(league);
        var decisions = new InMemoryDecisionRequestStore();
        var careers = new InMemoryPlayerCareerStore();
        var contracts = new InMemoryContractStore();
        var squads = new InMemoryClubSquadStore();
        var intake = new YouthAcademyIntakeService(
            clubs.Store,
            competition,
            manager.Store,
            world.TimelineStore,
            decisions);
        var lifecycle = new YouthAcademyLifecycleService(
            clubs.Store,
            competition,
            manager.Store,
            world.TimelineStore,
            decisions,
            careers,
            contracts,
            squads);
        return new AcademyLifecycleContext(
            world,
            clubs,
            competition,
            decisions,
            careers,
            contracts,
            squads,
            intake,
            lifecycle);
    }

    private static void AdvanceToNextSeason(AcademyLifecycleContext context)
    {
        var transitionDay = GameDate.FromCalendarDate(2027, 7, 1);
        var participants = context.Clubs.Store.Registry.Clubs
            .Select(club => SeasonParticipant.Rehydrate(club.Id))
            .ToArray();
        var previous = CompetitionSeason.Rehydrate(
            new CompetitionId(1),
            new SeasonId(3),
            IntakeDay,
            SeasonStatus.Archived,
            IntakeDay,
            transitionDay,
            transitionDay,
            participants);
        var current = CompetitionSeason.Rehydrate(
            new CompetitionId(1),
            new SeasonId(4),
            transitionDay,
            SeasonStatus.Active,
            transitionDay,
            completedAt: null,
            archivedAt: null,
            participants: participants);
        context.Competition.Replace(LeagueCompetition.Rehydrate(
            new CompetitionId(1),
            [previous, current]));
        context.World.TimelineStore.Timeline.AdvanceTo(transitionDay);
    }

    private sealed record AcademyLifecycleContext(
        WorldCalendarModule World,
        ClubGovernanceModule Clubs,
        InMemoryLeagueCompetitionStore Competition,
        InMemoryDecisionRequestStore Decisions,
        InMemoryPlayerCareerStore Careers,
        InMemoryContractStore Contracts,
        InMemoryClubSquadStore Squads,
        YouthAcademyIntakeService Intake,
        YouthAcademyLifecycleService Lifecycle);
}
