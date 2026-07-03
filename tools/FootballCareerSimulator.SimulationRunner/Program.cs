using FootballCareerSimulator.Simulation.Spike1Placeholder;

// docs/18_SPIKE_EXECUTION_PLAN.md Kart 2 (Spike 1): motor/UI olmadan calisan, ~20 kulup / ~500
// futbolculuk bir dunyayi N sezon ilerleten headless dogrulama araci. Gercek oyun executable'i
// degildir; sadece docs/17_TECHNOLOGY_AND_ARCHITECTURE_DECISION.md Bolum 16 basari kriterlerini
// kanitlamak icin kullanilir.

var seed = args.Length > 0 && int.TryParse(args[0], out var parsedSeed) ? parsedSeed : 42;
var seasonCount = args.Length > 1 && int.TryParse(args[1], out var parsedSeasonCount) ? parsedSeasonCount : 10;

Console.WriteLine("Football Career Simulator - Headless Simulation Runner (Spike 1 placeholder)");
Console.WriteLine($"Seed: {seed}");
Console.WriteLine($"Hedeflenen sezon sayisi: {seasonCount}");
Console.WriteLine();

var report = HeadlessSimulationRunner.Run(seed, seasonCount);

Console.WriteLine("Sonuc raporu:");
Console.WriteLine($"  Tamamlanan sezon    : {report.SeasonCount}");
Console.WriteLine($"  Kulup sayisi        : {report.ClubCount}");
Console.WriteLine($"  Futbolcu sayisi     : {report.PlayerCount}");
Console.WriteLine($"  Sure                : {report.ElapsedMilliseconds} ms");
Console.WriteLine($"  Bellek (calisma once): {report.MemoryBeforeBytes / 1024.0 / 1024.0:F2} MB");
Console.WriteLine($"  Bellek (calisma sonra): {report.MemoryAfterBytes / 1024.0 / 1024.0:F2} MB");
