namespace FootballCareerSimulator.Domain.Competition;

using FootballCareerSimulator.Domain.Shared;

public sealed class SeasonParticipant
{
    internal SeasonParticipant(ClubId clubId)
    {
        ClubId = clubId;
    }

    public ClubId ClubId { get; }

    public static SeasonParticipant Rehydrate(ClubId clubId) => new(clubId);
}
