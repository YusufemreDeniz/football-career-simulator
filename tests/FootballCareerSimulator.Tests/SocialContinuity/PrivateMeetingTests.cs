using FootballCareerSimulator.Application.SocialContinuity.Infrastructure;
using FootballCareerSimulator.Application.SocialContinuity.Services;
using FootballCareerSimulator.Domain.ManagerCareer;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Tests.SocialContinuity;

public sealed class PrivateMeetingTests
{
    [Fact]
    public void ApplyPrivateMeeting_ChangesRelationshipAndIsIdempotentPerDay()
    {
        var store = new InMemoryRelationshipStore();
        var service = new RelationshipEvaluationService(store);
        var day = GameDate.FromCalendarDate(2027, 3, 8);

        var first = service.ApplyPrivateMeeting(
            new PlayerId(8),
            new ManagerId(3),
            day,
            PrivateMeetingTopic.Captaincy,
            managerAuthority: 70,
            playerAmbition: 82);
        var duplicate = service.ApplyPrivateMeeting(
            new PlayerId(8),
            new ManagerId(3),
            day,
            PrivateMeetingTopic.Captaincy,
            managerAuthority: 70,
            playerAmbition: 82);

        Assert.True(first.Applied);
        Assert.True(first.TrustDelta > 0);
        Assert.True(first.RespectDelta > 0);
        Assert.False(duplicate.Applied);
        Assert.Single(store.Relationships);
    }
}
