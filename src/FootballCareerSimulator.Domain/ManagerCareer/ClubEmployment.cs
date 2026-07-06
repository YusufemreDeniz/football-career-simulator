namespace FootballCareerSimulator.Domain.ManagerCareer;

using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;

public sealed class ClubEmployment
{
    private ClubEmployment(ClubId clubId, GameDate startedAt)
    {
        ClubId = clubId;
        StartedAt = startedAt;
    }

    public ClubId ClubId { get; }

    public GameDate StartedAt { get; }

    public static ClubEmployment Create(ClubId clubId, GameDate startedAt) => new(clubId, startedAt);

    public static ClubEmployment Rehydrate(ClubId clubId, GameDate startedAt) => new(clubId, startedAt);
}
