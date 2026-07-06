namespace FootballCareerSimulator.Domain.Competition;

using FootballCareerSimulator.Domain.WorldCalendar;

/// <summary>
/// Tek lig competition kapsayıcısı; sezonlar arası geçiş invariant'larını uygular (docs/03_DOMAIN_MODEL.md Bölüm 12.3).
/// </summary>
public sealed class LeagueCompetition
{
    private readonly List<CompetitionSeason> _seasons = new();

    public LeagueCompetition(CompetitionId competitionId)
    {
        CompetitionId = competitionId;
    }

    public CompetitionId CompetitionId { get; }

    public IReadOnlyList<CompetitionSeason> Seasons => _seasons;

    public CompetitionSeason? CurrentSeason =>
        _seasons.LastOrDefault(season => season.Status is not SeasonStatus.Archived);

    public CompetitionSeason CreateSeason(SeasonId seasonId, GameDate preseasonStartDate)
    {
        if (CurrentSeason is { Status: SeasonStatus.Preseason or SeasonStatus.Active })
        {
            throw new CompetitionInvariantViolationException(
                "A new season cannot be created while another season is in preseason or active.");
        }

        if (CurrentSeason is { Status: SeasonStatus.Completed })
        {
            throw new CompetitionInvariantViolationException(
                "The completed season must be archived before a new season can be created.");
        }

        var season = CompetitionSeason.Create(CompetitionId, seasonId, preseasonStartDate);
        _seasons.Add(season);
        return season;
    }

    public void StartSeason(SeasonId seasonId, GameDate occurredAt)
    {
        var season = GetSeasonOrThrow(seasonId);

        if (CurrentSeason is not null && !ReferenceEquals(CurrentSeason, season))
        {
            throw new CompetitionInvariantViolationException(
                "Only the current non-archived season can be started.");
        }

        var previousSeason = _seasons
            .Where(existing => existing.SeasonId != seasonId)
            .OrderByDescending(existing => existing.SeasonId.Value)
            .FirstOrDefault();

        if (previousSeason is not null &&
            previousSeason.Status is not (SeasonStatus.Completed or SeasonStatus.Archived))
        {
            throw new CompetitionInvariantViolationException(
                "A season cannot become active before the previous season is completed.");
        }

        season.StartActiveSeason(occurredAt);
    }

    public void CompleteSeason(SeasonId seasonId, GameDate occurredAt)
    {
        GetSeasonOrThrow(seasonId).CompleteSeason(occurredAt);
    }

    public void ArchiveSeason(SeasonId seasonId, GameDate occurredAt)
    {
        GetSeasonOrThrow(seasonId).ArchiveSeason(occurredAt);
    }

    public static LeagueCompetition Rehydrate(
        CompetitionId competitionId,
        IEnumerable<CompetitionSeason> seasons)
    {
        var league = new LeagueCompetition(competitionId);
        league._seasons.AddRange(seasons);
        return league;
    }

    private CompetitionSeason GetSeasonOrThrow(SeasonId seasonId) =>
        _seasons.FirstOrDefault(season => season.SeasonId == seasonId)
        ?? throw new CompetitionInvariantViolationException($"Season {seasonId.Value} was not found.");
}
