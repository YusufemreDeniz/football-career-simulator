using FootballCareerSimulator.Application.WorldCalendar;
using FootballCareerSimulator.Simulation.Spike1Placeholder;
using FootballCareerSimulator.Simulation.WorldCalendar;

// Varsayilan: Production World & Calendar headless kosusu (Kart 4).
// `--spike1` ile legacy Spike 1 placeholder dunyasi calistirilir.

var useSpike1 = args.Contains("--spike1", StringComparer.OrdinalIgnoreCase);
var filteredArgs = args.Where(arg => !string.Equals(arg, "--spike1", StringComparison.OrdinalIgnoreCase)).ToArray();

var seed = filteredArgs.Length > 0 && int.TryParse(filteredArgs[0], out var parsedSeed) ? parsedSeed : 42;
var seasonCount = filteredArgs.Length > 1 && int.TryParse(filteredArgs[1], out var parsedSeasonCount) ? parsedSeasonCount : 10;

if (useSpike1)
{
    Console.WriteLine("Football Career Simulator - Headless Simulation Runner (Spike 1 placeholder)");
    Console.WriteLine($"Seed: {seed}");
    Console.WriteLine($"Hedeflenen sezon sayisi: {seasonCount}");
    Console.WriteLine();

    var spikeReport = HeadlessSimulationRunner.Run(seed, seasonCount);

    Console.WriteLine("Sonuc raporu:");
    Console.WriteLine($"  RNG surumu          : {spikeReport.RandomContextVersion}");
    Console.WriteLine($"  Tamamlanan sezon    : {spikeReport.SeasonCount}");
    Console.WriteLine($"  Kulup sayisi        : {spikeReport.ClubCount}");
    Console.WriteLine($"  Futbolcu sayisi     : {spikeReport.PlayerCount}");
    Console.WriteLine($"  Canonical state hash: {spikeReport.CanonicalStateHash}");
    Console.WriteLine($"  Sure                : {spikeReport.ElapsedMilliseconds} ms");
    Console.WriteLine($"  Bellek (calisma once): {spikeReport.MemoryBeforeBytes / 1024.0 / 1024.0:F2} MB");
    Console.WriteLine($"  Bellek (calisma sonra): {spikeReport.MemoryAfterBytes / 1024.0 / 1024.0:F2} MB");
    return;
}

Console.WriteLine("Football Career Simulator - Headless World & Calendar Runner");
Console.WriteLine($"Seed: {seed}");
Console.WriteLine($"Hedeflenen sezon sayisi: {seasonCount}");
Console.WriteLine();

var report = WorldCalendarHeadlessRunner.Run(seed, seasonCount);

Console.WriteLine("Sonuc raporu:");
Console.WriteLine($"  RNG surumu          : {report.RandomContextVersion}");
Console.WriteLine($"  Tamamlanan sezon    : {report.SeasonCount}");
Console.WriteLine($"  Simule edilen gun   : {report.SimulatedDayCount}");
Console.WriteLine($"  Son DayNumber       : {report.FinalDayNumber}");
Console.WriteLine($"  Canonical state hash: {report.CanonicalStateHash}");
Console.WriteLine($"  Commit edilen event : {report.CommittedEventCount}");
Console.WriteLine($"  Sure                : {report.ElapsedMilliseconds} ms");
Console.WriteLine($"  Bellek (calisma once): {report.MemoryBeforeBytes / 1024.0 / 1024.0:F2} MB");
Console.WriteLine($"  Bellek (calisma sonra): {report.MemoryAfterBytes / 1024.0 / 1024.0:F2} MB");
