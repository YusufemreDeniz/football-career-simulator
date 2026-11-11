using FootballCareerSimulator.Application.CareerHub.Queries;
using Godot;
using System.Text.Json;

namespace FootballCareerSimulator.Presentation;

/// <summary>
/// Kariyer save şemasından bağımsız cihaz tercihleri. Bozuk/eskimiş dosyada
/// güvenli varsayılanlara döner; hiçbir zaman kariyer yüklemeyi engellemez.
/// </summary>
internal static class GameExperienceSettingsStore
{
    private const string FileName = "experience-settings.json";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static GameExperiencePreferences? _current;

    public static event Action<GameExperiencePreferences>? Changed;

    public static GameExperiencePreferences Current => _current ??= Load();

    public static GameExperiencePreferences Update(
        Func<GameExperiencePreferences, GameExperiencePreferences> update)
    {
        ArgumentNullException.ThrowIfNull(update);
        var current = Current;
        var next = update(current).Normalize();
        if (next == current || !Save(next))
        {
            return current;
        }

        _current = next;
        Changed?.Invoke(next);
        return next;
    }

    private static GameExperiencePreferences Load()
    {
        try
        {
            var path = Path.Combine(OS.GetUserDataDir(), FileName);
            if (!File.Exists(path))
            {
                return GameExperiencePreferences.Default;
            }

            return (JsonSerializer.Deserialize<GameExperiencePreferences>(File.ReadAllText(path))
                    ?? GameExperiencePreferences.Default)
                .Normalize();
        }
        catch (Exception ex)
        {
            GD.PushWarning($"Oyun deneyimi ayarları okunamadı; varsayılanlar kullanılıyor: {ex.Message}");
            return GameExperiencePreferences.Default;
        }
    }

    private static bool Save(GameExperiencePreferences preferences)
    {
        try
        {
            var path = Path.Combine(OS.GetUserDataDir(), FileName);
            var tempPath = path + ".tmp";
            File.WriteAllText(tempPath, JsonSerializer.Serialize(preferences, JsonOptions));
            File.Move(tempPath, path, overwrite: true);
            return true;
        }
        catch (Exception ex)
        {
            GD.PushWarning($"Oyun deneyimi ayarları kaydedilemedi: {ex.Message}");
            return false;
        }
    }
}
