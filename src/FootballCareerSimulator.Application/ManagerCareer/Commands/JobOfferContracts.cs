namespace FootballCareerSimulator.Application.ManagerCareer.Commands;

public sealed record GenerateUnemployedJobOfferCommand(Guid CommandId);

public sealed record GenerateUnemployedJobOfferResult(
    bool Succeeded,
    bool WasAlreadyHeld,
    long? OfferId,
    long? ClubId);

public sealed record AcceptPendingJobOfferCommand(Guid CommandId);

public sealed record AcceptPendingJobOfferResult(
    bool Succeeded,
    long OfferId,
    long ClubId);
