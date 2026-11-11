using FootballCareerSimulator.Application.Competition.Queries;
using FootballCareerSimulator.Application.TeamPreparation.Queries;

namespace FootballCareerSimulator.Application.CareerHub.Queries;

/// <summary>
/// Sakin haftada staff fısıltısı — genel geçer not yerine takımın gerçek durumundan beslenir.
/// Sıradaki maç, sakatlık listesi ve lig durumu elverdiği ölçüde tek satıra yatar.
/// </summary>
public static class StaffWhisper
{
    public static string? Compose(
        PreMatchBriefing match,
        PreparationBriefing prep,
        LeagueWorldBriefing league,
        string moodCode,
        int dayNumber)
    {
        ArgumentNullException.ThrowIfNull(match);
        ArgumentNullException.ThrowIfNull(prep);
        ArgumentNullException.ThrowIfNull(league);

        if (!string.Equals(moodCode, WeekMoodDigest.MoodCalm, StringComparison.Ordinal)
            && !string.Equals(moodCode, WeekMoodDigest.MoodCalmMatch, StringComparison.Ordinal))
        {
            return null;
        }

        if (match.HasMatch)
        {
            return prep.HasInjuryPressure
                ? $"Not: Staff — {match.FixtureLine}; {prep.InjuredNames[0]} sahada yok, rotasyona hazır ol."
                : $"Not: Staff — {match.FixtureLine}; tempo yerinde, kadro seni bekliyor.";
        }

        if (prep.HasInjuryPressure)
        {
            var names = string.Join(", ", prep.InjuredNames.Take(2));
            return $"Not: Staff — {prep.InjuredNames.Count} oyuncu sahalarda yok ({names}).";
        }

        if (league.HasSeason && !string.IsNullOrWhiteSpace(league.Headline))
        {
            return $"Not: Staff — {league.Headline}";
        }

        return OfficeCalmNote.ToBeatLine(moodCode, dayNumber);
    }
}
