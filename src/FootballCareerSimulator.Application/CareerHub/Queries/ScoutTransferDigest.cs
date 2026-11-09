using FootballCareerSimulator.Simulation.TeamPreparation;

namespace FootballCareerSimulator.Application.CareerHub.Queries;

public enum TransferMarketScope
{
    All = 0,
    Domestic = 1,
    EuropeanElite = 2,
    FreeAgents = 3,
}

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
    bool IsListedTarget,
    string OriginLeague = "Trendyol Süper Lig",
    bool IsInternational = false,
    // Gerçek sözleşmeden kalan yıl; bilinmiyorsa değerleme modeli 2 yıl varsayar.
    double? ContractYearsRemaining = null);

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
    bool IsListedTarget,
    string OriginLeague = "Trendyol Süper Lig",
    bool IsInternational = false)
{
    public string ToListLabel() =>
        $"[{PositionCode,-3}] {DisplayName} · GÜÇ {EstimatedAbilityLow}-{EstimatedAbilityHigh} · {Age} yaş\n"
        + $"       {ClubName} {(IsInternational ? $"[{OriginLeague}] " : string.Empty)}· İlgi {InterestLabel} · Değer {(MarketValue == 0 ? "SERBEST" : $"{MarketValue:N0} TRY")}"
        + (IsAffordable ? " · ✓ Bütçe İçi" : " · ⚠️ BÜTÇE ÜSTÜ")
        + (IsListedTarget ? " · ⭐ HEDEF" : IsShortlisted ? " · 📋 KISA LİSTE" : string.Empty);

    public string ToDetailText() =>
        $"{DisplayName} · {PositionCode} · {ClubName}" + (IsInternational ? $" ({OriginLeague})" : string.Empty) + "\n"
        + $"Neden önerildi? {RecommendedSquadRole} ihtiyacına uyuyor · ilgi {InterestLabel}\n"
        + $"Scout bilgisi %{KnowledgePercent} · güç {EstimatedAbilityLow}-{EstimatedAbilityHigh} · potansiyel {PotentialAbility}\n"
        + (MarketValue == 0
            ? "Transfer durumu: SERBEST OYUNCU (Bonservis bedeli yok)\n"
            : $"Piyasa değeri {MarketValue:N0} · beklenen aralık {EstimatedFeeLow:N0}-{EstimatedFeeHigh:N0}\n"
              + $"Açılış teklifi {SuggestedOpeningFee:N0} · ")
        + $"önerilen maaş {SuggestedWeeklyWage:N0}/hafta"
        + (IsAffordable ? string.Empty : "\nUyarı: bütçe üstü — teklifi düşür veya başka aday seç.");
}

public sealed record ScoutTransferDigest(
    bool HasClub,
    string NeedPositionCode,
    string Headline,
    string NeedLine,
    bool HasDepthGap,
    IReadOnlyList<ScoutCandidateLine> Candidates,
    TransferMarketScope Scope = TransferMarketScope.All)
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
        int? transferBudgetAvailable = null,
        TransferMarketScope scope = TransferMarketScope.All)
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

        var scopedCandidates = (scope switch
        {
            TransferMarketScope.Domestic => candidates.Where(c => !c.IsInternational && c.ClubId > 0),
            TransferMarketScope.EuropeanElite => candidates.Where(c => c.IsInternational && c.ClubId >= 100),
            TransferMarketScope.FreeAgents => candidates.Where(c => c.ClubId == 0 || c.OriginLeague.Contains("Serbest")),
            _ => candidates,
        }).ToList();

        var matchingNeed = scopedCandidates.Where(c => c.PositionGroup == needGroup.Group).ToList();
        var pool = (matchingNeed.Count > 0 && scope != TransferMarketScope.FreeAgents)
            ? matchingNeed
            : (matchingNeed.Count > 0 ? matchingNeed.Concat(scopedCandidates.Where(c => c.PositionGroup != needGroup.Group)).ToList() : scopedCandidates);

        var lines = pool
            .OrderByDescending(candidate => candidate.IsListedTarget)
            .ThenByDescending(candidate => candidate.PositionGroup == needGroup.Group)
            .ThenByDescending(candidate => candidate.PotentialAbility)
            .ThenByDescending(candidate => candidate.Rating)
            .ThenBy(candidate => candidate.Age)
            .Take(12)
            .Select(candidate => ToLine(
                candidate,
                currentDayNumber,
                needGroup.Average,
                transferBudgetAvailable))
            .ToArray();
        var positionCode = GroupCode(needGroup.Group);
        var hasDepthGap = needGroup.Count < IdealDepth[needGroup.Group] || needGroup.Average < 72;

        var scopeTitle = scope switch
        {
            TransferMarketScope.Domestic => "Süper Lig",
            TransferMarketScope.EuropeanElite => "Avrupa Eliti",
            TransferMarketScope.FreeAgents => "Serbest Oyuncular",
            _ => "Tüm Dünya",
        };

        return new ScoutTransferDigest(
            true,
            positionCode,
            $"Scout merkezi · {managedClubName} · {scopeTitle} ({lines.Length} aday)",
            $"Kadro ihtiyacı: {GroupName(needGroup.Group)} ({positionCode})"
            + $" · mevcut {needGroup.Count}/{IdealDepth[needGroup.Group]}"
            + $" · ortalama güç {needGroup.Average}",
            hasDepthGap,
            lines,
            scope);
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
        var isFreeAgent = candidate.ClubId == 0 || candidate.OriginLeague.Contains("Serbest");
        var valuation = ScoutTransferValuationModel.Evaluate(
            candidate.Rating,
            candidate.PotentialAbility,
            candidate.Age,
            candidate.ManagedClubStrength,
            candidate.SourceClubStrength,
            positionalAverage,
            transferBudgetAvailable,
            isFreeAgent,
            contractYearsRemaining: candidate.ContractYearsRemaining ?? ScoutTransferValuationModel.DefaultContractYearsRemaining);
        var uncertaintyPercent = Math.Max(5, (110 - knowledge) / 2);
        var feeSpread = isFreeAgent ? 0 : Math.Max(100_000, valuation.MarketValue * uncertaintyPercent / 100);

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
            Math.Max(isFreeAgent ? 0 : 100_000, valuation.MarketValue - feeSpread),
            valuation.MarketValue + feeSpread,
            valuation.MarketValue,
            valuation.SuggestedOpeningFee,
            valuation.SuggestedWeeklyWage,
            valuation.InterestPercent,
            valuation.InterestLabel,
            valuation.RecommendedSquadRole,
            valuation.IsAffordable,
            candidate.ShortlistedOnDayNumber is not null,
            candidate.IsListedTarget,
            candidate.OriginLeague,
            candidate.IsInternational);
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
    /// <summary>Sözleşme bilgisi olmayan adaylar için varsayılan kalan süre.</summary>
    public const double DefaultContractYearsRemaining = 2.0;

    public static ScoutTransferValuation Evaluate(
        int currentAbility,
        int potentialAbility,
        int age,
        int managedClubStrength,
        int sourceClubStrength,
        int positionalAverage,
        int? transferBudgetAvailable = null,
        bool isFreeAgent = false,
        PlayerValuationBalanceProfile? balanceProfile = null,
        int? managedLeagueStrength = null,
        PlayerWageBalanceProfile? wageBalanceProfile = null,
        double contractYearsRemaining = DefaultContractYearsRemaining)
    {
        var ability = Math.Clamp(currentAbility, 40, 99);
        var potential = Math.Clamp(potentialAbility, ability, 99);
        balanceProfile ??= PlayerValuationBalanceProfile.Default;
        var valuation = PlayerMarketValuationModel.Evaluate(
            new PlayerMarketValuationInput(
                ability,
                potential,
                age,
                ContractYearsRemaining: Math.Max(0.0, contractYearsRemaining),
                SourceClubStrength: sourceClubStrength,
                IsFreeAgent: isFreeAgent),
            balanceProfile);
        var marketValue = valuation.MarketValue;
        var suggestedOpeningFee = isFreeAgent
            ? 0
            : RoundTo((int)(marketValue * balanceProfile.BuyerOpeningOfferFactor), balanceProfile.RoundingStep);
        wageBalanceProfile ??= PlayerWageBalanceProfile.Default;

        var role = ability switch
        {
            _ when ability >= positionalAverage + 6 => "Kilit oyuncu",
            _ when ability >= positionalAverage + 2 => "İlk 11",
            _ when ability >= positionalAverage - 3 => "Rotasyon",
            _ => "Gelecek yatırımı",
        };
        var roleWageMultiplier = role switch
        {
            "Kilit oyuncu" => wageBalanceProfile.KeyPlayerRoleFactor,
            "İlk 11" => wageBalanceProfile.StartingRoleFactor,
            "Gelecek yatırımı" => wageBalanceProfile.ProspectRoleFactor,
            _ => wageBalanceProfile.RotationRoleFactor,
        };
        var wage = PlayerWageModel.Evaluate(
            new PlayerWageCalculationInput(
                ability,
                potential,
                age,
                managedClubStrength,
                managedLeagueStrength ?? managedClubStrength,
                ContractLengthYears: wageBalanceProfile.SuggestedContractYears,
                IsPeaked: age >= wageBalanceProfile.SuggestedPeakedAge || potential <= ability,
                SquadRoleFactor: roleWageMultiplier,
                MarketStatusFactor: isFreeAgent ? wageBalanceProfile.FreeAgentMarketStatusFactor : 1.0),
            wageBalanceProfile);
        // Öneri, sözleşme kabul eşiğinin ortak tabanının altına inmez (aksi halde önerilen ücret reddedilirdi).
        var acceptanceBasis = PlayerWageModel.EvaluateContractBasis(
            ability,
            potential,
            age,
            managedClubStrength,
            wageBalanceProfile.SuggestedContractYears,
            isFreeAgent,
            wageBalanceProfile);
        var suggestedWage = Math.Max(wage.WeeklyWage, acceptanceBasis.WeeklyWage);

        var interest = isFreeAgent
            ? Math.Clamp(70 + (role is "Kilit oyuncu" or "İlk 11" ? 15 : 5) - Math.Max(0, ability - managedClubStrength), 30, 95)
            : Math.Clamp(
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
            isFreeAgent || transferBudgetAvailable is null || suggestedOpeningFee <= transferBudgetAvailable.Value);
    }

    private static int RoundTo(int value, int step) =>
        Math.Max(step, (int)Math.Round(value / (double)step, MidpointRounding.AwayFromZero) * step);
}
