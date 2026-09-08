using FootballCareerSimulator.Application.ClubGovernance.Queries;
using FootballCareerSimulator.Domain.ClubGovernance;
using FootballCareerSimulator.Domain.Shared;

namespace FootballCareerSimulator.Application.ClubGovernance.Services;

/// <summary>
/// Kulüp DNA profillerini yöneten ve okuma modellerini türeten servis (Yenilik 5: Club DNA).
/// </summary>
public sealed class ClubDnaService
{
    private readonly Dictionary<ClubId, ClubDnaProfile> _profiles = new();

    public ClubDnaProfile GetOrCreateProfile(ClubId clubId)
    {
        if (!_profiles.TryGetValue(clubId, out var profile))
        {
            profile = ClubDnaProfile.CreateDefault(clubId);
            _profiles[clubId] = profile;
        }

        return profile;
    }

    public void RegisterProfile(ClubDnaProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        _profiles[profile.ClubId] = profile;
    }

    public ClubCultureDigest BuildDigest(ClubId clubId)
    {
        var profile = GetOrCreateProfile(clubId);
        var badges = new List<string>();

        if (profile.YouthDevelopmentCulture >= 65)
        {
            badges.Add("Akademi Geleneği");
        }
        else if (profile.YouthDevelopmentCulture <= 35)
        {
            badges.Add("Hazır Oyuncu Odaklı");
        }

        if (profile.RecruitmentAggression >= 65)
        {
            badges.Add("Piyasa Avcısı");
        }

        if (profile.SellingClubTendency >= 65)
        {
            badges.Add("Yetiştirici / Satıcı Kulüp");
        }

        if (profile.ManagerialPatience >= 65)
        {
            badges.Add("İstikrarlı Yönetim");
        }
        else if (profile.ManagerialPatience <= 35)
        {
            badges.Add("Sabırsız Yönetim");
        }

        if (profile.WageDiscipline >= 65)
        {
            badges.Add("Kemer Sıkma Politikası");
        }

        if (profile.HighIntensityTradition >= 65)
        {
            badges.Add("Yüksek Tempo Geleneği");
        }

        var summary = badges.Count > 0
            ? string.Join(", ", badges)
            : "Dengeli Kurumsal Yapı";

        return new ClubCultureDigest(
            profile.ClubId,
            badges,
            summary,
            profile.YouthDevelopmentCulture,
            profile.ManagerialPatience,
            profile.WageDiscipline);
    }
}
