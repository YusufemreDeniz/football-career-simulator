namespace FootballCareerSimulator.Application.Spike4Placeholder;

/// <summary>
/// Spike 4 (bkz. docs/18_SPIKE_EXECUTION_PLAN.md Kart 6) için oluşturulmuş, Godot UI'ının render
/// edeceği yer tutucu bir okuma modelidir (read model). Gerçek futbolcu görüntüleme sözleşmesi
/// `docs/03_DOMAIN_MODEL.md` ve ilgili UI tasarım çalışmalarında ayrıca kesinleştirilecektir.
/// </summary>
public sealed record PlayerListRow(int PlayerId, string PlayerLabel, int ClubId, string ClubName, int Age, int Form);
