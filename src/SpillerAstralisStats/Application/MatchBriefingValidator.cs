namespace SpillerAstralisStats.Application;

internal static class MatchBriefingValidator
{
    public static void Validate(MatchBriefing briefing)
    {
        ArgumentNullException.ThrowIfNull(briefing);

        ValidateText(briefing.Headline, nameof(briefing.Headline), MatchBriefingContract.MaxHeadlineLength);
        ValidateText(briefing.Summary, nameof(briefing.Summary), MatchBriefingContract.MaxSummaryLength);

        if (briefing.KeyPoints is null)
        {
            throw new MatchBriefingValidationException("KeyPoints is required.");
        }

        if (briefing.KeyPoints.Count is < MatchBriefingContract.MinKeyPoints or > MatchBriefingContract.MaxKeyPoints)
        {
            throw new MatchBriefingValidationException(
                $"KeyPoints must contain between {MatchBriefingContract.MinKeyPoints} and {MatchBriefingContract.MaxKeyPoints} items.");
        }

        for (var index = 0; index < briefing.KeyPoints.Count; index++)
        {
            ValidateText(
                briefing.KeyPoints[index],
                $"KeyPoints[{index}]",
                MatchBriefingContract.MaxKeyPointLength);
        }
    }

    private static void ValidateText(string? value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new MatchBriefingValidationException($"{fieldName} is required and cannot be blank.");
        }

        if (value.Length > maxLength)
        {
            throw new MatchBriefingValidationException($"{fieldName} cannot exceed {maxLength} characters.");
        }
    }
}

public sealed class MatchBriefingValidationException(string message) : Exception(message);
