using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;

namespace FootballCareerSimulator.Domain.ContractRegistration;

/// <summary>
/// Sözleşmesi bitmiş oyuncunun serbest ajan kaydı (Transfer öncesi MVP).
/// </summary>
public sealed class PlayerFreeAgency
{
    private PlayerFreeAgency(PlayerId playerId, ClubId lastClubId, GameDate becameFreeAgentOn)
    {
        PlayerId = playerId;
        LastClubId = lastClubId;
        BecameFreeAgentOn = becameFreeAgentOn;
    }

    public PlayerId PlayerId { get; }

    public ClubId LastClubId { get; }

    public GameDate BecameFreeAgentOn { get; }

    public static PlayerFreeAgency Release(PlayerId playerId, ClubId lastClubId, GameDate day) =>
        new(playerId, lastClubId, day);

    public static PlayerFreeAgency Rehydrate(
        PlayerId playerId,
        ClubId lastClubId,
        GameDate becameFreeAgentOn) =>
        new(playerId, lastClubId, becameFreeAgentOn);
}
