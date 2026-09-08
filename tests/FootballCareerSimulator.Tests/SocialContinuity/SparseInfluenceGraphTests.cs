using FootballCareerSimulator.Application.SocialContinuity.Services;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Tests.SocialContinuity;

public sealed class SparseInfluenceGraphTests
{
    private static readonly ClubId TestClubId = new(1);
    private static readonly GameDate Day1 = GameDate.FromCalendarDate(2026, 8, 1);
    private static readonly GameDate Day5 = GameDate.FromCalendarDate(2026, 8, 5);
    private static readonly GameDate Day20 = GameDate.FromCalendarDate(2026, 8, 20);

    [Fact]
    public void AddInfluenceLink_EnforcesMaxTiesPerPlayerBound()
    {
        var service = new SocialContagionService();
        const long captainId = 10;

        // Add 4 allowed ties
        service.AddInfluenceLink(new SparseInfluenceLink(captainId, targetPlayerId: 11, "ViceCaptain", 80));
        service.AddInfluenceLink(new SparseInfluenceLink(captainId, targetPlayerId: 12, "Mentor", 70));
        service.AddInfluenceLink(new SparseInfluenceLink(captainId, targetPlayerId: 13, "AgeCohort", 60));
        service.AddInfluenceLink(new SparseInfluenceLink(captainId, targetPlayerId: 14, "AgeCohort", 60));

        // 5th tie must throw invariant violation to preserve bounded retention
        Assert.Throws<SocialContinuityInvariantViolationException>(() =>
            service.AddInfluenceLink(new SparseInfluenceLink(captainId, targetPlayerId: 15, "AgeCohort", 50)));
    }

    [Fact]
    public void PropagateContagion_DepthIs1_OnlyAffectsDirectTies()
    {
        var service = new SocialContagionService();
        const long captainId = 10;
        const long viceCaptainId = 11;
        const long youngPlayerId = 12;

        // Captain -> ViceCaptain (tie exists)
        service.AddInfluenceLink(new SparseInfluenceLink(captainId, viceCaptainId, "ViceCaptain", strength: 100));
        // ViceCaptain -> YoungPlayer (tie exists)
        service.AddInfluenceLink(new SparseInfluenceLink(viceCaptainId, youngPlayerId, "Mentor", strength: 100));

        // When Captain suffers fallout, only direct tie (ViceCaptain) is impacted, depth=1 limit!
        var impacted = service.PropagateContagion(captainId, trustLoss: 20, Day1, TestClubId);

        Assert.Single(impacted);
        Assert.Equal(viceCaptainId, impacted[0].TargetPlayerId);
        Assert.Equal(7, impacted[0].SecondaryTrustDelta); // 20 * 1.0 * 0.35 = 7
    }

    [Fact]
    public void PropagateContagion_Cooldown_PreventsDuplicateContagionWithin14Days()
    {
        var service = new SocialContagionService();
        const long captainId = 10;
        const long viceCaptainId = 11;

        service.AddInfluenceLink(new SparseInfluenceLink(captainId, viceCaptainId, "ViceCaptain", strength: 80));

        // Day 1: First fallout event propagates
        var impactedDay1 = service.PropagateContagion(captainId, trustLoss: 20, Day1, TestClubId);
        Assert.Single(impactedDay1);

        // Day 5 (within 14-day cooldown): Second fallout event must NOT propagate again
        var impactedDay5 = service.PropagateContagion(captainId, trustLoss: 20, Day5, TestClubId);
        Assert.Empty(impactedDay5);

        // Day 20 (after 14-day cooldown): Third fallout event can propagate
        var impactedDay20 = service.PropagateContagion(captainId, trustLoss: 20, Day20, TestClubId);
        Assert.Single(impactedDay20);
    }

    [Fact]
    public void PropagateContagion_IncreasesFrictionAndShiftsClimateState()
    {
        var service = new SocialContagionService();
        const long captainId = 10;

        service.AddInfluenceLink(new SparseInfluenceLink(captainId, 11, "ViceCaptain", 90));
        service.AddInfluenceLink(new SparseInfluenceLink(captainId, 12, "Mentor", 90));
        service.AddInfluenceLink(new SparseInfluenceLink(captainId, 13, "AgeCohort", 90));
        service.AddInfluenceLink(new SparseInfluenceLink(captainId, 14, "AgeCohort", 90));

        var initialDigest = service.BuildDigest(TestClubId);
        Assert.Equal("Kenetlenmiş", initialDigest.ClimateStateTurkish);

        // Severe captain fallout impacting 4 teammates
        service.PropagateContagion(captainId, trustLoss: 30, Day1, TestClubId);

        var updatedDigest = service.BuildDigest(TestClubId);
        Assert.True(updatedDigest.FrictionScore >= 44);
        Assert.Equal("Bölünmüş", updatedDigest.ClimateStateTurkish);
        Assert.Contains("Soyunma odasında klikleşme ve gerginlik gözleniyor.", updatedDigest.KeyObservations);
    }
}
