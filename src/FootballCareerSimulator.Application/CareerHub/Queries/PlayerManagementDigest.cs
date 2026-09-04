using FootballCareerSimulator.Application.TeamPreparation.Queries;
using FootballCareerSimulator.Domain.ContractRegistration;
using FootballCareerSimulator.Domain.PlayerCareer;
using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.SocialContinuity;
using FootballCareerSimulator.Domain.TeamPreparation;
using FootballCareerSimulator.Domain.TrainingPhysicalState;
using FootballCareerSimulator.Domain.WorldCalendar;
using FootballCareerSimulator.Simulation.TeamPreparation;
using PlayerCareerAggregate = FootballCareerSimulator.Domain.PlayerCareer.PlayerCareer;

namespace FootballCareerSimulator.Application.CareerHub.Queries;

public sealed record PlayerManagementLine(
    long PlayerId,
    int SlotIndex,
    int SquadNumber,
    string DisplayName,
    string PositionCode,
    string PositionName,
    int Rating,
    int Age,
    int CurrentAbility,
    int PotentialAbility,
    string CareerPhase,
    int Fitness,
    int Fatigue,
    string Availability,
    bool IsInjured,
    string FatigueRiskBand,
    bool HasFatigueRisk,
    bool HasPromiseRisk,
    int MatchMinutesLast7Days,
    int DaysSinceLastMatch,
    string? InjuryReasonCode,
    string? WorkloadHint,
    int? WeeklyWage,
    string ContractEnd,
    int? Trust,
    int? Respect,
    int? Compatibility,
    string RelationshipState,
    string PromiseSummary,
    string CausalitySummary)
{
    public string ToListLabel() =>
        $"{SquadNumber}. {DisplayName} · {PositionCode} · GÜÇ {Rating} · {FatigueRiskBand}"
        + (Availability == "Hazır" ? string.Empty : $" · {Availability.ToUpperInvariant()}");

    public string ToSaleLabel() => $"{DisplayName} ({PositionCode} · GÜÇ {Rating})";

    public string ToDetailText()
    {
        var contract = WeeklyWage is int wage
            ? $"Haftalık ücret {wage:N0} · bitiş {ContractEnd}"
            : "Aktif sözleşme yok";
        var relationship = Trust is int trust && Respect is int respect && Compatibility is int compatibility
            ? $"Güven {trust} · saygı {respect} · uyum {compatibility}"
            : "Henüz bireysel ilişki kaydı yok";

        var workload = WorkloadHint is null ? string.Empty : $"\nYük: {WorkloadHint}";
        var injuryReason = InjuryReasonCode is null
            ? string.Empty
            : $"\nSakatlık nedeni: {InjuryReasonLabel(InjuryReasonCode)}";
        return $"{DisplayName} · {PositionName} ({PositionCode})\n"
            + $"Yaş {Age} · CA {CurrentAbility} / PA {PotentialAbility} · {CareerPhase}\n"
            + $"Kondisyon: {FatigueRiskBand} · fitness %{Fitness} · {Availability}"
            + workload
            + injuryReason
            + "\n"
            + $"{contract}\n"
            + $"İlişki: {RelationshipState} · {relationship}\n"
            + $"Sözler: {PromiseSummary}\n"
            + $"Nedensellik: {CausalitySummary}";
    }

    public string ToDossierText()
    {
        var contract = WeeklyWage is int wage
            ? $"Haftalık ücret {wage:N0}\nSözleşme bitişi {ContractEnd}"
            : "Aktif sözleşme yok";
        var relationship = Trust is int trust && Respect is int respect && Compatibility is int compatibility
            ? $"Güven {trust} · saygı {respect} · uyum {compatibility}"
            : "Henüz bireysel ilişki kaydı yok";

        var workload = WorkloadHint is null ? string.Empty : $"Yük: {WorkloadHint}\n";
        var injuryReason = InjuryReasonCode is null
            ? string.Empty
            : $"Sakatlık nedeni: {InjuryReasonLabel(InjuryReasonCode)}\n";
        return $"{PositionName} · {PositionCode}\n"
            + $"Güç {Rating} · yaş {Age} · {CareerPhase}\n"
            + $"Yetenek {CurrentAbility} / potansiyel {PotentialAbility}\n"
            + $"Kondisyon: {FatigueRiskBand}\n"
            + $"Fitness %{Fitness} · yorgunluk %{Fatigue}\n"
            + $"Durum: {Availability}\n"
            + workload
            + injuryReason
            + "\n"
            + $"{contract}\n\n"
            + $"İlişki: {RelationshipState}\n{relationship}\n\n"
            + $"Sözler: {PromiseSummary}\n"
            + $"Not: {CausalitySummary}";
    }

    private static string InjuryReasonLabel(string code) => code switch
    {
        PlayerPhysicalState.ReasonTrainingLoad => "Antrenman yükü",
        PlayerPhysicalState.ReasonMatchLoad => "Maç yükü",
        PlayerPhysicalState.ReasonAccumulatedWorkload => "Birikimli iş yükü",
        PlayerPhysicalState.ReasonReturnFromInjury => "Sakatlıktan dönüş",
        PlayerPhysicalState.ReasonUnexpected => "Beklenmedik / temas",
        _ => code,
    };
}

public sealed record PlayerManagementDigest(
    bool HasClub,
    string Headline,
    IReadOnlyList<PlayerManagementLine> Players)
{
    public static PlayerManagementDigest Clear() =>
        new(false, "Futbolcu yönetimi: kulüp görevi yok.", Array.Empty<PlayerManagementLine>());

    public static PlayerManagementDigest Compose(
        ClubId clubId,
        long managerId,
        GameDate day,
        IReadOnlyList<MvpSquadPlayerProfile> profiles,
        IReadOnlyList<SquadPlayerReadModel> squadPlayers,
        ClubSquad? membership,
        IReadOnlyList<PlayerCareerAggregate> careers,
        IReadOnlyDictionary<(long ClubId, int SlotIndex), PlayerPhysicalState> physicalBySlot,
        IReadOnlyList<PlayerContract> contracts,
        IReadOnlyList<RelationshipRecord> relationships,
        IReadOnlyList<Promise> promises,
        IReadOnlyList<MemoryRecord>? memories = null)
    {
        ArgumentNullException.ThrowIfNull(profiles);
        ArgumentNullException.ThrowIfNull(squadPlayers);
        ArgumentNullException.ThrowIfNull(careers);
        ArgumentNullException.ThrowIfNull(physicalBySlot);
        ArgumentNullException.ThrowIfNull(contracts);
        ArgumentNullException.ThrowIfNull(relationships);
        ArgumentNullException.ThrowIfNull(promises);
        memories ??= Array.Empty<MemoryRecord>();

        var memberBySlot = membership?.Members.ToDictionary(member => member.SlotIndex)
            ?? new Dictionary<int, SquadMember>();
        var players = squadPlayers
            .OrderBy(player => player.SlotIndex)
            .Select(player =>
            {
                var slot = player.SlotIndex;
                var playerId = memberBySlot.TryGetValue(slot, out var member)
                    ? member.PlayerId
                    : PlayerId.FromClubSlot(clubId.Value, slot);
                var profile = slot >= 0 && slot < profiles.Count
                    ? profiles[slot]
                    : new MvpSquadPlayerProfile(player.DisplayName, MvpSquadPositionGroup.Midfielder);
                var career = careers.FirstOrDefault(candidate => candidate.Id == playerId)
                    ?? careers.FirstOrDefault(candidate =>
                        candidate.OriginClubId == clubId && candidate.SlotIndex == slot);
                var physical = physicalBySlot.TryGetValue((clubId.Value, slot), out var state)
                    ? state
                    : PlayerPhysicalState.CreateRested(clubId, slot);
                var contract = contracts.FirstOrDefault(candidate =>
                    candidate.PlayerId == playerId && candidate.IsActiveOn(day));
                var relationship = relationships.FirstOrDefault(candidate =>
                    candidate.Status == RelationshipStatus.Active
                    && candidate.Observer.Kind == ActorKind.Player
                    && candidate.Observer.Id == playerId.Value
                    && candidate.Subject.Kind == ActorKind.Manager
                    && candidate.Subject.Id == managerId);
                var playerPromises = promises
                    .Where(candidate =>
                        candidate.Promisee.Kind == ActorKind.Player
                        && candidate.Promisee.Id == playerId.Value)
                    .OrderByDescending(candidate => candidate.IsActive)
                    .ThenBy(candidate => candidate.DeadlineOn.DayNumber)
                    .ToArray();
                var activePromises = playerPromises.Where(candidate => candidate.IsActive).ToArray();
                var playerMemories = memories
                    .Where(candidate =>
                        candidate.Status == MemoryStatus.Active
                        && candidate.RememberingActor.Kind == ActorKind.Player
                        && candidate.RememberingActor.Id == playerId.Value
                        && candidate.Category is MemoryCategory.Promise or MemoryCategory.Trust)
                    .OrderByDescending(candidate => candidate.CreatedOn.DayNumber)
                    .ThenByDescending(candidate => candidate.MemoryId.Value)
                    .ToArray();

                var band = PlayerPhysicalState.FatigueBandLabel(physical.Fatigue);
                var promiseLabel = PromiseLabel(activePromises);
                var hasPromiseRisk = promiseLabel.Contains("risk", StringComparison.OrdinalIgnoreCase)
                    || promiseLabel.Contains("bozul", StringComparison.OrdinalIgnoreCase)
                    || RelationshipLabel(relationship).Contains("Kırılgan", StringComparison.Ordinal);
                var workloadHint = BuildWorkloadHint(physical, day);

                return new PlayerManagementLine(
                    playerId.Value,
                    slot,
                    player.SquadNumber,
                    profile.DisplayName,
                    profile.PositionCode,
                    profile.PositionName,
                    player.Rating,
                    career?.AgeYears(day) ?? 18,
                    career?.CurrentAbility ?? player.Rating,
                    career?.PotentialAbility ?? player.Rating,
                    CareerPhaseLabel(career?.GetPhase(day)),
                    physical.Fitness,
                    physical.Fatigue,
                    AvailabilityLabel(physical, day),
                    !physical.IsAvailableOn(day),
                    band,
                    physical.Fatigue >= 65
                        || band is "Yüksek Risk" or "Çok Yorgun",
                    hasPromiseRisk,
                    physical.MatchMinutesLast7Days,
                    physical.DaysSinceLastMatch(day),
                    physical.LastInjuryReasonCode,
                    workloadHint,
                    contract?.WeeklyWage,
                    contract is null ? "—" : $"{contract.EndDate.Year:D4}-{contract.EndDate.Month:D2}-{contract.EndDate.Day:D2}",
                    relationship?.Trust,
                    relationship?.Respect,
                    relationship?.ProfessionalCompatibility,
                    RelationshipLabel(relationship),
                    promiseLabel,
                    CausalityLabel(relationship, playerPromises, playerMemories));
            })
            .ToArray();

        return new PlayerManagementDigest(
            true,
            $"Futbolcu yönetim merkezi · {players.Length} oyuncu",
            players);
    }

    private static string CareerPhaseLabel(CareerPhase? phase) => phase switch
    {
        CareerPhase.Developing => "Gelişim çağında",
        CareerPhase.Peak => "Zirve döneminde",
        CareerPhase.Declining => "Tecrübe döneminde",
        _ => "Kariyer profili bekleniyor",
    };

    private static string AvailabilityLabel(PlayerPhysicalState physical, GameDate day)
    {
        if (!physical.IsAvailableOn(day))
        {
            return physical.InjuredUntilDayNumber is int until
                ? $"Sakat · dönüş {GameDate.ToDisplayDateString(until)}"
                : "Sakat";
        }

        return physical.Fatigue >= 65 || physical.Fitness < 55
            ? PlayerPhysicalState.FatigueBandLabel(physical.Fatigue)
            : "Hazır";
    }

    private static string? BuildWorkloadHint(PlayerPhysicalState physical, GameDate day)
    {
        var parts = new List<string>();
        if (physical.MatchMinutesLast7Days > 0)
        {
            parts.Add($"Son 7 günde {physical.MatchMinutesLast7Days} dk");
        }

        if (physical.MatchMinutesLast14Days >= 270)
        {
            parts.Add($"Son 14 günde {physical.MatchMinutesLast14Days} dk");
        }

        if (physical.HasCongestedFixture(day))
        {
            parts.Add($"{physical.DaysSinceLastMatch(day)} günde ikinci maç");
        }

        if (physical.Fatigue >= 65)
        {
            parts.Add("Yüksek yorgunluk birikimi");
        }

        return parts.Count == 0 ? null : string.Join(" · ", parts);
    }

    private static string RelationshipLabel(RelationshipRecord? relationship)
    {
        if (relationship is null)
        {
            return "Yeni";
        }

        var average = (relationship.Trust + relationship.Respect + relationship.ProfessionalCompatibility) / 3;
        return average switch
        {
            >= 65 => "Güçlü",
            <= 35 => "Kırılgan",
            _ => "Dengeli",
        };
    }

    private static string PromiseLabel(IReadOnlyList<Promise> promises)
    {
        if (promises.Count == 0)
        {
            return "aktif söz yok";
        }

        return string.Join(
            " · ",
            promises.Select(promise =>
                $"{(promise.Kind == PromiseKind.StartingOpportunity ? "İlk 11" : "Oyun süresi")} "
                + $"{promise.StartsGiven}/{promise.TargetStarts}, son {promise.DeadlineOn.ToDisplayDateString()}"));
    }

    private static string CausalityLabel(
        RelationshipRecord? relationship,
        IReadOnlyList<Promise> promises,
        IReadOnlyList<MemoryRecord> memories)
    {
        var parts = new List<string>();

        var terminal = promises
            .Where(p => p.Status is PromiseStatus.Fulfilled or PromiseStatus.Broken)
            .OrderByDescending(p => p.TerminalOn?.DayNumber ?? 0)
            .ThenByDescending(p => p.PromiseId.Value)
            .FirstOrDefault();
        if (terminal is not null)
        {
            var kind = terminal.Kind == PromiseKind.StartingOpportunity ? "İlk 11" : "Oyun süresi";
            var status = terminal.Status == PromiseStatus.Fulfilled ? "tutuldu" : "bozuldu";
            parts.Add($"{kind} sözü {status} (#{terminal.PromiseId.Value})");
        }

        var memory = memories.FirstOrDefault();
        if (memory is not null)
        {
            var category = memory.Category == MemoryCategory.Trust ? "Güven hafızası" : "Söz hafızası";
            var valence = memory.Valence switch
            {
                MemoryValence.Positive => "olumlu",
                MemoryValence.Negative => "olumsuz",
                _ => "nötr",
            };
            parts.Add($"{category} {valence} (etki {memory.CurrentInfluence})");
        }

        if (relationship?.LastChangeReasonCode is { Length: > 0 } reason)
        {
            parts.Add(ExplainReason(reason));
        }

        return parts.Count == 0 ? "henüz izlenebilir neden yok" : string.Join(" · ", parts);
    }

    private static string ExplainReason(string reasonCode) => reasonCode switch
    {
        "CreatedNeutral" => "ilişki: yeni kayıt (nötr başlangıç)",
        "PromiseBrokenTrust" => "ilişki: söz bozulması güveni düşürdü",
        "PromiseFulfilledTrust" => "ilişki: söz tutulması güveni yükseltti",
        "DecisionPlayingTimeGranted" => "ilişki: forma sözü verildi",
        "DecisionPlayingTimeRefused" => "ilişki: forma talebi reddedildi",
        "DecisionPlayingTimeExpired" => "ilişki: forma talebi zaman aşımına uğradı",
        "DecisionTransferAcknowledged" => "ilişki: transfer talebi kabul edildi",
        "DecisionTransferRefused" => "ilişki: transfer talebi reddedildi",
        "SelectionStartedRespect" => "ilişki: XI seçimi saygıyı yükseltti",
        "SelectionOmittedCompatibility" => "ilişki: kadro dışı bırakma uyumu düşürdü",
        _ => $"ilişki: {reasonCode}",
    };
}
