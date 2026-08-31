namespace SpillerAstralisStats.Application;

internal static class MatchBriefingContract
{
    internal const string PromptVersion = "match-briefing-prompt-v1";
    internal const string SchemaVersion = "match-briefing-schema-v1";
    internal const string SchemaName = "match_briefing";

    internal const int MaxHeadlineLength = 120;
    internal const int MaxSummaryLength = 600;
    internal const int MinKeyPoints = 1;
    internal const int MaxKeyPoints = 4;
    internal const int MaxKeyPointLength = 240;

    internal const string DeveloperInstruction = """
        You are an editor writing a concise historical CS2 match briefing.
        Write the headline, summary, and every key point in Danish.
        Use only the deterministic facts supplied in the user JSON as factual evidence.
        Do not calculate or recalculate statistics, scores, percentages, dates, or outcomes.
        Do not invent numbers, facts, context, or explanations that were not supplied.
        Do not make predictions, estimate win probabilities, or choose an expected winner.
        If the supplied facts do not support a conclusion, omit that conclusion instead of guessing.
        Return only the structured output defined by the supplied JSON Schema.
        """;

    internal const string JsonSchema = """
        {
          "type": "object",
          "additionalProperties": false,
          "required": ["headline", "summary", "keyPoints"],
          "properties": {
            "headline": {
              "type": "string",
              "minLength": 1,
              "maxLength": 120
            },
            "summary": {
              "type": "string",
              "minLength": 1,
              "maxLength": 600
            },
            "keyPoints": {
              "type": "array",
              "minItems": 1,
              "maxItems": 4,
              "items": {
                "type": "string",
                "minLength": 1,
                "maxLength": 240
              }
            }
          }
        }
        """;
}
