namespace FootballCareerSimulator.Application.TeamPreparation.Queries;

public sealed record PreMatchPromiseTensionLine(
    long PromiseId,
    long PlayerId,
    int SlotIndex,
    string KindName,
    string PlacementCode,
    string SummaryLine);

public sealed record PreMatchPromiseTensionReadModel(
    long FixtureId,
    long ClubId,
    bool SelectionApproved,
    bool HasTension,
    string ToneCode,
    string Headline,
    IReadOnlyList<PreMatchPromiseTensionLine> Lines);
