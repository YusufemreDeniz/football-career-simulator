namespace FootballCareerSimulator.Domain.ManagerCareer;

using FootballCareerSimulator.Domain.Competition;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;

public sealed class ManagerCareer
{
    private ManagerCareer(
        ManagerId managerId,
        string displayName,
        ClubEmployment? activeEmployment,
        ManagerEmploymentStatus employmentStatus,
        EmploymentEndReason? terminationReason,
        ClubId? lastClubId,
        FixtureId? dismissedDueToFixtureId,
        GameDate? dismissedAt,
        JobOffer? pendingJobOffer,
        ManagerReputation reputation,
        string? lastReputationReasonCode)
    {
        ManagerId = managerId;
        DisplayName = displayName;
        ActiveEmployment = activeEmployment;
        EmploymentStatus = employmentStatus;
        TerminationReason = terminationReason;
        LastClubId = lastClubId;
        DismissedDueToFixtureId = dismissedDueToFixtureId;
        DismissedAt = dismissedAt;
        PendingJobOffer = pendingJobOffer;
        Reputation = reputation;
        LastReputationReasonCode = lastReputationReasonCode;
    }

    public ManagerId ManagerId { get; }

    public string DisplayName { get; }

    public ClubEmployment? ActiveEmployment { get; }

    public ManagerEmploymentStatus EmploymentStatus { get; }

    public EmploymentEndReason? TerminationReason { get; }

    public ClubId? LastClubId { get; }

    public FixtureId? DismissedDueToFixtureId { get; }

    public GameDate? DismissedAt { get; }

    public JobOffer? PendingJobOffer { get; }

    public ManagerReputation Reputation { get; }

    public string? LastReputationReasonCode { get; }

    public bool IsEmployed =>
        EmploymentStatus == ManagerEmploymentStatus.Employed && ActiveEmployment is not null;

    public static ManagerCareer StartNewCareer(
        ManagerId managerId,
        string displayName,
        ClubId startingClubId,
        GameDate startedAt,
        SeasonExpectationTier seasonExpectation,
        int initialBoardConfidence = BoardConfidence.DefaultInitialValue)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ManagerCareerInvariantViolationException("Manager display name cannot be empty.");
        }

        return new ManagerCareer(
            managerId,
            displayName.Trim(),
            ClubEmployment.Create(
                startingClubId,
                startedAt,
                seasonExpectation,
                new BoardConfidence(initialBoardConfidence)),
            ManagerEmploymentStatus.Employed,
            terminationReason: null,
            lastClubId: startingClubId,
            dismissedDueToFixtureId: null,
            dismissedAt: null,
            pendingJobOffer: null,
            new ManagerReputation(ManagerReputation.DefaultInitialValue),
            lastReputationReasonCode: null);
    }

    public static ManagerCareer StartNewCareerForClubStrength(
        ManagerId managerId,
        string displayName,
        ClubId startingClubId,
        GameDate startedAt,
        int clubSportiveStrength,
        int initialBoardConfidence = BoardConfidence.DefaultInitialValue) =>
        StartNewCareer(
            managerId,
            displayName,
            startingClubId,
            startedAt,
            SeasonExpectation.FromSportiveStrength(clubSportiveStrength),
            initialBoardConfidence);

    public static ManagerCareer Rehydrate(
        ManagerId managerId,
        string displayName,
        ClubEmployment? activeEmployment,
        ManagerEmploymentStatus employmentStatus,
        EmploymentEndReason? terminationReason,
        ClubId? lastClubId,
        FixtureId? dismissedDueToFixtureId,
        GameDate? dismissedAt,
        JobOffer? pendingJobOffer = null,
        ManagerReputation? reputation = null,
        string? lastReputationReasonCode = null)
    {
        if (employmentStatus == ManagerEmploymentStatus.Employed && activeEmployment is null)
        {
            throw new ManagerCareerInvariantViolationException(
                "Employed manager career requires active employment.");
        }

        if (employmentStatus == ManagerEmploymentStatus.Unemployed && activeEmployment is not null)
        {
            throw new ManagerCareerInvariantViolationException(
                "Unemployed manager career cannot keep active employment.");
        }

        if (employmentStatus == ManagerEmploymentStatus.Employed && pendingJobOffer is not null)
        {
            throw new ManagerCareerInvariantViolationException(
                "Employed manager cannot hold a pending job offer.");
        }

        return new ManagerCareer(
            managerId,
            displayName,
            activeEmployment,
            employmentStatus,
            terminationReason,
            lastClubId,
            dismissedDueToFixtureId,
            dismissedAt,
            pendingJobOffer,
            reputation ?? new ManagerReputation(ManagerReputation.DefaultInitialValue),
            lastReputationReasonCode);
    }

    public static ManagerCareer Rehydrate(
        ManagerId managerId,
        string displayName,
        ClubEmployment? activeEmployment) =>
        activeEmployment is null
            ? Rehydrate(
                managerId,
                displayName,
                activeEmployment: null,
                ManagerEmploymentStatus.Unemployed,
                terminationReason: EmploymentEndReason.Dismissed,
                lastClubId: null,
                dismissedDueToFixtureId: null,
                dismissedAt: null)
            : Rehydrate(
                managerId,
                displayName,
                activeEmployment,
                ManagerEmploymentStatus.Employed,
                terminationReason: null,
                lastClubId: activeEmployment.ClubId,
                dismissedDueToFixtureId: null,
                dismissedAt: null);

    public BoardAssessmentResult ApplyMatchBoardAssessment(
        FixtureId fixtureId,
        MatchOutcomeForManagedClub matchOutcome,
        int leaguePosition,
        int leagueSize)
    {
        if (ActiveEmployment is null)
        {
            throw new ManagerCareerInvariantViolationException(
                "Board assessment requires active employment.");
        }

        if (ActiveEmployment.LastAssessedFixtureId == fixtureId)
        {
            return BoardAssessmentResult.AlreadyApplied(this, ActiveEmployment);
        }

        var meetsExpectation = SeasonExpectation.MeetsExpectation(
            ActiveEmployment.SeasonExpectation,
            leaguePosition,
            leagueSize);

        var (delta, reasonCode) = ComputeDelta(matchOutcome, meetsExpectation);
        var newConfidence = ActiveEmployment.BoardConfidence.Adjust(delta);
        var updatedEmployment = ActiveEmployment.WithBoardAssessment(fixtureId, newConfidence, reasonCode);
        var updatedCareer = new ManagerCareer(
            ManagerId,
            DisplayName,
            updatedEmployment,
            ManagerEmploymentStatus.Employed,
            terminationReason: null,
            lastClubId: updatedEmployment.ClubId,
            dismissedDueToFixtureId: null,
            dismissedAt: null,
            pendingJobOffer: null,
            Reputation,
            LastReputationReasonCode);

        return BoardAssessmentResult.Applied(updatedCareer, updatedEmployment, delta, reasonCode);
    }

    /// <summary>
    /// Yönetim talebi (BoardDemand) yanıtı → Board Confidence. Dialogue bu metodu doğrudan çağırmaz.
    /// </summary>
    public BoardAssessmentResult ApplyBoardDemandResponse(string optionCode)
    {
        if (ActiveEmployment is null)
        {
            throw new ManagerCareerInvariantViolationException(
                "Board demand response requires active employment.");
        }

        if (string.IsNullOrWhiteSpace(optionCode))
        {
            throw new ManagerCareerInvariantViolationException("Board demand option code is required.");
        }

        var trimmed = optionCode.Trim();
        var (delta, reasonCode) = trimmed switch
        {
            "AcceptBoardDemand" => (8, "BoardDemandAccepted"),
            "CounterBoardDemand" => (-4, "BoardDemandCountered"),
            "Refuse" => (-12, "BoardDemandRefused"),
            _ => throw new ManagerCareerInvariantViolationException(
                $"Unsupported board demand option: {trimmed}."),
        };

        var newConfidence = ActiveEmployment.BoardConfidence.Adjust(delta);
        var updatedEmployment = ActiveEmployment.WithBoardConfidenceAdjustment(newConfidence, reasonCode);
        var updatedCareer = new ManagerCareer(
            ManagerId,
            DisplayName,
            updatedEmployment,
            ManagerEmploymentStatus.Employed,
            terminationReason: null,
            lastClubId: updatedEmployment.ClubId,
            dismissedDueToFixtureId: null,
            dismissedAt: null,
            pendingJobOffer: null,
            Reputation,
            LastReputationReasonCode);

        return BoardAssessmentResult.Applied(updatedCareer, updatedEmployment, delta, reasonCode);
    }

    /// <summary>
    /// Kritik basın kamuya açık anlatısı → Manager Reputation. Dialogue doğrudan mutasyon yapamaz.
    /// </summary>
    public ManagerReputationChangeResult ApplyPressPublicNarrative(string optionCode)
    {
        if (string.IsNullOrWhiteSpace(optionCode))
        {
            throw new ManagerCareerInvariantViolationException("Press narrative option code is required.");
        }

        var trimmed = optionCode.Trim();
        var (delta, reasonCode) = trimmed switch
        {
            "PubliclyDefend" => (2, "PressPubliclyDefend"),
            "PubliclyCriticize" => (-3, "PressPubliclyCriticize"),
            _ => throw new ManagerCareerInvariantViolationException(
                $"Unsupported press narrative option: {trimmed}."),
        };

        var nextReputation = Reputation.Adjust(delta);
        var updatedCareer = new ManagerCareer(
            ManagerId,
            DisplayName,
            ActiveEmployment,
            EmploymentStatus,
            TerminationReason,
            LastClubId,
            DismissedDueToFixtureId,
            DismissedAt,
            PendingJobOffer,
            nextReputation,
            reasonCode);

        return ManagerReputationChangeResult.Applied(updatedCareer, delta, reasonCode);
    }

    public DismissalResult DismissDueToBoardConfidence(FixtureId causationFixtureId, GameDate dismissedAt)
    {
        if (DismissedDueToFixtureId == causationFixtureId
            && EmploymentStatus == ManagerEmploymentStatus.Unemployed)
        {
            return DismissalResult.AlreadyApplied(this);
        }

        if (!IsEmployed || ActiveEmployment is null)
        {
            return DismissalResult.AlreadyApplied(this);
        }

        if (ActiveEmployment.RiskBand != EmploymentRiskBand.Critical)
        {
            throw new ManagerCareerInvariantViolationException(
                "Dismissal requires Critical employment risk band.");
        }

        var unemployed = new ManagerCareer(
            ManagerId,
            DisplayName,
            activeEmployment: null,
            ManagerEmploymentStatus.Unemployed,
            terminationReason: EmploymentEndReason.Dismissed,
            lastClubId: ActiveEmployment.ClubId,
            dismissedDueToFixtureId: causationFixtureId,
            dismissedAt: dismissedAt,
            pendingJobOffer: null,
            Reputation,
            LastReputationReasonCode);

        return DismissalResult.Applied(unemployed, ActiveEmployment.ClubId, causationFixtureId);
    }

    public JobOfferReceiveResult ReceiveJobOffer(JobOffer offer)
    {
        ArgumentNullException.ThrowIfNull(offer);

        if (IsEmployed)
        {
            throw new ManagerCareerInvariantViolationException(
                "Employed manager cannot receive a job offer.");
        }

        if (PendingJobOffer is { Status: JobOfferStatus.Offered })
        {
            if (PendingJobOffer.Id == offer.Id)
            {
                return JobOfferReceiveResult.AlreadyHeld(this);
            }

            throw new ManagerCareerInvariantViolationException(
                "A pending job offer already exists; accept or clear it first.");
        }

        if (offer.Status != JobOfferStatus.Offered)
        {
            throw new ManagerCareerInvariantViolationException(
                "Only Offered-status offers can be received.");
        }

        var updated = new ManagerCareer(
            ManagerId,
            DisplayName,
            activeEmployment: null,
            ManagerEmploymentStatus.Unemployed,
            TerminationReason,
            LastClubId,
            DismissedDueToFixtureId,
            DismissedAt,
            offer,
            Reputation,
            LastReputationReasonCode);

        return JobOfferReceiveResult.Received(updated, offer);
    }

    public JobOfferAcceptResult AcceptPendingJobOffer(
        GameDate startedAt,
        SeasonExpectationTier seasonExpectation,
        int initialBoardConfidence = BoardConfidence.DefaultInitialValue)
    {
        if (IsEmployed)
        {
            throw new ManagerCareerInvariantViolationException(
                "Already employed; cannot accept a job offer.");
        }

        if (PendingJobOffer is not { Status: JobOfferStatus.Offered } offer)
        {
            throw new ManagerCareerInvariantViolationException(
                "No pending offered job offer to accept.");
        }

        var employment = ClubEmployment.Create(
            offer.ClubId,
            startedAt,
            seasonExpectation,
            new BoardConfidence(initialBoardConfidence));

        var employed = new ManagerCareer(
            ManagerId,
            DisplayName,
            employment,
            ManagerEmploymentStatus.Employed,
            terminationReason: null,
            lastClubId: employment.ClubId,
            dismissedDueToFixtureId: null,
            dismissedAt: null,
            pendingJobOffer: null,
            Reputation,
            LastReputationReasonCode);

        return JobOfferAcceptResult.Accepted(employed, offer.Id, employment.ClubId);
    }

    private static (int Delta, string ReasonCode) ComputeDelta(
        MatchOutcomeForManagedClub outcome,
        bool meetsExpectation) =>
        (outcome, meetsExpectation) switch
        {
            (MatchOutcomeForManagedClub.Win, true) => (5, "WinOnTrack"),
            (MatchOutcomeForManagedClub.Win, false) => (3, "WinBehindExpectation"),
            (MatchOutcomeForManagedClub.Draw, true) => (0, "DrawOnTrack"),
            (MatchOutcomeForManagedClub.Draw, false) => (-2, "DrawBehindExpectation"),
            (MatchOutcomeForManagedClub.Loss, true) => (-3, "LossOnTrack"),
            (MatchOutcomeForManagedClub.Loss, false) => (-6, "LossBehindExpectation"),
            _ => (0, "Neutral"),
        };
}

public enum MatchOutcomeForManagedClub
{
    Win = 1,
    Draw = 2,
    Loss = 3,
}

public sealed record BoardAssessmentResult(
    bool WasApplied,
    bool WasAlreadyApplied,
    ManagerCareer Career,
    int ConfidenceDelta,
    int BoardConfidence,
    EmploymentRiskBand RiskBand,
    SeasonExpectationTier SeasonExpectation,
    string? ReasonCode)
{
    public static BoardAssessmentResult AlreadyApplied(ManagerCareer career, ClubEmployment employment) =>
        new(
            WasApplied: false,
            WasAlreadyApplied: true,
            career,
            ConfidenceDelta: 0,
            employment.BoardConfidence.Value,
            employment.RiskBand,
            employment.SeasonExpectation,
            employment.LastAssessmentReasonCode);

    public static BoardAssessmentResult Applied(
        ManagerCareer career,
        ClubEmployment employment,
        int delta,
        string reasonCode) =>
        new(
            WasApplied: true,
            WasAlreadyApplied: false,
            career,
            delta,
            employment.BoardConfidence.Value,
            employment.RiskBand,
            employment.SeasonExpectation,
            reasonCode);
}

public sealed record DismissalResult(
    bool WasApplied,
    bool WasAlreadyApplied,
    ManagerCareer Career,
    long? DismissedFromClubId,
    long? CausationFixtureId)
{
    public static DismissalResult AlreadyApplied(ManagerCareer career) =>
        new(false, true, career, career.LastClubId?.Value, career.DismissedDueToFixtureId?.Value);

    public static DismissalResult Applied(
        ManagerCareer career,
        ClubId fromClubId,
        FixtureId causationFixtureId) =>
        new(true, false, career, fromClubId.Value, causationFixtureId.Value);
}

public sealed record JobOfferReceiveResult(
    bool WasReceived,
    bool WasAlreadyHeld,
    ManagerCareer Career,
    long? OfferId,
    long? ClubId)
{
    public static JobOfferReceiveResult AlreadyHeld(ManagerCareer career) =>
        new(
            false,
            true,
            career,
            career.PendingJobOffer?.Id.Value,
            career.PendingJobOffer?.ClubId.Value);

    public static JobOfferReceiveResult Received(ManagerCareer career, JobOffer offer) =>
        new(true, false, career, offer.Id.Value, offer.ClubId.Value);
}

public sealed record JobOfferAcceptResult(
    bool WasAccepted,
    ManagerCareer Career,
    long OfferId,
    long ClubId)
{
    public static JobOfferAcceptResult Accepted(ManagerCareer career, JobOfferId offerId, ClubId clubId) =>
        new(true, career, offerId.Value, clubId.Value);
}

public sealed record ManagerReputationChangeResult(
    bool WasApplied,
    ManagerCareer Career,
    int ReputationDelta,
    int Reputation,
    string ReasonCode)
{
    public static ManagerReputationChangeResult Applied(
        ManagerCareer career,
        int delta,
        string reasonCode) =>
        new(true, career, delta, career.Reputation.Value, reasonCode);
}
