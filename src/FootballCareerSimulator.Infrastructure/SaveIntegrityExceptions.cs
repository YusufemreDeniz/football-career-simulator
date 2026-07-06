namespace FootballCareerSimulator.Infrastructure;

/// <summary>
/// Bir save dosyası okunamayacak veya güvenle yüklenemeyecek durumdayken fırlatılan ortak temel
/// istisnadır (bkz. docs/18_SPIKE_EXECUTION_PLAN.md Kart 4, Spike 3).
/// </summary>
public abstract class SaveIntegrityException : Exception
{
    protected SaveIntegrityException(string message)
        : base(message)
    {
    }

    protected SaveIntegrityException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Save dosyası açılamadığında (bozuk SQLite dosyası) veya okunan içerik saklanan bütünlük hash'i ile
/// eşleşmediğinde (bilinçli/kazara veri değişikliği) fırlatılır. Bozuk bir save asla sessizce geçerli
/// bir state olarak yüklenmez.
/// </summary>
public sealed class SaveCorruptionException : SaveIntegrityException
{
    public SaveCorruptionException(string message)
        : base(message)
    {
    }

    public SaveCorruptionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Save dosyasının şema sürümü, bu spike'ın desteklediği minimum sürümden düşük veya güncel sürümden
/// yüksek olduğunda fırlatılır. Bilinmeyen bir sürüm sessizce tahmin edilmez
/// (`docs/15_DECISION_LOG.md` D-069 ile uyumlu).
/// </summary>
public sealed class UnsupportedSaveSchemaVersionException : SaveIntegrityException
{
    public int FoundVersion { get; }

    public UnsupportedSaveSchemaVersionException(int foundVersion)
        : base($"Desteklenmeyen save şema sürümü: {foundVersion}.")
    {
        FoundVersion = foundVersion;
    }
}

public sealed class UnsupportedLegacySpikeSaveException : SaveIntegrityException
{
    public UnsupportedLegacySpikeSaveException()
        : base("Spike placeholder save format is not supported by the production save loader.")
    {
    }
}
