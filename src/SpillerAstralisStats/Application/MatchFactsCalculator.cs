using SpillerAstralisStats.Domain;

namespace SpillerAstralisStats.Application;

internal static class MatchFactsCalculator
{
    internal const long AstralisTeamId = 3209;

    public static MatchFactsSnapshot Calculate(
        IReadOnlyList<MatchRecord> source,
        MatchFactsRequest request)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();

        var normalized = Normalize(source);
        var recentMatches = normalized.Take(request.RecentMatchCount).ToArray();
        var recentForm = CreateRecentForm(recentMatches);
        var headToHeadMatches = normalized
            .Where(match => match.OpponentTeamId == request.OpponentTeamId)
            .ToArray();

        return new MatchFactsSnapshot(
            recentMatches,
            recentForm,
            CreateHeadToHead(request.OpponentTeamId, headToHeadMatches));
    }

    private static IReadOnlyList<MatchFact> Normalize(IReadOnlyList<MatchRecord> source) =>
        source
            .Where(IsEligible)
            .GroupBy(match => match.ProviderId)
            .Select(group => group.First())
            .Select(CreateMatchFact)
            .OrderByDescending(match => match.RelevantAt)
            .ThenByDescending(match => match.ProviderId)
            .ToArray();

    private static bool IsEligible(MatchRecord match)
    {
        var hasAstralis = match.Opponents.Any(opponent => opponent.TeamId == AstralisTeamId);
        var hasOtherOpponent = match.Opponents.Any(opponent => opponent.TeamId is > 0 and not AstralisTeamId);
        return match.ProviderId > 0
            && string.Equals(match.Status, "finished", StringComparison.OrdinalIgnoreCase)
            && (match.BeginAt ?? match.ScheduledAt) is not null
            && hasAstralis
            && hasOtherOpponent;
    }

    private static MatchFact CreateMatchFact(MatchRecord match)
    {
        var relevantAt = match.BeginAt ?? match.ScheduledAt
            ?? throw new InvalidOperationException("An eligible match must have a relevant timestamp.");
        var opponent = match.Opponents.First(opponent => opponent.TeamId is > 0 and not AstralisTeamId);
        var outcome = ClassifyOutcome(match, opponent.TeamId);
        var scores = GetScores(match, opponent.TeamId);

        return new MatchFact(
            match.ProviderId,
            relevantAt,
            opponent.TeamId!.Value,
            opponent.Name,
            outcome,
            scores.AstralisScore,
            scores.OpponentScore,
            match.NumberOfGames,
            match.League,
            match.Serie,
            match.Tournament,
            GetSweep(outcome, scores.AstralisScore, scores.OpponentScore));
    }

    private static MatchOutcome ClassifyOutcome(MatchRecord match, long? opponentTeamId) =>
        match.WinnerTeamId switch
        {
            AstralisTeamId => MatchOutcome.Win,
            var winner when winner == opponentTeamId => MatchOutcome.Loss,
            _ => MatchOutcome.Unknown
        };

    private static (int? AstralisScore, int? OpponentScore) GetScores(MatchRecord match, long? opponentTeamId)
    {
        var astralisScores = match.Scores.Where(score => score.TeamId == AstralisTeamId).Select(score => score.Score).ToArray();
        var opponentScores = match.Scores.Where(score => score.TeamId == opponentTeamId).Select(score => score.Score).ToArray();

        return (
            astralisScores.Length == 1 ? astralisScores[0] : null,
            opponentScores.Length == 1 ? opponentScores[0] : null);
    }

    private static bool? GetSweep(MatchOutcome outcome, int? astralisScore, int? opponentScore)
    {
        if (outcome is MatchOutcome.Unknown || astralisScore is null || opponentScore is null)
        {
            return null;
        }

        return outcome == MatchOutcome.Win
            ? astralisScore > 0 && opponentScore == 0
            : opponentScore > 0 && astralisScore == 0;
    }

    private static RecentFormFacts CreateRecentForm(IReadOnlyList<MatchFact> matches)
    {
        var outcomes = matches.Select(match => match.Outcome).ToArray();
        var wins = outcomes.Count(outcome => outcome == MatchOutcome.Win);
        var losses = outcomes.Count(outcome => outcome == MatchOutcome.Loss);
        var unknown = outcomes.Length - wins - losses;
        var decided = wins + losses;
        decimal? percentage = decided == 0
            ? null
            : Math.Round((decimal)wins / decided * 100, 1, MidpointRounding.AwayFromZero);

        return new RecentFormFacts(outcomes, wins, losses, unknown, percentage);
    }

    private static HeadToHeadFacts CreateHeadToHead(long opponentTeamId, IReadOnlyList<MatchFact> matches)
    {
        var wins = matches.Count(match => match.Outcome == MatchOutcome.Win);
        var losses = matches.Count(match => match.Outcome == MatchOutcome.Loss);

        return new HeadToHeadFacts(
            opponentTeamId,
            matches.FirstOrDefault()?.OpponentName,
            matches.Count,
            wins,
            losses,
            matches.Count - wins - losses,
            matches,
            matches.FirstOrDefault());
    }
}
