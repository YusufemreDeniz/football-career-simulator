using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;
using PlayerCareerAggregate = FootballCareerSimulator.Domain.PlayerCareer.PlayerCareer;

namespace FootballCareerSimulator.Application.PlayerCareer.Ports;

/// <summary>
/// Emekli olan oyuncunun kariyer slotunu, hazır bir akademi oyuncusuyla doldurmak
/// için sezon yaşam döngüsünün kullandığı dar sınır.
/// </summary>
public interface IYouthAcademySuccessorProvider
{
    PlayerCareerAggregate? CreateSuccessor(
        ClubId clubId,
        int slotIndex,
        int generation,
        GameDate day,
        IReadOnlySet<FootballCareerSimulator.Domain.PlayerCareer.PlayerId> excludedPlayerIds);
}
