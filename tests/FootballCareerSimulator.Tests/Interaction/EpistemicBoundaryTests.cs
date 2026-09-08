using FootballCareerSimulator.Application.Interaction.Services;
using FootballCareerSimulator.Domain.Interaction;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Tests.Interaction;

public sealed class EpistemicBoundaryTests
{
    private static readonly GameDate Day1 = GameDate.FromCalendarDate(2026, 8, 1);
    private static readonly GameDate Day15 = GameDate.FromCalendarDate(2026, 8, 15);
    private static readonly GameDate Day35 = GameDate.FromCalendarDate(2026, 9, 5);

    [Fact]
    public void PrivateDiscussion_IsNotPublic_DoesNotAppearInMediaStoryDigest()
    {
        var service = new EpistemicKnowledgeService();
        var privateClaim = new ClaimRecord(
            claimId: 1,
            subjectKey: "ManagerPlayerMeeting:42:101",
            claimantActor: new ActorRef(ActorKind.Player, 101),
            contentCode: "Oyuncu menajerle tartıştı",
            credibility: 90,
            isPublic: false,
            publishedOn: Day1);

        service.PublishClaim(privateClaim);

        var digest = service.BuildMediaStoryDigest();
        Assert.Empty(digest.Stories);
    }

    [Fact]
    public void PublicClaim_AppearsInMediaDigest_WithAppropriateReliabilityTag()
    {
        var service = new EpistemicKnowledgeService();

        var verifiedNews = new ClaimRecord(
            claimId: 1,
            subjectKey: "TransferAgreement",
            claimantActor: new ActorRef(ActorKind.Manager, 42),
            contentCode: "Yıldız forvet imza attı",
            credibility: 95,
            isPublic: true,
            publishedOn: Day1,
            isVerifiedTruth: true);

        var rumorNews = new ClaimRecord(
            claimId: 2,
            subjectKey: "TransferRumor",
            claimantActor: new ActorRef(ActorKind.Player, 99),
            contentCode: "Büyük kulübe transfer olmak istiyor",
            credibility: 50,
            isPublic: true,
            publishedOn: Day1,
            isVerifiedTruth: false);

        service.PublishClaim(verifiedNews);
        service.PublishClaim(rumorNews);

        var digest = service.BuildMediaStoryDigest();
        Assert.Equal(2, digest.Stories.Count);

        var verified = digest.Stories.Single(s => s.ClaimId == 1);
        Assert.Equal("Doğrulandı", verified.ReliabilityTag);
        Assert.True(verified.IsVerifiedTruth);

        var rumor = digest.Stories.Single(s => s.ClaimId == 2);
        Assert.Equal("İddia", rumor.ReliabilityTag);
        Assert.False(rumor.IsVerifiedTruth);
    }

    [Fact]
    public void ActorBelief_FormedFromClaim_PreservesConfidenceLevel()
    {
        var service = new EpistemicKnowledgeService();
        var claim = new ClaimRecord(
            claimId: 10,
            subjectKey: "ManagerPromise",
            claimantActor: new ActorRef(ActorKind.Player, 7),
            contentCode: "Hoca bana ilk 11 sözü verdi",
            credibility: 80,
            isPublic: true,
            publishedOn: Day1);

        service.PublishClaim(claim);

        var observer = new ActorRef(ActorKind.Player, 8);
        service.FormBelief(observer, "ManagerPromise", claimId: 10, Day1);

        var belief = service.GetBelief(observer, "ManagerPromise");
        Assert.NotNull(belief);
        Assert.Equal(10, belief.BelievedClaimId);
        Assert.Equal(80, belief.ConfidenceLevel);
    }

    [Fact]
    public void RumorRecord_TtlExpiration_PruningPurgesStaleRumors()
    {
        var service = new EpistemicKnowledgeService();
        var rumor = new RumorRecord(
            rumorId: "Rumor_1",
            sourceContext: "LockerRoom",
            topicCode: "UnhappyCaptain",
            narrativeText: "Kaptan takımdan ayrılmak istiyor",
            spreadDepth: 1,
            createdOn: Day1,
            ttlDays: 20); // Expires around Day 21

        service.SpreadRumor(rumor);

        // Day 15: Rumor is active
        var activeOnDay15 = service.GetActiveRumors(Day15);
        Assert.Single(activeOnDay15);

        var digestOnDay15 = service.BuildLockerRoomRumorDigest(Day15);
        Assert.Equal(1, digestOnDay15.TotalActiveCount);
        Assert.Equal(6, digestOnDay15.ActiveRumors[0].DaysRemaining); // 1 + 20 - 15 = 6

        // Day 35: Rumor is expired
        Assert.False(rumor.IsActive(Day35));
        service.PruneExpiredRumors(Day35);

        var activeOnDay35 = service.GetActiveRumors(Day35);
        Assert.Empty(activeOnDay35);
        Assert.Equal(0, service.BuildLockerRoomRumorDigest(Day35).TotalActiveCount);
    }
}
