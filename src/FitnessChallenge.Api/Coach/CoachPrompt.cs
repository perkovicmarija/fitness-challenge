using System.Globalization;
using System.Text;
using FitnessChallenge.Api.DTO;
using FitnessChallenge.Domain;

namespace FitnessChallenge.Api.Coach;

public static class CoachPrompt
{

    public enum Ask
    {

        Advice,

        RankChange,

        WeeklyRecap,
    }

    public static string Build(CoachFactsResponse facts, Ask ask = Ask.Advice)
    {
        var briefing = new StringBuilder();

        briefing.AppendLine("You are the coach inside a workplace fitness challenge. You advise one competitor.");
        briefing.AppendLine();

        briefing.AppendLine("How the challenge scores activities:");

        foreach (var rule in ActivityRules.All)
        {
            briefing.AppendLine(
                $"- {SportWire.Label(rule.Sport)}: {Amount(rule.PointsPerUnit)} points per {UnitOf(rule.Metric)}");
        }

        briefing.AppendLine();

        if (facts.Challenge is { } challenge)
        {
            briefing.AppendLine(challenge.HasEnded
                ? $"The challenge \"{challenge.Name}\" is over. Nothing more can be earned in it."
                : $"The challenge \"{challenge.Name}\" is on day {challenge.Day} of {challenge.TotalDays}. "
                  + $"{DaysLeft(challenge.DaysLeft)} to earn points in.");

            briefing.AppendLine();
        }

        AppendFacts(briefing, facts);

        briefing.AppendLine(ask switch
        {
            Ask.RankChange =>
                "Explain why their rank changed, in at most 45 words. Name what they earned and what the "
                + "other competitor earned over the same 7 days, then the gap as it stands now.",
            Ask.WeeklyRecap =>
                "Summarise their last 7 days in at most 45 words: how the week compares to their usual, "
                + "which sport carried it, how consistent they were, and where they stand now.",
            _ => "How to reply:",
        });

        foreach (var rule in RulesFor(ask))
        {
            briefing.AppendLine($"- {rule}");
        }

        return briefing.ToString();
    }

    private static void AppendFacts(StringBuilder briefing, CoachFactsResponse facts)
    {
        briefing.AppendLine("Facts about this competitor, computed by the app:");

        briefing.AppendLine(facts.Rank is null
            ? $"- Unranked: they have logged nothing yet. {facts.Competitors} people are competing."
            : $"- Rank {facts.Rank} of {facts.Competitors}, with {Points(facts.TotalPoints)} points.");

        briefing.AppendLine(facts.PointsToPassNextRank is null
            ? "- They lead the challenge. The goal is holding the lead, not closing a gap."
            : $"- {Points(facts.PointsToPassNextRank.Value)} points would move them up one place.");

        if (facts.LeadOverNextRank is not null)
        {
            briefing.AppendLine($"- They are {Points(facts.LeadOverNextRank.Value)} points clear of second place.");
        }

        if (facts.PointsToTakeTheLead is not null)
        {
            briefing.AppendLine($"- {Points(facts.PointsToTakeTheLead.Value)} points would take them into first place.");
        }

        briefing.AppendLine(
            $"- Last 7 days: {facts.ActivitiesLastSevenDays} activities, {Points(facts.PointsLastSevenDays)} points. "
            + $"Their usual week is {Points(facts.AverageWeeklyPoints)} points.");

        briefing.AppendLine(
            $"- Trained on {facts.DaysTrainedLastFourWeeks} of the last 28 days, and "
            + $"{facts.ActiveDaysLastSevenDays} of the last 7.");

        if (facts.BestDay is { } best)
        {
            briefing.AppendLine($"- Their best single day in the last four weeks: {Points(best.Points)} points on {best.Date:D}.");
        }

        if (facts.Outlook is { } outlook)
        {
            briefing.AppendLine(outlook switch
            {
                { WithinReach: true } =>
                    $"- At their usual pace they would earn about {Points(outlook.PointsAtCurrentPace)} points in the "
                    + $"{outlook.DaysLeft} days left, which is more than the gap. The gap is within reach.",
                { Unlikely: false } =>
                    $"- At their usual pace they would earn about {Points(outlook.PointsAtCurrentPace)} points in the "
                    + $"{outlook.DaysLeft} days left, which is less than the gap. Closing it needs more than their "
                    + "usual effort, but it is not out of range.",
                _ =>
                    $"- At their usual pace they would earn about {Points(outlook.PointsAtCurrentPace)} points in the "
                    + $"{outlook.DaysLeft} days left. The gap is several times that, so closing it before the "
                    + "challenge ends is unlikely. Say so plainly and give them a goal they can actually reach.",
            });
        }

        if (facts.RankChange is { } change)
        {
            briefing.AppendLine(
                $"- They moved from rank {change.PreviousRank} to rank {change.CurrentRank} over the last "
                + $"7 days, earning {Points(change.MyPointsThisWeek)} points in that time.");

            if (change.MoverPointsThisWeek is { } moverPoints)
            {
                briefing.AppendLine(
                    $"- The competitor they swapped places with earned {Points(moverPoints)} points over the "
                    + "same 7 days. That difference is what moved the ranking.");
            }
        }

        if (facts.BestWeekday is not null)
        {
            briefing.AppendLine($"- They earn more points on {facts.BestWeekday}s than on any other day.");
        }

        briefing.AppendLine("- By sport, all time:");

        foreach (var sport in facts.Sports)
        {
            briefing.AppendLine(sport.Sessions == 0
                ? $"  - {sport.Label}: never done."
                : $"  - {sport.Label}: {Points(sport.Points)} points over {sport.Sessions} sessions, "
                  + $"averaging {Points(sport.AveragePoints)} a session.");
        }

        if (facts.WaysToPassNextRank.Count > 0)
        {
            briefing.AppendLine(
                "- Any one of these moves them up one place: "
                + string.Join(", ", facts.WaysToPassNextRank.Select(way => $"{way.Label} {Amount(way.Quantity)} {way.Unit}")) + ".");
        }

        if (facts.Recommended is { } recommended)
        {
            briefing.AppendLine(
                $"- The screen already recommends one of those, chosen because it is the sport they do most: "
                + $"{recommended.Label} {Amount(recommended.Quantity)} {recommended.Unit}.");
        }

        briefing.AppendLine();
    }

    private static IEnumerable<string> RulesFor(Ask ask)
    {
        yield return "Do not present a gap as easy when the figures say otherwise. Where the briefing "
            + "says the gap is unlikely to close, say that plainly and offer a goal that is reachable — "
            + "holding position, a daily total, more active days — rather than a quantity nobody will do.";

        yield return "Never say what will happen. You were not told what anybody else will log, so no "
            + "reply may contain \"will\", \"likely\", \"should hold\" or \"you'll\" about their "
            + "position. Describe what an effort is worth, never what it achieves.";

        yield return "Use only the figures above. Comparing two of them is fine; working out a new "
            + "number is not.";

        yield return "Quote every figure exactly as written, decimals included. Rounding 5.08 km to 5 km "
            + "makes the advice fall short of the gap, which is the one thing these figures exist to prevent.";

        yield return "Other competitors are identified by rank. You were not told their names, so never "
            + "use one.";

        yield return "State the figures plainly. No exclamation marks, and no encouragement standing in "
            + "for a fact.";

        yield return "You advise on a competition, not on wellbeing. Talk about position, margin, what "
            + "the days left allow, and which of their sports returns the most for the time it takes. "
            + "Leave out general fitness advice such as starting small or building gradually.";

        if (ask != Ask.Advice)
        {
            yield return "No advice, and no question back. This is one observation about figures already "
                + "on their screen.";
            yield break;
        }

        yield return "Answer what was actually asked. The screen above your reply already states their "
            + "strongest sport, the gap, and the quantity that closes it, so opening with any of those "
            + "wastes the reply on something they have already read.";

        yield return "Earn the reply with something the screen cannot say: how their answer changes what "
            + "you would suggest, what they never touch, whether this week is unusual for them, or which "
            + "of their sports pays best for the time it takes.";

        yield return "If what they say rules out the sport the screen recommends, switch to the next best "
            + "one they actually do and say why it is the next best.";

        yield return "The screen already lists every way to move up, so do not repeat the list. Name at "
            + "most one, and only where it makes the suggestion concrete.";

        yield return "Where days remain, let that shape the advice: what is realistic in the time left, "
            + "not in general. Do not invent a per-day target.";

        yield return "Warm, specific and direct. At most 110 words, plain sentences, no headings and no "
            + "markdown.";

        yield return "Suggest gradual, realistic progression. Give no medical advice: if asked about pain, "
            + "injury or illness, say to speak to a professional.";
    }

    private static string DaysLeft(int days) =>
        days switch
        {
            0 => "Today is the final day",
            1 => "One day is left",
            _ => $"{days} days are left",
        };

    private static string UnitOf(MetricKind metric) =>
        metric switch
        {
            MetricKind.Distance => "kilometre",
            MetricKind.Duration => "whole minute",
            _ => "step",
        };

    private static string Points(int value) => value.ToString("#,##0", CultureInfo.InvariantCulture);

    private static string Amount(decimal value) => value.ToString("#,##0.####", CultureInfo.InvariantCulture);
}
