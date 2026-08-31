using System.Reflection;
using System.Text.Json;
using SpillerAstralisStats.Application;

namespace SpillerAstralisStats.Tests;

public sealed class MatchBriefingContractTests
{
    [Fact]
    public void Developer_instruction_limits_the_model_to_supplied_facts_and_Danish_output()
    {
        var instruction = MatchBriefingContract.DeveloperInstruction;

        Assert.Contains("in Danish", instruction, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("only the deterministic facts", instruction, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Do not calculate", instruction, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Do not invent", instruction, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Do not make predictions", instruction, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("expected winner", instruction, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("omit", instruction, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Strict_schema_matches_the_CSharp_output_contract()
    {
        using var document = JsonDocument.Parse(MatchBriefingContract.JsonSchema);
        var root = document.RootElement;
        var properties = root.GetProperty("properties");
        var schemaProperties = properties.EnumerateObject().Select(property => property.Name).Order().ToArray();
        var contractProperties = typeof(MatchBriefing)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(property => JsonNamingPolicy.CamelCase.ConvertName(property.Name))
            .Order()
            .ToArray();

        Assert.Equal("object", root.GetProperty("type").GetString());
        Assert.False(root.GetProperty("additionalProperties").GetBoolean());
        Assert.Equal(contractProperties, schemaProperties);
        Assert.Equal(contractProperties, root.GetProperty("required").EnumerateArray().Select(item => item.GetString()).Order());
        Assert.Equal(MatchBriefingContract.MaxHeadlineLength, properties.GetProperty("headline").GetProperty("maxLength").GetInt32());
        Assert.Equal(MatchBriefingContract.MaxSummaryLength, properties.GetProperty("summary").GetProperty("maxLength").GetInt32());
        Assert.Equal(MatchBriefingContract.MinKeyPoints, properties.GetProperty("keyPoints").GetProperty("minItems").GetInt32());
        Assert.Equal(MatchBriefingContract.MaxKeyPoints, properties.GetProperty("keyPoints").GetProperty("maxItems").GetInt32());
        Assert.Equal(MatchBriefingContract.MaxKeyPointLength, properties.GetProperty("keyPoints").GetProperty("items").GetProperty("maxLength").GetInt32());
    }

    [Fact]
    public void Prompt_and_schema_versions_are_explicit()
    {
        Assert.Equal("match-briefing-prompt-v1", MatchBriefingContract.PromptVersion);
        Assert.Equal("match-briefing-schema-v1", MatchBriefingContract.SchemaVersion);
    }
}
