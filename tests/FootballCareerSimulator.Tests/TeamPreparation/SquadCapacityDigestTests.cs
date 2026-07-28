using FootballCareerSimulator.Application.TeamPreparation.Queries;
using FootballCareerSimulator.Domain.TeamPreparation;

namespace FootballCareerSimulator.Tests.TeamPreparation;

public sealed class SquadCapacityDigestTests
{
    [Fact]
    public void OverCapacity_ListsOverflowAndAdvice()
    {
        var digest = SquadCapacityDigest.Compose(
            26,
            25,
            ClubSquad.MaxMembers,
            [2001, 2002]);

        Assert.True(digest.IsOverCapacity);
        Assert.True(digest.IsFull);
        Assert.Contains("sığmıyor", digest.Headline, StringComparison.Ordinal);
        Assert.Contains("#2001", digest.ToDisplayText(), StringComparison.Ordinal);
        Assert.Contains("Taşanı Serbest Bırak", digest.AdviceLine, StringComparison.Ordinal);
        Assert.Contains("Satışa Çıkar", digest.AdviceLine, StringComparison.Ordinal);
        Assert.Contains("Öneri:", digest.ToDisplayText(), StringComparison.Ordinal);
    }

    [Fact]
    public void OpenSquad_ReportsFreeSlots()
    {
        var digest = SquadCapacityDigest.Compose(20, 20, ClubSquad.MaxMembers, Array.Empty<long>());
        Assert.False(digest.IsOverCapacity);
        Assert.Contains("açık", digest.Headline, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("5 slot boş", digest.AdviceLine, StringComparison.Ordinal);
    }

    [Fact]
    public void FullSquad_AdvicePointsToReleaseAction()
    {
        var digest = SquadCapacityDigest.Compose(
            ClubSquad.MaxMembers,
            ClubSquad.MaxMembers,
            ClubSquad.MaxMembers,
            Array.Empty<long>());
        Assert.True(digest.IsFull);
        Assert.Contains("Yer Aç", digest.AdviceLine, StringComparison.Ordinal);
        Assert.Contains("Satışa Çıkar", digest.AdviceLine, StringComparison.Ordinal);
    }
}
