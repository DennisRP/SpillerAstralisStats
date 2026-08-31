using SpillerAstralisStats.Application;

namespace SpillerAstralisStats.Tests;

public sealed class MatchBriefingValidatorTests
{
    [Fact]
    public void Accepts_a_valid_briefing()
    {
        var briefing = ValidBriefing();

        var exception = Record.Exception(() => MatchBriefingValidator.Validate(briefing));

        Assert.Null(exception);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\r\n")]
    public void Rejects_a_blank_headline(string headline)
    {
        var exception = Assert.Throws<MatchBriefingValidationException>(
            () => MatchBriefingValidator.Validate(ValidBriefing() with { Headline = headline }));

        Assert.Contains("Headline", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Rejects_a_blank_summary()
    {
        var exception = Assert.Throws<MatchBriefingValidationException>(
            () => MatchBriefingValidator.Validate(ValidBriefing() with { Summary = "  " }));

        Assert.Contains("Summary", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Rejects_overlong_headline_summary_and_key_point()
    {
        Assert.Throws<MatchBriefingValidationException>(() => MatchBriefingValidator.Validate(
            ValidBriefing() with { Headline = new string('h', MatchBriefingContract.MaxHeadlineLength + 1) }));
        Assert.Throws<MatchBriefingValidationException>(() => MatchBriefingValidator.Validate(
            ValidBriefing() with { Summary = new string('s', MatchBriefingContract.MaxSummaryLength + 1) }));
        Assert.Throws<MatchBriefingValidationException>(() => MatchBriefingValidator.Validate(
            ValidBriefing() with { KeyPoints = [new string('k', MatchBriefingContract.MaxKeyPointLength + 1)] }));
    }

    [Fact]
    public void Rejects_a_blank_key_point()
    {
        var exception = Assert.Throws<MatchBriefingValidationException>(
            () => MatchBriefingValidator.Validate(ValidBriefing() with { KeyPoints = ["Valid", " "] }));

        Assert.Contains("KeyPoints[1]", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    public void Rejects_an_invalid_key_point_count(int count)
    {
        var keyPoints = Enumerable.Range(0, count).Select(index => $"Point {index}").ToArray();

        var exception = Assert.Throws<MatchBriefingValidationException>(
            () => MatchBriefingValidator.Validate(ValidBriefing() with { KeyPoints = keyPoints }));

        Assert.Contains("between 1 and 4", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Rejects_null_key_points_from_deserialized_input()
    {
        var briefing = ValidBriefing() with { KeyPoints = null! };

        var exception = Assert.Throws<MatchBriefingValidationException>(
            () => MatchBriefingValidator.Validate(briefing));

        Assert.Contains("required", exception.Message, StringComparison.Ordinal);
    }

    private static MatchBriefing ValidBriefing() => new(
        "Astralis møder G2 igen",
        "Astralis har vundet tre af de fem seneste kampe.",
        ["G2 vandt det seneste indbyrdes opgør."]);
}
