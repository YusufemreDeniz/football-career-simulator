using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Domain.ManagerCareer;

public readonly record struct JobOfferId : IComparable<JobOfferId>
{
    public long Value { get; }

    public JobOfferId(long value)
    {
        if (value < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Job offer id must be positive.");
        }

        Value = value;
    }

    public int CompareTo(JobOfferId other) => Value.CompareTo(other.Value);
}

public enum JobOfferStatus
{
    Offered = 1,
    Accepted = 2,
}

public sealed class JobOffer
{
    private JobOffer(JobOfferId id, ClubId clubId, JobOfferStatus status, GameDate createdAt)
    {
        Id = id;
        ClubId = clubId;
        Status = status;
        CreatedAt = createdAt;
    }

    public JobOfferId Id { get; }

    public ClubId ClubId { get; }

    public JobOfferStatus Status { get; }

    public GameDate CreatedAt { get; }

    public static JobOffer CreateOffered(JobOfferId id, ClubId clubId, GameDate createdAt) =>
        new(id, clubId, JobOfferStatus.Offered, createdAt);

    public static JobOffer Rehydrate(JobOfferId id, ClubId clubId, JobOfferStatus status, GameDate createdAt) =>
        new(id, clubId, status, createdAt);

    public JobOffer MarkAccepted()
    {
        if (Status != JobOfferStatus.Offered)
        {
            throw new ManagerCareerInvariantViolationException(
                "Only an offered job offer can be accepted.");
        }

        return new JobOffer(Id, ClubId, JobOfferStatus.Accepted, CreatedAt);
    }
}
