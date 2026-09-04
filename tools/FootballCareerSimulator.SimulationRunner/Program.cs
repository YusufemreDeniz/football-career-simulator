using FootballCareerSimulator.Application.WorldCalendar;

// Production World & Calendar headless kosusu (Kart 4).

var seed = args.Length > 0 && int.TryParse(args[0], out var parsedSeed) ? parsedSeed : 42;
var seasonCount = args.Length > 1 && int.TryParse(args[1], out var parsedSeasonCount) ? parsedSeasonCount : 10;

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
