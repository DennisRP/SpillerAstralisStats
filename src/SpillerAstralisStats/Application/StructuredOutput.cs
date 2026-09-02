namespace SpillerAstralisStats.Application;

internal sealed record StructuredOutputRequest(
    string Instructions,
    string InputJson,
    string SchemaName,
    string JsonSchema);

internal sealed record StructuredOutputResponse(
    string? OutputText,
    string? Refusal);

internal interface IStructuredOutputClient
{
    Task<StructuredOutputResponse> GenerateAsync(
        StructuredOutputRequest request,
        CancellationToken cancellationToken);
}

internal sealed class MatchBriefingClientAdapter(IStructuredOutputClient client) : IMatchBriefingClient
{
    public async Task<MatchBriefingModelResponse> GenerateAsync(
        MatchBriefingModelRequest request,
        CancellationToken cancellationToken)
    {
        var response = await client.GenerateAsync(
            new StructuredOutputRequest(request.Instructions, request.InputJson, request.SchemaName, request.JsonSchema),
            cancellationToken);
        return new MatchBriefingModelResponse(response.OutputText, response.Refusal);
    }
}
