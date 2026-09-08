namespace FitnessChallenge.Domain;

/// <summary>
/// The activity types the challenge scores.
/// </summary>
/// <remarks>
/// <see cref="DailySteps"/> is submitted with the <c>sport</c> field absent, as the assignment
/// specifies. That translation belongs to the API; the domain never models "no type".
/// </remarks>
public enum Sport
{
    Running,
    Walking,
    Cycling,
    Swimming,
    Gym,
    DailySteps,
}
