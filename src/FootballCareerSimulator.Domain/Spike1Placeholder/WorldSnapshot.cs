namespace FootballCareerSimulator.Domain.Spike1Placeholder;

public sealed record ClubSnapshot(int ClubId, string Name);

public sealed record PlayerSnapshot(int PlayerId, int ClubId, int Age, int Form);

/// <summary>
/// Spike 2 (bkz. docs/18_SPIKE_EXECUTION_PLAN.md Kart 3) için oluşturulmuş, `World`'ün fiziksel
/// serialization biçiminden bağımsız, saf semantic içeriğini taşıyan yer tutucu bir kayıt (snapshot)
/// temsilidir. `docs/13_SAVE_SYSTEM.md`'deki gerçek save formatının yerine geçmez; yalnızca "mevcut
/// domain state'i persist edip geri yükleme" akışının mimari olarak çalıştığını kanıtlar.
/// </summary>
public sealed record WorldSnapshot(
    int CurrentSeason,
    IReadOnlyList<ClubSnapshot> Clubs,
    IReadOnlyList<PlayerSnapshot> Players);
