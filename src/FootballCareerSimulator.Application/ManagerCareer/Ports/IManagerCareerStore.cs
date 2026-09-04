namespace FootballCareerSimulator.Application.ManagerCareer.Ports;

using FootballCareerSimulator.Domain.ManagerCareer;

public interface IManagerCareerStore
{
    ManagerCareer Career { get; }

    void Replace(ManagerCareer career);
}
