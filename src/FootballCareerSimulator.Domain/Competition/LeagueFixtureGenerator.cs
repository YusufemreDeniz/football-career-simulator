namespace FootballCareerSimulator.Domain.Competition;

using FootballCareerSimulator.Domain.Shared;
using FootballCareerSimulator.Domain.WorldCalendar;

/// <summary>
/// Çift devreli lig fikstürü üretir (docs/02_MVP_SCOPE.md Bölüm 17.2 — 20 takım, 38 hafta).
/// </summary>
public static class LeagueFixtureGenerator
{
    public static IReadOnlyList<Fixture> GenerateDoubleRoundRobin(
        CompetitionId competitionId,
        SeasonId seasonId,
        IReadOnlyList<ClubId> participants,
        GameDate firstMatchdayDate,
        int daysBetweenRounds,
        FixtureId startingFixtureId)
    {
        ArgumentNullException.ThrowIfNull(participants);

        if (participants.Count != CompetitionMvpConstraints.LeagueTeamCount)
        {
            throw new CompetitionInvariantViolationException(
                $"League fixture generation requires exactly {CompetitionMvpConstraints.LeagueTeamCount} participants.");
        }

        if (daysBetweenRounds < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(daysBetweenRounds),
                daysBetweenRounds,
                "Days between rounds must be at least 1.");
        }

        if (participants.Distinct().Count() != participants.Count)
        {
            throw new CompetitionInvariantViolationException(
                "Fixture generation requires distinct participants.");
        }

        var orderedClubs = participants.OrderBy(club => club.Value).ToArray();
        var firstLegRounds = GenerateSingleRoundRobinRounds(orderedClubs);
        var fixtures = new List<Fixture>(CompetitionMvpConstraints.LeagueTeamCount * (CompetitionMvpConstraints.LeagueTeamCount - 1));
        var nextFixtureId = startingFixtureId.Value;

        AppendLegFixtures(
            fixtures,
            competitionId,
            seasonId,
            firstLegRounds,
            firstMatchdayDate,
            daysBetweenRounds,
            startingRound: 1,
            swapHomeAway: false,
            ref nextFixtureId);

        var secondLegStartDate = firstMatchdayDate.AddDays(
            CompetitionMvpConstraints.SingleLegRoundCount * daysBetweenRounds);

        AppendLegFixtures(
            fixtures,
            competitionId,
            seasonId,
            firstLegRounds,
            secondLegStartDate,
            daysBetweenRounds,
            startingRound: CompetitionMvpConstraints.SingleLegRoundCount + 1,
            swapHomeAway: true,
            ref nextFixtureId);

        return fixtures;
    }

    private static List<IReadOnlyList<(ClubId Home, ClubId Away)>> GenerateSingleRoundRobinRounds(ClubId[] clubs)
    {
        var rotating = clubs.ToList();
        var rounds = new List<IReadOnlyList<(ClubId Home, ClubId Away)>>(CompetitionMvpConstraints.SingleLegRoundCount);

        for (var roundIndex = 0; roundIndex < clubs.Length - 1; roundIndex++)
        {
            var pairings = new List<(ClubId Home, ClubId Away)>(clubs.Length / 2);

            for (var pairingIndex = 0; pairingIndex < clubs.Length / 2; pairingIndex++)
            {
                var home = rotating[pairingIndex];
                var away = rotating[rotating.Count - 1 - pairingIndex];
                pairings.Add((home, away));
            }

            rounds.Add(pairings);
            rotating = [rotating[0], rotating[^1], .. rotating.Skip(1).Take(rotating.Count - 2)];
        }

        return rounds;
    }

    private static void AppendLegFixtures(
        List<Fixture> fixtures,
        CompetitionId competitionId,
        SeasonId seasonId,
        IReadOnlyList<IReadOnlyList<(ClubId Home, ClubId Away)>> rounds,
        GameDate legStartDate,
        int daysBetweenRounds,
        int startingRound,
        bool swapHomeAway,
        ref long nextFixtureId)
    {
        for (var roundOffset = 0; roundOffset < rounds.Count; roundOffset++)
        {
            var round = new FixtureRound(startingRound + roundOffset);
            var scheduledDate = legStartDate.AddDays(roundOffset * daysBetweenRounds);

            foreach (var pairing in rounds[roundOffset])
            {
                var home = swapHomeAway ? pairing.Away : pairing.Home;
                var away = swapHomeAway ? pairing.Home : pairing.Away;

                fixtures.Add(
                    new Fixture(
                        new FixtureId(nextFixtureId++),
                        competitionId,
                        seasonId,
                        home,
                        away,
                        round,
                        scheduledDate,
                        FixtureStatus.Planned));
            }
        }
    }
}
