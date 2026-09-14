namespace FitnessChallenge.Domain;

public static class RankGap
{
    public const int TieBreakPoint = 1;

    public static int ToOvertake(int theirPoints, int myPoints) => theirPoints - myPoints + TieBreakPoint;
}
