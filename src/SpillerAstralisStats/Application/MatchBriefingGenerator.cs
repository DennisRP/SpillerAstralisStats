using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpillerAstralisStats.Application;

internal sealed record MatchBriefingModelRequest(
    string Instructions,
    string InputJson,
    string SchemaName,
    string JsonSchema);

internal sealed record MatchBriefingModelResponse(
    string? OutputText,
    string? Refusal);

internal interface IMatchBriefingClient
{
    Task<MatchBriefingModelResponse> GenerateAsync(
        MatchBriefingModelRequest request,
        CancellationToken cancellationToken);
}

internal sealed class MatchBriefingGenerator(IMatchBriefingClient client)
{
    private static readonly JsonSerializerOptions InputJsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly JsonSerializerOptions OutputJsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public async Task<MatchBriefingResult> GenerateAsync(
        GroundedMatchBriefingContext grounding,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(grounding);

        var request = new MatchBriefingModelRequest(
            MatchBriefingContract.DeveloperInstruction,
            JsonSerializer.Serialize(grounding, InputJsonOptions),
            MatchBriefingContract.SchemaName,
            MatchBriefingContract.JsonSchema);
        var response = await client.GenerateAsync(request, cancellationToken);

        if (!string.IsNullOrWhiteSpace(response.Refusal))
        {
            throw new MatchBriefingRefusalException(response.Refusal);
        }

        if (string.IsNullOrWhiteSpace(response.OutputText))
        {
            throw new MatchBriefingOutputException("The model returned no briefing output.");
        }

        MatchBriefing briefing;
        try
        {
            briefing = JsonSerializer.Deserialize<MatchBriefing>(response.OutputText, OutputJsonOptions)
                ?? throw new MatchBriefingOutputException("The model returned a null briefing.");
        }
        catch (JsonException exception)
        {
            throw new MatchBriefingOutputException("The model returned malformed or schema-incompatible JSON.", exception);
        }

        try
        {
            MatchBriefingValidator.Validate(briefing);
        }
        catch (MatchBriefingValidationException exception)
        {
            throw new MatchBriefingOutputException("The model returned a briefing that failed application validation.", exception);
        }

        return new MatchBriefingResult(
            briefing,
            MatchBriefingContract.PromptVersion,
            MatchBriefingContract.SchemaVersion,
            grounding);
    }
}

public class MatchBriefingGenerationException : Exception
{
    public MatchBriefingGenerationException(string message)
        : base(message)
    {
    }

    public MatchBriefingGenerationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

public sealed class MatchBriefingRefusalException(string refusal)
    : MatchBriefingGenerationException($"The model refused to generate a briefing: {refusal}");

public sealed class MatchBriefingOutputException : MatchBriefingGenerationException
{
    public MatchBriefingOutputException(string message)
        : base(message)
    {
    }

    public MatchBriefingOutputException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
