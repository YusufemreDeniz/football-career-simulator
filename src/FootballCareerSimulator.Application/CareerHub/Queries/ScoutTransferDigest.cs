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
    int ManagedClubStrength,
    int SourceClubStrength,
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
    int MarketValue,
    int SuggestedOpeningFee,
    int SuggestedWeeklyWage,
    int InterestPercent,
    string InterestLabel,
    string RecommendedSquadRole,
    bool IsAffordable,
    bool IsShortlisted,
    bool IsListedTarget)
{
    public string ToListLabel() =>
        $"{DisplayName} · {PositionCode} · GÜÇ {EstimatedAbilityLow}-{EstimatedAbilityHigh} · {Age} yaş\n"
        + $"{ClubName} · İlgi {InterestLabel} · Değer {MarketValue:N0}"
        + (IsAffordable ? string.Empty : " · BÜTÇE ÜSTÜ")
        + (IsListedTarget ? " · HEDEF" : IsShortlisted ? " · KISA LİSTE" : string.Empty);

    public string ToDetailText() =>
        $"{DisplayName} · {PositionCode} · {ClubName}\n"
        + $"Neden önerildi? {RecommendedSquadRole} ihtiyacına uyuyor · ilgi {InterestLabel}\n"
        + $"Scout bilgisi %{KnowledgePercent} · güç {EstimatedAbilityLow}-{EstimatedAbilityHigh} · potansiyel {PotentialAbility}\n"
        + $"Piyasa değeri {MarketValue:N0} · beklenen aralık {EstimatedFeeLow:N0}-{EstimatedFeeHigh:N0}\n"
        + $"Açılış teklifi {SuggestedOpeningFee:N0} · önerilen maaş {SuggestedWeeklyWage:N0}/hafta"
        + (IsAffordable ? string.Empty : "\nUyarı: bütçe üstü — teklifi düşür veya başka aday seç.");
}

public sealed record ScoutTransferDigest(
    bool HasClub,
    string NeedPositionCode,
    string Headline,
    string NeedLine,
    bool HasDepthGap,
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
        new(false, "—", "Scout merkezi: kulüp görevi yok.", "İhtiyaç hesaplanamadı.", false, Array.Empty<ScoutCandidateLine>());

    public static ScoutTransferDigest Compose(
        string managedClubName,
        int currentDayNumber,
        IReadOnlyList<MvpSquadPlayerProfile> managedProfiles,
        IReadOnlyDictionary<int, int> managedRatingsBySlot,
        IReadOnlyList<ScoutCandidateSource> candidates,
        int? transferBudgetAvailable = null)
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
            .Select(candidate => ToLine(
                candidate,
                currentDayNumber,
                needGroup.Average,
                transferBudgetAvailable))
            .ToArray();
        var positionCode = GroupCode(needGroup.Group);
        var hasDepthGap = needGroup.Count < IdealDepth[needGroup.Group] || needGroup.Average < 72;

        return new ScoutTransferDigest(
            true,
            positionCode,
            $"Scout merkezi · {managedClubName} · {lines.Length} öncelikli aday",
            $"Kadro ihtiyacı: {GroupName(needGroup.Group)} ({positionCode})"
            + $" · mevcut {needGroup.Count}/{IdealDepth[needGroup.Group]}"
            + $" · ortalama güç {needGroup.Average}",
            hasDepthGap,
            lines);
    }

    private static ScoutCandidateLine ToLine(
        ScoutCandidateSource candidate,
        int currentDayNumber,
        int positionalAverage,
        int? transferBudgetAvailable)
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
        var valuation = ScoutTransferValuationModel.Evaluate(
            candidate.Rating,
            candidate.PotentialAbility,
            candidate.Age,
            candidate.ManagedClubStrength,
            candidate.SourceClubStrength,
            positionalAverage,
            transferBudgetAvailable);
        var uncertaintyPercent = Math.Max(5, (110 - knowledge) / 2);
        var feeSpread = Math.Max(100_000, valuation.MarketValue * uncertaintyPercent / 100);

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
            Math.Max(100_000, valuation.MarketValue - feeSpread),
            valuation.MarketValue + feeSpread,
            valuation.MarketValue,
            valuation.SuggestedOpeningFee,
            valuation.SuggestedWeeklyWage,
            valuation.InterestPercent,
            valuation.InterestLabel,
            valuation.RecommendedSquadRole,
            valuation.IsAffordable,
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

public sealed record ScoutTransferValuation(
    int MarketValue,
    int SuggestedOpeningFee,
    int SuggestedWeeklyWage,
    int InterestPercent,
    string InterestLabel,
    string RecommendedSquadRole,
    bool IsAffordable);

public static class ScoutTransferValuationModel
{
    public static ScoutTransferValuation Evaluate(
        int currentAbility,
        int potentialAbility,
        int age,
        int managedClubStrength,
        int sourceClubStrength,
        int positionalAverage,
        int? transferBudgetAvailable = null)
    {
        var ability = Math.Clamp(currentAbility, 40, 99);
        var potential = Math.Clamp(potentialAbility, ability, 99);
        var ageMultiplier = age switch
        {
            <= 21 => 1.18,
            <= 25 => 1.08,
            <= 29 => 1.00,
            <= 32 => 0.80,
            _ => 0.60,
        };
        var potentialMultiplier = 1.0 + Math.Min(0.50, (potential - ability) * 0.05);
        var rawValue = 750_000d
            * Math.Pow(1.16, ability - 60)
            * ageMultiplier
            * potentialMultiplier;
        var marketValue = RoundTo((int)Math.Clamp(rawValue, 250_000, 90_000_000), 50_000);
        var suggestedOpeningFee = RoundTo((int)(marketValue * 0.90), 50_000);

        var role = ability switch
        {
            _ when ability >= positionalAverage + 6 => "Kilit oyuncu",
            _ when ability >= positionalAverage + 2 => "İlk 11",
            _ when ability >= positionalAverage - 3 => "Rotasyon",
            _ => "Gelecek yatırımı",
        };
        var roleWageMultiplier = role switch
        {
            "Kilit oyuncu" => 1.25,
            "İlk 11" => 1.10,
            "Gelecek yatırımı" => 0.80,
            _ => 1.00,
        };
        var suggestedWage = RoundTo(
            Math.Max(5_000, (int)(Math.Pow(Math.Max(0, ability - 50), 2) * 35 * roleWageMultiplier)),
            500);

        var interest = Math.Clamp(
            55
            + ((managedClubStrength - sourceClubStrength) * 2)
            + (role is "Kilit oyuncu" or "İlk 11" ? 10 : 3)
            - Math.Max(0, ability - managedClubStrength),
            10,
            95);
        var interestLabel = interest switch
        {
            >= 80 => "Çok yüksek",
            >= 65 => "Yüksek",
            >= 45 => "Orta",
            >= 25 => "Düşük",
            _ => "Çok düşük",
        };

        return new ScoutTransferValuation(
            marketValue,
            suggestedOpeningFee,
            suggestedWage,
            interest,
            interestLabel,
            role,
            transferBudgetAvailable is null || suggestedOpeningFee <= transferBudgetAvailable.Value);
    }

    private static int RoundTo(int value, int step) =>
        Math.Max(step, (int)Math.Round(value / (double)step, MidpointRounding.AwayFromZero) * step);
}
