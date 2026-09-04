namespace FootballCareerSimulator.Application.CareerHub.Queries;

using FootballCareerSimulator.Domain.WorldCalendar;

public sealed record CareerEmploymentLegacySource(
    string ClubName,
    int StartedDayNumber,
    int? EndedDayNumber,
    string? EndReason,
    int BoardConfidence);

public sealed record CareerEmploymentLegacyLine(
    string ClubName,
    string Period,
    string Outcome)
{
    public string ToDisplayText() => $"Görev · {ClubName} · {Period}\n{Outcome}";
}

public sealed record CareerSeasonLegacySource(
    long SeasonId,
    string Status,
    int Rank,
    int TeamCount,
    int Played,
    int Won,
    int Drawn,
    int Lost,
    int Points,
    int GoalsFor,
    int GoalsAgainst);

public sealed record CareerSeasonLegacyLine(
    long SeasonId,
    string Status,
    string Finish,
    string Record,
    string GoalBalance)
{
    public string ToDisplayText() =>
        $"Sezon {SeasonId} · {Status} · {Finish}\n{Record} · {GoalBalance}";
}

public sealed record CareerLegacyDigest(
    bool HasCareer,
    string Headline,
    string RecordLine,
    string DevelopmentLine,
    string NextMilestoneLine,
    IReadOnlyList<CareerSeasonLegacyLine> Seasons,
    IReadOnlyList<CareerEmploymentLegacyLine> Employments)
{
    public static CareerLegacyDigest Empty() =>
        new(false, "Kariyer mirası: kulüp görevi yok.", "Maç kaydı yok.", "Gelişim kaydı yok.", "Sıradaki hedef: kulüp görevi bul.", Array.Empty<CareerSeasonLegacyLine>(), Array.Empty<CareerEmploymentLegacyLine>());

    public static CareerLegacyDigest WithoutActiveEmployment(
        string managerName,
        int reputation,
        IReadOnlyList<CareerEmploymentLegacySource> employments)
    {
        var lines = ComposeEmployments(employments);
        return new CareerLegacyDigest(
            lines.Count > 0,
            $"{managerName} · işsiz · itibar {reputation}",
            $"Kariyer: {lines.Count} tamamlanmış kulüp görevi.",
            "Kadro mirası yeni görevle devam edecek.",
            "Sıradaki hedef: geçerli bir iş teklifini değerlendir.",
            Array.Empty<CareerSeasonLegacyLine>(),
            lines);
    }

    public static CareerLegacyDigest Compose(
        string managerName,
        string clubName,
        int tenureDays,
        int reputation,
        int boardConfidence,
        int developedPlayerCount,
        int averageSquadAge,
        int expiringContractCount,
        IReadOnlyList<CareerSeasonLegacySource> seasons,
        IReadOnlyList<CareerEmploymentLegacySource>? employments = null)
    {
        ArgumentNullException.ThrowIfNull(seasons);
        var played = seasons.Sum(season => season.Played);
        var won = seasons.Sum(season => season.Won);
        var drawn = seasons.Sum(season => season.Drawn);
        var lost = seasons.Sum(season => season.Lost);
        var completed = seasons.Count(season =>
            season.Status is "Completed" or "Archived");
        var bestRank = seasons.Where(season => season.Rank > 0).Select(season => season.Rank).DefaultIfEmpty(0).Min();
        var winRate = played == 0 ? 0 : (int)Math.Round(won * 100d / played);
        var lines = seasons
            .OrderByDescending(season => season.SeasonId)
            .Select(season => new CareerSeasonLegacyLine(
                season.SeasonId,
                StatusLabel(season.Status),
                season.Rank > 0 ? $"{season.Rank}/{season.TeamCount}. sıra · {season.Points} puan" : "Derece bekleniyor",
                $"{season.Played} maç · {season.Won}G {season.Drawn}B {season.Lost}M",
                $"Gol {season.GoalsFor}-{season.GoalsAgainst} ({FormatSigned(season.GoalsFor - season.GoalsAgainst)})"))
            .ToArray();

        return new CareerLegacyDigest(
            true,
            $"{managerName} · {clubName} · görev günü {tenureDays} · itibar {reputation} · yönetim {boardConfidence}",
            $"Kariyer: {seasons.Count} sezon ({completed} tamamlandı) · {played} maç"
            + $" · {won}G {drawn}B {lost}M · galibiyet %{winRate}"
            + (bestRank > 0 ? $" · en iyi derece {bestRank}." : string.Empty),
            $"Kadro mirası: {developedPlayerCount} gelişim alan oyuncu · ortalama yaş {averageSquadAge}"
            + $" · 1 yıl içinde bitecek sözleşme {expiringContractCount}",
            NextMilestone(played, won, completed, bestRank),
            lines,
            ComposeEmployments(employments ?? Array.Empty<CareerEmploymentLegacySource>()));
    }

    private static IReadOnlyList<CareerEmploymentLegacyLine> ComposeEmployments(
        IReadOnlyList<CareerEmploymentLegacySource> employments) =>
        employments
            .OrderByDescending(employment => employment.StartedDayNumber)
            .Select(employment => new CareerEmploymentLegacyLine(
                employment.ClubName,
                employment.EndedDayNumber is int ended
                    ? $"{GameDate.ToDisplayDateString(employment.StartedDayNumber)}–{GameDate.ToDisplayDateString(ended)}"
                    : $"{GameDate.ToDisplayDateString(employment.StartedDayNumber)}–devam",
                employment.EndedDayNumber is null
                    ? $"Aktif görev · yönetim {employment.BoardConfidence}"
                    : $"{EndReasonLabel(employment.EndReason)} · kapanış yönetim {employment.BoardConfidence}"))
            .ToArray();

    private static string EndReasonLabel(string? reason) => reason switch
    {
        "Dismissed" => "Görevden alındı",
        "Resigned" => "Ayrıldı",
        _ => "Görev tamamlandı",
    };

    private static string NextMilestone(int played, int won, int completed, int bestRank)
    {
        if (played < 50)
        {
            return $"Sıradaki kariyer eşiği: 50 maç ({50 - played} kaldı).";
        }

        if (won < 50)
        {
            return $"Sıradaki kariyer eşiği: 50 galibiyet ({50 - won} kaldı).";
        }

        if (completed < 3)
        {
            return $"Sıradaki kariyer eşiği: 3 tamamlanmış sezon ({3 - completed} kaldı).";
        }

        return bestRank == 1
            ? "Kariyer eşiği: şampiyonluğu yeni kulüp ve genç çekirdekle tekrarla."
            : "Sıradaki kariyer eşiği: lig şampiyonluğu.";
    }

    private static string StatusLabel(string status) => status switch
    {
        "Preseason" => "Sezon öncesi",
        "Active" => "Devam ediyor",
        "Completed" => "Tamamlandı",
        "Archived" => "Arşiv",
        _ => status,
    };

    private static string FormatSigned(int value) => value > 0 ? $"+{value}" : value.ToString();
}
