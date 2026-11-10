using FootballCareerSimulator.Application.TrainingPhysicalState.Ports;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.TrainingPhysicalState;

namespace FootballCareerSimulator.Application.TrainingPhysicalState.Services;

/// <summary>
/// Kadro membership ile physical location (ClubId+Slot) tutarlılığını korur.
/// State PlayerId'de kalır; ayrılan oyuncunun slot işgali temizlenir.
/// </summary>
public static class PhysicalLocationSync
{
    public static void SyncClubMembership(
        ITrainingPhysicalStateStore store,
        ClubId clubId,
        IReadOnlyList<SquadMember> members)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(members);

        var memberIds = members.Select(m => m.PlayerId.Value).ToHashSet();

        foreach (var state in store.GetPhysicalStatesForClub(clubId))
        {
            if (!memberIds.Contains(state.PlayerId.Value))
            {
                store.UpsertPhysical(state.ClearLocation());
            }
        }

        foreach (var member in members)
        {
            var existing = store.GetPhysical(member.PlayerId)
                ?? PlayerPhysicalState.CreateRested(member.PlayerId, clubId, member.SlotIndex);
            if (!existing.HasLocation
                || existing.ClubId != clubId
                || existing.SlotIndex != member.SlotIndex)
            {
                store.UpsertPhysical(existing.WithLocation(clubId, member.SlotIndex));
            }
        }
    }

    public static void ClearPlayerLocation(ITrainingPhysicalStateStore store, Domain.PlayerCareer.PlayerId playerId)
    {
        ArgumentNullException.ThrowIfNull(store);
        var existing = store.GetPhysical(playerId);
        if (existing is { HasLocation: true })
        {
            store.UpsertPhysical(existing.ClearLocation());
        }
    }
}
