using FootballCareerSimulator.Application.SocialContinuity.Queries;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Application.SocialContinuity.Services;

/// <summary>
/// Soyunma odasında seyrek bağlar üzerinden sınırlı ve açıklamalı sosyal etki yayılımını yöneten servis (Yenilik 6: Social Contagion).
/// $O(N^2)$ kontrolsüz yayılımı engeller (depth=1 sınırı ve 14 gün cooldown).
/// </summary>
public sealed class SocialContagionService
{
    private readonly List<SparseInfluenceLink> _links = new();
    private readonly Dictionary<ClubId, DressingRoomClimate> _climates = new();

    public void AddInfluenceLink(SparseInfluenceLink link)
    {
        ArgumentNullException.ThrowIfNull(link);

        var existingCount = _links.Count(l => l.SourcePlayerId == link.SourcePlayerId);
        if (existingCount >= SparseInfluenceLink.MaxTiesPerPlayer)
        {
            throw new SocialContinuityInvariantViolationException(
                $"Player {link.SourcePlayerId} has reached the maximum allowed influence ties ({SparseInfluenceLink.MaxTiesPerPlayer}).");
        }

        _links.Add(link);
    }

    public DressingRoomClimate GetOrCreateClimate(ClubId clubId)
    {
        if (!_climates.TryGetValue(clubId, out var climate))
        {
            climate = new DressingRoomClimate(clubId);
            _climates[clubId] = climate;
        }

        return climate;
    }

    public IReadOnlyList<(long TargetPlayerId, int SecondaryTrustDelta)> PropagateContagion(
        long primaryPlayerId,
        int trustLoss,
        GameDate day,
        ClubId clubId)
    {
        var directLinks = _links
            .Where(l => l.SourcePlayerId == primaryPlayerId && l.CanPropagate(day))
            .ToList();

        var results = new List<(long, int)>();

        foreach (var link in directLinks)
        {
            // İkincil etki katsayısı: birincil kaybın en fazla %35'i kadar
            var secondaryDelta = (int)Math.Round(trustLoss * (link.Strength / 100.0) * 0.35, MidpointRounding.AwayFromZero);
            link.MarkPropagated(day);
            results.Add((link.TargetPlayerId, secondaryDelta));
        }

        if (results.Count > 0)
        {
            var climate = GetOrCreateClimate(clubId);
            climate.ApplyCrisis(results.Count * 7);
        }

        return results;
    }

    public DressingRoomClimateDigest BuildDigest(ClubId clubId)
    {
        var climate = GetOrCreateClimate(clubId);
        var stateTurkish = climate.State switch
        {
            "United" => "Kenetlenmiş",
            "Calm" => "Sakin",
            "Split" => "Bölünmüş",
            _ => "İsyan Eşiğinde",
        };

        var observations = new List<string>();
        if (climate.Friction >= 45)
        {
            observations.Add("Soyunma odasında klikleşme ve gerginlik gözleniyor.");
        }

        if (climate.Harmony >= 75)
        {
            observations.Add("Takım arkadaşlığı ve menajere güven üst düzeyde.");
        }

        return new DressingRoomClimateDigest(
            clubId,
            stateTurkish,
            climate.Harmony,
            climate.Friction,
            observations);
    }
}
