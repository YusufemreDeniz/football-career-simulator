using System.Globalization;
using System.Text;
using FootballCareerSimulator.Application.CareerHub.Queries;

var outputPath = args.Length > 0
    ? Path.GetFullPath(args[0])
    : Path.GetFullPath(Path.Combine("research", "SM25", "simulations", "player_valuation_matrix.csv"));
Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

int[] ratings = [40, 50, 60, 65, 70, 75, 80, 85, 88, 90, 92, 94, 95, 97, 99];
int[] ages = [16, 18, 20, 22, 24, 26, 28, 30, 32, 35, 38];
double[] contractYears = [0, 1, 2, 3, 4, 5];
(string Name, int Strength, double LeagueFactor)[] clubContexts =
[
    ("low", 50, 0.85),
    ("mid", 65, 0.95),
    ("high", 80, 1.05),
    ("elite", 95, 1.15),
];

var csv = new StringBuilder();
csv.AppendLine("rating,potential,age,contract_years,club_tier,source_club_strength,league_factor,market_value,ability_value,potential_multiplier,age_multiplier,contract_multiplier,market_context_multiplier,short_term_multiplier");
var rowCount = 0;

foreach (var rating in ratings)
{
    var potentials = new[] { rating, rating + 5, rating + 10, 95, 99 }
        .Select(value => Math.Clamp(value, rating, 99))
        .Distinct()
        .Order()
        .ToArray();

    foreach (var age in ages)
    foreach (var potential in potentials)
    foreach (var contract in contractYears)
    foreach (var club in clubContexts)
    {
        var result = PlayerMarketValuationModel.Evaluate(
            new PlayerMarketValuationInput(
                rating,
                potential,
                age,
                contract,
                club.Strength,
                LeagueMarketFactor: club.LeagueFactor));

        csv.Append(rating).Append(',')
            .Append(potential).Append(',')
            .Append(age).Append(',')
            .Append(contract.ToString("0.##", CultureInfo.InvariantCulture)).Append(',')
            .Append(club.Name).Append(',')
            .Append(club.Strength).Append(',')
            .Append(club.LeagueFactor.ToString("0.00", CultureInfo.InvariantCulture)).Append(',')
            .Append(result.MarketValue).Append(',')
            .Append(result.AbilityValue.ToString("0.00", CultureInfo.InvariantCulture)).Append(',')
            .Append(result.PotentialMultiplier.ToString("0.0000", CultureInfo.InvariantCulture)).Append(',')
            .Append(result.AgeMultiplier.ToString("0.0000", CultureInfo.InvariantCulture)).Append(',')
            .Append(result.ContractMultiplier.ToString("0.0000", CultureInfo.InvariantCulture)).Append(',')
            .Append(result.MarketContextMultiplier.ToString("0.0000", CultureInfo.InvariantCulture)).Append(',')
            .Append(result.ShortTermMultiplier.ToString("0.0000", CultureInfo.InvariantCulture))
            .AppendLine();
        rowCount++;
    }
}

File.WriteAllText(outputPath, csv.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
Console.WriteLine($"VALUATION_SIMULATION=PASS rows={rowCount} output={outputPath}");
