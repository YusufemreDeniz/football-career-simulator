namespace FootballCareerSimulator.Domain.Interaction;

public enum DialogueSessionStatus
{
    AwaitingPlayerDecision = 1,
    Resolved = 2,
    Expired = 3,
    Invalidated = 4,
    Archived = 5,
}
