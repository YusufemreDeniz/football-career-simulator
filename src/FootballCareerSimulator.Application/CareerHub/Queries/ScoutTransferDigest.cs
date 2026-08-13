using FootballCareerSimulator.Simulation.TeamPreparation;

namespace FootballCareerSimulator.Application.CareerHub.Queries;

public sealed record ScoutCandidateSource(
    long PlayerId,
    long ClubId,
    string ClubName,
    string DisplayName,
    MvpSquadPositionGroup PositionGroup,
    string PositionCode,
    int Rating,
    int Age,
    int PotentialAbility,
    int? ShortlistedOnDayNumber,
    bool IsListedTarget);

public sealed record ScoutCandidateLine(
    long PlayerId,
    string DisplayName,
    string ClubName,
    string PositionCode,
    int Age,
    int KnowledgePercent,
    int EstimatedAbilityLow,
    int EstimatedAbilityHigh,
    int PotentialAbility,
    int EstimatedFeeLow,
    int EstimatedFeeHigh,
    bool IsShortlisted,
    bool IsListedTarget)
{
    public string ToListLabel() =>
        $"{DisplayName} · {PositionCode} · {ClubName} · {Age} yaş\n"
        + $"Bilgi %{KnowledgePercent} · güç {EstimatedAbilityLow}-{EstimatedAbilityHigh}"
        + $" · tahmini bedel {EstimatedFeeLow:N0}-{EstimatedFeeHigh:N0}"
        + (IsListedTarget ? " · HEDEF" : IsShortlisted ? " · KISA LİSTE" : string.Empty);
}

public sealed record ScoutTransferDigest(
    bool HasClub,
    string NeedPositionCode,
    string Headline,
    string NeedLine,
    IReadOnlyList<ScoutCandidateLine> Candidates)
{
    private static readonly IReadOnlyDictionary<MvpSquadPositionGroup, int> IdealDepth =
        new Dictionary<MvpSquadPositionGroup, int>
        {
            [MvpSquadPositionGroup.Goalkeeper] = 3,
            [MvpSquadPositionGroup.Defender] = 8,
            [MvpSquadPositionGroup.Midfielder] = 8,
            [MvpSquadPositionGroup.Forward] = 6,
        };

    public static ScoutTransferDigest Clear() =>
        new(false, "—", "Scout merkezi: kulüp görevi yok.", "İhtiyaç hesaplanamadı.", Array.Empty<ScoutCandidateLine>());

    public static ScoutTransferDigest Compose(
        string managedClubName,
        int currentDayNumber,
        IReadOnlyList<MvpSquadPlayerProfile> managedProfiles,
        IReadOnlyDictionary<int, int> managedRatingsBySlot,
        IReadOnlyList<ScoutCandidateSource> candidates)
    {
        ArgumentNullException.ThrowIfNull(managedProfiles);
        ArgumentNullException.ThrowIfNull(managedRatingsBySlot);
        ArgumentNullException.ThrowIfNull(candidates);

        var needGroup = Enum.GetValues<MvpSquadPositionGroup>()
            .Select(group =>
            {
                var slots = managedProfiles
                    .Select((profile, slot) => (profile, slot))
                    .Where(item => item.profile.PositionGroup == group)
                    .ToArray();
                var average = slots.Length == 0
                    ? 0
                    : (int)Math.Round(slots.Average(item =>
                        managedRatingsBySlot.GetValueOrDefault(item.slot, 60)));
                var depthGap = Math.Max(0, IdealDepth[group] - slots.Length);
                return new { Group = group, Count = slots.Length, Average = average, Score = depthGap * 20 + (80 - average) };
            })
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Average)
            .First();

        var lines = candidates
            .Where(candidate => candidate.PositionGroup == needGroup.Group)
            .OrderByDescending(candidate => candidate.IsListedTarget)
            .ThenByDescending(candidate => candidate.PotentialAbility)
            .ThenByDescending(candidate => candidate.Rating)
            .ThenBy(candidate => candidate.Age)
            .Take(8)
            .Select(candidate => ToLine(candidate, currentDayNumber))
            .ToArray();
        var positionCode = GroupCode(needGroup.Group);

        return new ScoutTransferDigest(
            true,
            positionCode,
            $"Scout merkezi · {managedClubName} · {lines.Length} öncelikli aday",
            $"Kadro ihtiyacı: {GroupName(needGroup.Group)} ({positionCode})"
            + $" · mevcut {needGroup.Count}/{IdealDepth[needGroup.Group]}"
            + $" · ortalama güç {needGroup.Average}",
            lines);
    }

    private static ScoutCandidateLine ToLine(ScoutCandidateSource candidate, int currentDayNumber)
    {
        var watchedDays = candidate.ShortlistedOnDayNumber is int added
            ? Math.Max(0, currentDayNumber - added)
            : 0;
        var knowledge = candidate.IsListedTarget
            ? Math.Min(100, 70 + watchedDays * 5)
            : candidate.ShortlistedOnDayNumber is not null
                ? Math.Min(95, 55 + watchedDays * 5)
                : 40;
        var uncertainty = Math.Max(1, (105 - knowledge) / 8);
        var feeBase = Math.Max(250_000, candidate.Rating * candidate.Rating * 2_000);
        var feeSpread = Math.Max(100_000, feeBase * (100 - knowledge) / 100);

        return new ScoutCandidateLine(
            candidate.PlayerId,
            candidate.DisplayName,
            candidate.ClubName,
            candidate.PositionCode,
            candidate.Age,
            knowledge,
            Math.Max(40, candidate.Rating - uncertainty),
            Math.Min(99, candidate.Rating + uncertainty),
            candidate.PotentialAbility,
            Math.Max(100_000, feeBase - feeSpread),
            feeBase + feeSpread,
            candidate.ShortlistedOnDayNumber is not null,
            candidate.IsListedTarget);
    }

    private static string GroupCode(MvpSquadPositionGroup group) => group switch
    {
        MvpSquadPositionGroup.Goalkeeper => "KL",
        MvpSquadPositionGroup.Defender => "DEF",
        MvpSquadPositionGroup.Midfielder => "ORT",
        _ => "HÜC",
    };

    private static string GroupName(MvpSquadPositionGroup group) => group switch
    {
        MvpSquadPositionGroup.Goalkeeper => "Kaleci",
        MvpSquadPositionGroup.Defender => "Savunma",
        MvpSquadPositionGroup.Midfielder => "Orta saha",
        _ => "Hücum",
    };
}
