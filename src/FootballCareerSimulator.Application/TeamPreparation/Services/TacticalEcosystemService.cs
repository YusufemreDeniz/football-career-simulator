using FootballCareerSimulator.Application.TeamPreparation.Queries;
using FootballCareerSimulator.Domain.TeamPreparation;

namespace FootballCareerSimulator.Application.TeamPreparation.Services;

/// <summary>
/// Lig genelindeki maç taktiksel tercihlerini toplayarak taktiksel meta analizini yürüten servis (Yenilik 4).
/// </summary>
public sealed class TacticalEcosystemService
{
    private readonly Dictionary<(string Comp, int Year), List<(Formation Formation, TacticalApproach Approach)>> _observations = new();
    private readonly Dictionary<(string Comp, int Year), LeagueTacticalMetaSnapshot> _snapshots = new();

    public void RecordMatchTactics(
        string competitionCode,
        int seasonYear,
        Formation homeFormation,
        TacticalApproach homeApproach,
        Formation awayFormation,
        TacticalApproach awayApproach)
    {
        var key = (competitionCode, seasonYear);
        if (!_observations.TryGetValue(key, out var list))
        {
            list = new List<(Formation, TacticalApproach)>();
            _observations[key] = list;
        }

        list.Add((homeFormation, homeApproach));
        list.Add((awayFormation, awayApproach));
    }

    public LeagueTacticalMetaSnapshot CompileSnapshot(string competitionCode, int seasonYear)
    {
        var key = (competitionCode, seasonYear);
        if (_snapshots.TryGetValue(key, out var existing))
        {
            return existing;
        }

        if (!_observations.TryGetValue(key, out var list) || list.Count == 0)
        {
            var fallback = new LeagueTacticalMetaSnapshot(
                competitionCode,
                seasonYear,
                formation433Share: 34,
                formation442Share: 33,
                formation352Share: 33,
                highPressPopularity: 50,
                TacticalApproach.Balanced);
            _snapshots[key] = fallback;
            return fallback;
        }

        var total = list.Count;
        var count433 = list.Count(o => o.Formation == Formation.F433);
        var count442 = list.Count(o => o.Formation == Formation.F442);
        var count352 = list.Count(o => o.Formation == Formation.F352);

        var countAttacking = list.Count(o => o.Approach == TacticalApproach.Attacking);
        var countDefensive = list.Count(o => o.Approach == TacticalApproach.Defensive);
        var countBalanced = list.Count(o => o.Approach == TacticalApproach.Balanced);

        var dominantApproach = countAttacking >= countDefensive && countAttacking >= countBalanced
            ? TacticalApproach.Attacking
            : (countDefensive >= countBalanced ? TacticalApproach.Defensive : TacticalApproach.Balanced);

        var snapshot = new LeagueTacticalMetaSnapshot(
            competitionCode,
            seasonYear,
            formation433Share: (count433 * 100) / total,
            formation442Share: (count442 * 100) / total,
            formation352Share: (count352 * 100) / total,
            highPressPopularity: (countAttacking * 100) / total,
            dominantApproach);

        _snapshots[key] = snapshot;
        return snapshot;
    }

    public TacticalIntelligenceDigest BuildDigest(string competitionCode, int seasonYear)
    {
        var snapshot = CompileSnapshot(competitionCode, seasonYear);
        var dominant = snapshot.DominantFormation;
        var counterForm = LeagueTacticalMetaSnapshot.GetCounterFormation(dominant);
        var counterApp = LeagueTacticalMetaSnapshot.GetCounterApproach(snapshot.DominantApproach);

        var metaName = dominant switch
        {
            Formation.F433 => "Akıcı Hücum & 4-3-3 Dalgası",
            Formation.F352 => "Yoğun Orta Saha & 3-5-2 Bloğu",
            _ => "Dengeli Kanat Oyunu & 4-4-2 Düzeni",
        };

        var bulletins = new List<string>
        {
            $"Lig genelinde en yaygın formasyon: {dominant} (Pay: %{Math.Max(snapshot.Formation433Share, Math.Max(snapshot.Formation442Share, snapshot.Formation352Share))}).",
            $"Taktiksel antitez tavsiyesi: {counterForm} formasyonu ile karşı koyulması öneriliyor.",
            $"Rakip yaklaşım eğilimi ({snapshot.DominantApproach}) için {counterApp} yaklaşım tavsiye ediliyor.",
        };

        return new TacticalIntelligenceDigest(
            competitionCode,
            seasonYear,
            metaName,
            dominant,
            counterForm,
            counterApp,
            bulletins);
    }
}
