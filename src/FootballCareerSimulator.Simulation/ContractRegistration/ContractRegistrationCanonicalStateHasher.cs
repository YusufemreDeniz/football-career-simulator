using System.Text;
using FootballCareerSimulator.Domain.ContractRegistration;

namespace FootballCareerSimulator.Simulation.ContractRegistration;

public static class ContractRegistrationCanonicalStateHasher
{
    public static string BuildCanonicalText(IReadOnlyList<PlayerContract> contracts)
    {
        ArgumentNullException.ThrowIfNull(contracts);

        var builder = new StringBuilder("Contracts=");
        foreach (var contract in contracts
                     .OrderBy(c => c.Id.Value)
                     .ThenBy(c => c.PlayerId.Value))
        {
            builder.Append("Id=").Append(contract.Id.Value)
                .Append(";P=").Append(contract.PlayerId.Value)
                .Append(";C=").Append(contract.ClubId.Value)
                .Append(";S=").Append(contract.StartDate.DayNumber)
                .Append(";E=").Append(contract.EndDate.DayNumber)
                .Append(";W=").Append(contract.WeeklyWage)
                .Append(";St=").Append((int)contract.Status)
                .Append('|');
        }

        return builder.ToString();
    }
}
