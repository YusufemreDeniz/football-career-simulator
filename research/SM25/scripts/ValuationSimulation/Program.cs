using System.Globalization;
using System.Text;
using FootballCareerSimulator.Application.CareerHub.Queries;
using FootballCareerSimulator.Application.Transfer.Services;

var outputPath = args.Length > 0
    ? Path.GetFullPath(args[0])
    : Path.GetFullPath(Path.Combine("research", "SM25", "simulations", "player_valuation_matrix.csv"));
var wageOutputPath = args.Length > 1
    ? Path.GetFullPath(args[1])
    : Path.GetFullPath(Path.Combine("research", "SM25", "simulations", "weekly_wage_simulation.csv"));
var negotiationOutputPath = args.Length > 2
    ? Path.GetFullPath(args[2])
    : Path.GetFullPath(Path.Combine("research", "SM25", "simulations", "contract_negotiation_matrix.csv"));
Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
Directory.CreateDirectory(Path.GetDirectoryName(wageOutputPath)!);
Directory.CreateDirectory(Path.GetDirectoryName(negotiationOutputPath)!);

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

int[] wageAbilities = [40, 55, 65, 75, 85, 92, 99];
int[] wageAges = [19, 27, 34];
int[] wageContractYears = [1, 3, 5];
(int Club, int League)[] wageContexts = [(50, 50), (70, 68), (85, 82), (95, 92)];
(string Name, double ManagerTrait, double Staff, double Role, double Market)[] wageStates =
[
    ("normal", 0.0, 0.0, 1.0, 1.0),
    ("manager_trait", -10.0, 0.0, 1.0, 1.0),
    ("staff_negotiator", 0.0, -8.0, 1.0, 1.0),
    ("key_player", 0.0, 0.0, 1.25, 1.0),
    ("free_agent", 0.0, 0.0, 1.0, 1.20),
];

var wageCsv = new StringBuilder();
wageCsv.AppendLine("CA,PA,age,clubStrength,leagueStrength,contractLength,traitState,raw,rounded,capped");
var wageRowCount = 0;

foreach (var ability in wageAbilities)
foreach (var potential in new[] { ability, Math.Min(99, ability + 10) }.Distinct())
foreach (var age in wageAges)
foreach (var context in wageContexts)
foreach (var contractLength in wageContractYears)
foreach (var state in wageStates)
{
    var result = PlayerWageModel.Evaluate(
        new PlayerWageCalculationInput(
            ability,
            potential,
            age,
            context.Club,
            context.League,
            contractLength,
            IsPeaked: age >= 30,
            ManagerTraitPercentage: state.ManagerTrait,
            StaffNegotiatorPercentage: state.Staff,
            SquadRoleFactor: state.Role,
            MarketStatusFactor: state.Market));

    wageCsv.Append(ability).Append(',')
        .Append(potential).Append(',')
        .Append(age).Append(',')
        .Append(context.Club).Append(',')
        .Append(context.League).Append(',')
        .Append(contractLength).Append(',')
        .Append(state.Name).Append(',')
        .Append(result.RawWeeklyWage.ToString("0.00", CultureInfo.InvariantCulture)).Append(',')
        .Append(result.RoundedWeeklyWage).Append(',')
        .Append(result.WeeklyWage)
        .AppendLine();
    wageRowCount++;
}

File.WriteAllText(wageOutputPath, wageCsv.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
Console.WriteLine($"WAGE_SIMULATION=PASS rows={wageRowCount} output={wageOutputPath}");

int[] negotiationRatings = [60, 75, 90];
int[] negotiationAges = [20, 33];
int[] negotiationLengths = [1, 3, 5];
double[] offerRatios = [0.49, 0.90, 1.00, 1.10];
(int Club, int League)[] negotiationContexts = [(70, 68), (90, 88)];

var negotiationCsv = new StringBuilder();
negotiationCsv.AppendLine(
    "rating,potential,peaked,club_rating,league_rating,contract_years,trait_36_pct,trait_38_pct," +
    "demanded_wage,displayed_wage,offered_wage,signing_fee_demand,offered_signing_fee," +
    "simulator_bonus_offer,squad_status,desired_squad_status,patience_before,negotiation_round,native_negotiation_count," +
    "rng_player_id,rng_game_seed,accepted,rejection_reason,patience_after");
var negotiationRowCount = 0;

foreach (var rating in negotiationRatings)
foreach (var age in negotiationAges)
foreach (var context in negotiationContexts)
foreach (var contractLength in negotiationLengths)
foreach (var offerRatio in offerRatios)
foreach (var lowerStatus in new[] { false, true })
{
    var potential = Math.Min(99, rating + (age < 30 ? 8 : 0));
    var peaked = age >= 30;
    var baseWage = PlayerWageModel.Evaluate(
        new PlayerWageCalculationInput(
            rating,
            potential,
            age,
            context.Club,
            context.League,
            contractLength,
            IsPeaked: peaked,
            ManagerTraitPercentage: -5.0,
            StaffNegotiatorPercentage: -10.0)).WeeklyWage;
    var desiredStatus = rating >= 88
        ? ContractSquadStatus.KeyPlayer
        : rating >= 72
            ? ContractSquadStatus.FirstTeam
            : ContractSquadStatus.Rotation;
    var seed = 2_026 + rating * 31 + age * 7 + contractLength;
    var demand = PlayerContractDemandModel.Generate(
        new PlayerContractDemandInput(
            PlayerId: rating * 100 + age,
            GameSeed: seed,
            BaseAcceptableWeeklyWage: baseWage,
            PlayerValue: Math.Max(100_000, baseWage * 250),
            Age: age,
            PlayerRating: rating,
            TargetClubAverageRating: context.Club,
            DesiredContractYears: contractLength,
            MinimumContractYears: 1,
            DesiredSquadStatus: desiredStatus,
            IsTargetUserClub: true,
            TransferNegotiatorMultiplier: 0.90,
            SigningFeeTraitPercentage: -5.0));
    var offeredStatus = lowerStatus && desiredStatus < ContractSquadStatus.Youngster
        ? desiredStatus + 1
        : desiredStatus;
    var offeredWage = (int)Math.Round(demand.WeeklyWage * offerRatio, MidpointRounding.ToEven);
    var offeredSigningFee = offerRatio < 1.0
        ? (int)Math.Round(demand.SigningFee * 0.75, MidpointRounding.ToEven)
        : demand.SigningFee;
    var offer = new ContractOffer(
        offeredWage,
        offeredSigningFee,
        contractLength,
        offeredStatus);
    var state = ContractNegotiationState.Start(
        isTransferWanted: false,
        hasActiveConcerns: rating >= 90);
    var patienceBefore = state.CurrentPatience;
    var result = ContractOfferEvaluator.Evaluate(demand, offer, state);

    negotiationCsv.Append(rating).Append(',')
        .Append(potential).Append(',')
        .Append(peaked.ToString().ToLowerInvariant()).Append(',')
        .Append(context.Club).Append(',')
        .Append(context.League).Append(',')
        .Append(contractLength).Append(',')
        .Append("-5.0,-5.0,")
        .Append(demand.WeeklyWage).Append(',')
        .Append(demand.DisplayedWeeklyWage).Append(',')
        .Append(offeredWage).Append(',')
        .Append(demand.SigningFee).Append(',')
        .Append(offeredSigningFee).Append(',')
        .Append(0).Append(',')
        .Append(offeredStatus).Append(',')
        .Append(desiredStatus).Append(',')
        .Append(patienceBefore.ToString("0.0", CultureInfo.InvariantCulture)).Append(',')
        .Append(result.State.Round).Append(',')
        .Append(result.State.NativeNegotiationCount).Append(',')
        .Append(rating * 100 + age).Append(',')
        .Append(seed).Append(',')
        .Append(result.Accepted.ToString().ToLowerInvariant()).Append(',')
        .Append(result.Reason).Append(',')
        .Append(result.State.CurrentPatience.ToString("0.0", CultureInfo.InvariantCulture))
        .AppendLine();
    negotiationRowCount++;
}

File.WriteAllText(
    negotiationOutputPath,
    negotiationCsv.ToString(),
    new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
Console.WriteLine(
    $"CONTRACT_NEGOTIATION_SIMULATION=PASS rows={negotiationRowCount} output={negotiationOutputPath}");
