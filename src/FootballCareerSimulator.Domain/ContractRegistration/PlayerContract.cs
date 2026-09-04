using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Domain.ContractRegistration;

public enum ContractStatus
{
    Active = 1,
    Expired = 2,
}

/// <summary>
/// Oyuncu-kulüp hukuki bağlılığı (MVP: tek aktif sözleşme, sade ücret/süre).
/// </summary>
public sealed class PlayerContract
{
    private PlayerContract(
        ContractId id,
        PlayerId playerId,
        ClubId clubId,
        GameDate startDate,
        GameDate endDate,
        int weeklyWage,
        ContractStatus status)
    {
        Id = id;
        PlayerId = playerId;
        ClubId = clubId;
        StartDate = startDate;
        EndDate = endDate;
        WeeklyWage = weeklyWage;
        Status = status;
    }

    public ContractId Id { get; }

    public PlayerId PlayerId { get; }

    public ClubId ClubId { get; }

    public GameDate StartDate { get; }

    public GameDate EndDate { get; }

    public int WeeklyWage { get; }

    public ContractStatus Status { get; }

    public static PlayerContract Activate(
        PlayerId playerId,
        ClubId clubId,
        GameDate startDate,
        GameDate endDate,
        int weeklyWage)
    {
        if (endDate.DayNumber < startDate.DayNumber)
        {
            throw new ContractRegistrationInvariantViolationException(
                "Contract end date cannot be before start date.");
        }

        if (weeklyWage < 0)
        {
            throw new ContractRegistrationInvariantViolationException(
                "Weekly wage cannot be negative.");
        }

        return new PlayerContract(
            ContractId.ForPlayer(playerId.Value),
            playerId,
            clubId,
            startDate,
            endDate,
            weeklyWage,
            ContractStatus.Active);
    }

    public static PlayerContract Rehydrate(
        ContractId id,
        PlayerId playerId,
        ClubId clubId,
        GameDate startDate,
        GameDate endDate,
        int weeklyWage,
        ContractStatus status)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ContractRegistrationInvariantViolationException(
                $"Unknown contract status: {status}.");
        }

        if (endDate.DayNumber < startDate.DayNumber)
        {
            throw new ContractRegistrationInvariantViolationException(
                "Contract end date cannot be before start date.");
        }

        if (weeklyWage < 0)
        {
            throw new ContractRegistrationInvariantViolationException(
                "Weekly wage cannot be negative.");
        }

        return new PlayerContract(id, playerId, clubId, startDate, endDate, weeklyWage, status);
    }

    public bool IsActiveOn(GameDate day) =>
        Status == ContractStatus.Active
        && day.DayNumber >= StartDate.DayNumber
        && day.DayNumber <= EndDate.DayNumber;

    public PlayerContract ExpireIfDue(GameDate day)
    {
        if (Status != ContractStatus.Active)
        {
            return this;
        }

        if (day.DayNumber <= EndDate.DayNumber)
        {
            return this;
        }

        return new PlayerContract(
            Id,
            PlayerId,
            ClubId,
            StartDate,
            EndDate,
            WeeklyWage,
            ContractStatus.Expired);
    }

    /// <summary>
    /// Kulüp kararıyla erken fesih — süre bitmeden serbest ajanlığa geçiş.
    /// </summary>
    public PlayerContract ReleaseEarly(GameDate day)
    {
        if (Status != ContractStatus.Active)
        {
            throw new ContractRegistrationInvariantViolationException(
                $"Contract {Id.Value} is not active.");
        }

        if (!IsActiveOn(day))
        {
            throw new ContractRegistrationInvariantViolationException(
                $"Contract {Id.Value} is not active on day {day.DayNumber}.");
        }

        return new PlayerContract(
            Id,
            PlayerId,
            ClubId,
            StartDate,
            EndDate,
            WeeklyWage,
            ContractStatus.Expired);
    }
}
