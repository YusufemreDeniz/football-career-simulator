namespace FootballCareerSimulator.Application.ManagerCareer.Services;

using FootballCareerSimulator.Application.ManagerCareer.Ports;
using FootballCareerSimulator.Application.ManagerCareer.Queries;

public sealed class ManagerCareerQueryService
{
    private readonly IManagerCareerStore _store;

    public ManagerCareerQueryService(IManagerCareerStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public ManagerCareerReadModel GetCareer()
    {
        var career = _store.Career;
        return new ManagerCareerReadModel(
            career.ManagerId.Value,
            career.DisplayName,
            career.ActiveEmployment?.ClubId.Value,
            career.ActiveEmployment?.StartedAt.DayNumber);
    }
}
