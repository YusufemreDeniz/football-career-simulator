using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Simulation.TeamPreparation;

namespace FootballCareerSimulator.Application.TeamPreparation.Queries;

public sealed record SmartRotationCandidate(
    int SlotIndex,
    string DisplayName,
    string PositionCode,
    MvpSquadPositionGroup PositionGroup,
    MvpSquadPositionRole? PositionRole,
    int Rating,
    int Fitness,
    int Fatigue,
    bool IsAvailable,
    bool HasActivePromise);

public sealed record SmartRotationPlanDigest(
    bool IsValid,
    Formation Formation,
    IReadOnlyList<int> StartingSlotIndices,
    IReadOnlyList<int> BenchSlotIndices,
    int RestedFatiguedCount,
    int PromisedStarterCount,
    int AverageXiFitness,
    int AverageXiRating,
    string SummaryLine,
    IReadOnlyList<string> SwapExplanationLines)
{
    public static SmartRotationPlanDigest Invalid(Formation formation, string reason) =>
        new(
            IsValid: false,
            Formation: formation,
            StartingSlotIndices: Array.Empty<int>(),
            BenchSlotIndices: Array.Empty<int>(),
            RestedFatiguedCount: 0,
            PromisedStarterCount: 0,
            AverageXiFitness: 0,
            AverageXiRating: 0,
            SummaryLine: reason,
            SwapExplanationLines: Array.Empty<string>());

    public static SmartRotationPlanDigest Compose(
        Formation formation,
        IReadOnlyList<SmartRotationCandidate> candidates,
        IReadOnlyList<int>? currentStartingSlots = null)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        var available = candidates
            .Where(c => c.IsAvailable)
            .OrderBy(c => c.SlotIndex)
            .ToArray();

        if (available.Length < MatchSelection.StartingXiSize)
        {
            return Invalid(
                formation,
                $"Akıllı rotasyon için en az {MatchSelection.StartingXiSize} müsait oyuncu gerekiyor ({available.Length} bulundu).");
        }

        var requiredRoles = MvpLineupRoleFitCalculator.RequiredRolesFor(formation);
        var chosenSlots = new int[MatchSelection.StartingXiSize];
        var usedSlots = new HashSet<int>();

        // 1. Önce kaleci rolünü (pitch index 0) en uygun kaleciye ata
        for (var roleIdx = 0; roleIdx < requiredRoles.Count; roleIdx++)
        {
            var requiredRole = requiredRoles[roleIdx];
            var bestCandidate = available
                .Where(c => !usedSlots.Contains(c.SlotIndex))
                .OrderByDescending(c => ScoreCandidateForRole(c, requiredRole))
                .ThenBy(c => c.SlotIndex)
                .First();

            chosenSlots[roleIdx] = bestCandidate.SlotIndex;
            usedSlots.Add(bestCandidate.SlotIndex);
        }

        // 2. Seçilen 11 oyuncuyu MvpLineupRoleFitCalculator ile optimal saha slotlarına hizala
        var chosenProfiles = chosenSlots
            .Select(slot =>
            {
                var c = available.First(item => item.SlotIndex == slot);
                return c.PositionRole is { } r
                    ? new MvpSquadPlayerProfile(c.DisplayName, r)
                    : new MvpSquadPlayerProfile(c.DisplayName, c.PositionGroup);
            })
            .ToList();
        var optimalOrder = MvpLineupRoleFitCalculator.ComputeOptimalPitchSlots(formation, chosenProfiles);
        var alignedStarting = new int[MatchSelection.StartingXiSize];
        for (var i = 0; i < MatchSelection.StartingXiSize; i++)
        {
            alignedStarting[i] = chosenSlots[optimalOrder[i]];
        }

        // 3. Yedek kulübesi (en fazla 7 oyuncu: 1 GK + en iyi kalanlar)
        var remaining = available
            .Where(c => !usedSlots.Contains(c.SlotIndex))
            .ToList();
        var bench = new List<int>(MatchSelection.MaxBenchSize);
        var backupGk = remaining
            .Where(c => c.PositionGroup == MvpSquadPositionGroup.Goalkeeper)
            .OrderByDescending(ScoreCandidateGeneral)
            .ThenBy(c => c.SlotIndex)
            .FirstOrDefault();
        if (backupGk is not null)
        {
            bench.Add(backupGk.SlotIndex);
            remaining.Remove(backupGk);
        }

        foreach (var candidate in remaining
                     .OrderByDescending(ScoreCandidateGeneral)
                     .ThenBy(c => c.SlotIndex)
                     .Take(MatchSelection.MaxBenchSize - bench.Count))
        {
            bench.Add(candidate.SlotIndex);
        }

        var bySlot = candidates.ToDictionary(c => c.SlotIndex);
        var xiCandidates = alignedStarting.Select(s => bySlot[s]).ToArray();
        var avgFitness = (int)Math.Round(xiCandidates.Average(c => c.Fitness), MidpointRounding.AwayFromZero);
        var avgRating = (int)Math.Round(xiCandidates.Average(c => c.Rating), MidpointRounding.AwayFromZero);
        var promisedInXi = xiCandidates.Count(c => c.HasActivePromise);

        var explanations = new List<string>();
        var restedFatigued = 0;
        if (currentStartingSlots is { Count: > 0 })
        {
            var newSet = alignedStarting.ToHashSet();
            foreach (var oldSlot in currentStartingSlots)
            {
                if (!newSet.Contains(oldSlot) && bySlot.TryGetValue(oldSlot, out var dropped))
                {
                    if (!dropped.IsAvailable)
                    {
                        explanations.Add($"{dropped.DisplayName} (müsait değil) kadrodan çıkarıldı.");
                    }
                    else if (dropped.Fatigue >= 55 || dropped.Fitness <= 68)
                    {
                        restedFatigued++;
                        explanations.Add($"{dropped.DisplayName} (Fit %{dropped.Fitness}, Yorgunluk %{dropped.Fatigue}) dinlendirildi.");
                    }
                }
            }
        }
        else
        {
            restedFatigued = available.Count(c => !usedSlots.Contains(c.SlotIndex) && (c.Fatigue >= 55 || c.Fitness <= 68));
        }

        foreach (var starter in xiCandidates.Where(c => c.HasActivePromise))
        {
            explanations.Add($"{starter.DisplayName} aktif forma sözü nedeniyle ilk 11'de tutuldu.");
        }

        var summary = $"Akıllı Rotasyon ({formation}): Ort. Güç {avgRating} · XI Zindelik %{avgFitness}"
                      + (restedFatigued > 0 ? $" · {restedFatigued} yorgun as dinlendirildi" : string.Empty)
                      + (promisedInXi > 0 ? $" · {promisedInXi} söz verilen oyuncu 11'de" : string.Empty);

        return new SmartRotationPlanDigest(
            IsValid: true,
            Formation: formation,
            StartingSlotIndices: alignedStarting,
            BenchSlotIndices: bench,
            RestedFatiguedCount: restedFatigued,
            PromisedStarterCount: promisedInXi,
            AverageXiFitness: avgFitness,
            AverageXiRating: avgRating,
            SummaryLine: summary,
            SwapExplanationLines: explanations);
    }

    private static int ScoreCandidateForRole(SmartRotationCandidate c, MvpSquadPositionRole targetRole)
    {
        // Kaleci kuralı: kaleci slotuna kaleci olmayan, saha slotuna kaleci girmesin (zorunlu olmadıkça)
        var isTargetGk = targetRole == MvpSquadPositionRole.Goalkeeper;
        var isCandidateGk = c.PositionGroup == MvpSquadPositionGroup.Goalkeeper
                            || c.PositionRole == MvpSquadPositionRole.Goalkeeper;
        var roleScore = 0;
        if (isTargetGk != isCandidateGk)
        {
            roleScore = -500;
        }
        else if (c.PositionRole == targetRole)
        {
            roleScore = 140;
        }
        else if (c.PositionGroup == targetRole.ToPositionGroup())
        {
            roleScore = 95;
        }
        else
        {
            roleScore = 30;
        }

        return roleScore + ScoreCandidateGeneral(c);
    }

    private static int ScoreCandidateGeneral(SmartRotationCandidate c)
    {
        var fitnessScore = (c.Fitness - 70) * 2;
        var fatiguePenalty = c.Fatigue >= 65
            ? (c.Fatigue - 40) * 4
            : c.Fatigue >= 50
                ? (c.Fatigue - 35) * 2
                : c.Fatigue / 2;
        var promiseBonus = c.HasActivePromise && c.Fatigue < 75 ? 45 : 0;
        return (c.Rating * 3) + fitnessScore - fatiguePenalty + promiseBonus;
    }
}
