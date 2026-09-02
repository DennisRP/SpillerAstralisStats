using Azure.Identity;
using Microsoft.Extensions.Options;
using OpenAI.Responses;
using SpillerAstralisStats.Application;
using SpillerAstralisStats.Configuration;
using System.ClientModel;
using System.ClientModel.Primitives;

namespace SpillerAstralisStats.Infrastructure.Foundry;

#pragma warning disable OPENAI001 // The approved design explicitly uses the Responses API, which the SDK marks experimental.

internal sealed class FoundryMatchBriefingClient : IStructuredOutputClient, IMatchBriefingClient
{
    internal const string AuthenticationScope = "https://ai.azure.com/.default";

    private readonly ResponsesClient client;
    private readonly string deploymentName;

    public FoundryMatchBriefingClient(IOptions<FoundryOptions> options)
    {
        var settings = options.Value;
        if (!settings.TryValidate(out var failure))
        {
            throw new InvalidOperationException(failure);
        }

        deploymentName = settings.DeploymentName;
        client = new ResponsesClient(
            new BearerTokenPolicy(new DefaultAzureCredential(), AuthenticationScope),
            new ResponsesClientOptions { Endpoint = new Uri(settings.Endpoint) });
    }

    internal FoundryMatchBriefingClient(ResponsesClient client, string deploymentName)
    {
        this.client = client ?? throw new ArgumentNullException(nameof(client));
        this.deploymentName = string.IsNullOrWhiteSpace(deploymentName)
            ? throw new ArgumentException("Deployment name is required.", nameof(deploymentName))
            : deploymentName;
    }

    public async Task<MatchBriefingModelResponse> GenerateAsync(
        MatchBriefingModelRequest request,
        CancellationToken cancellationToken)
    {
        var response = await GenerateAsync(
            new StructuredOutputRequest(request.Instructions, request.InputJson, request.SchemaName, request.JsonSchema),
            cancellationToken);
        return new MatchBriefingModelResponse(response.OutputText, response.Refusal);
    }

    public async Task<StructuredOutputResponse> GenerateAsync(
        StructuredOutputRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var options = new CreateResponseOptions
        {
            Model = deploymentName,
            Instructions = request.Instructions,
            StoredOutputEnabled = false,
            TextOptions = new ResponseTextOptions
            {
                TextFormat = ResponseTextFormat.CreateJsonSchemaFormat(
                    request.SchemaName,
                    BinaryData.FromString(request.JsonSchema),
                    jsonSchemaFormatDescription: null,
                    jsonSchemaIsStrict: true)
            }
        };
        options.InputItems.Add(ResponseItem.CreateUserMessageItem(request.InputJson));

        try
        {
            var result = await client.CreateResponseAsync(options, cancellationToken);
            var response = result.Value;
            if (response.Error is not null)
            {
                throw new MatchBriefingProviderException("Foundry returned a failed response.");
            }

            var refusal = response.OutputItems
                .OfType<MessageResponseItem>()
                .SelectMany(item => item.Content)
                .FirstOrDefault(part => part.Kind == ResponseContentPartKind.Refusal)
                ?.Refusal;

            return new StructuredOutputResponse(response.GetOutputText(), refusal);
        }
        catch (ClientResultException exception)
        {
            throw new MatchBriefingProviderException(
                $"Foundry returned HTTP status {exception.Status}.",
                exception.Status,
                exception);
        }
    }
}

#pragma warning restore OPENAI001

public sealed class MatchBriefingProviderException : MatchBriefingGenerationException
{
    public MatchBriefingProviderException(string message)
        : base(message)
    {
    }

    public MatchBriefingProviderException(string message, int statusCode, Exception innerException)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }

    public int? StatusCode { get; }
}
