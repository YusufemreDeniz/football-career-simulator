using FootballCareerSimulator.Domain.Spike1Placeholder;

namespace FootballCareerSimulator.Infrastructure;

/// <summary>
/// `SqliteSaveReader.Load` çağrısının sonucudur; yüklenen dünya durumunun yanında determinizm
/// devamlılığı için gereken seed/RNG bilgisini ve migration'ın gerçekleşip gerçekleşmediğini taşır.
/// </summary>
public sealed record SqliteLoadResult(
    World World,
    int RootSeed,
    string RandomContextVersion,
    int SchemaVersionLoaded,
    bool WasMigrated);
