using System.ClientModel;
using System.ClientModel.Primitives;
using System.Net;
using System.Text;
using System.Text.Json;
using OpenAI.Responses;
using SpillerAstralisStats.Application;
using SpillerAstralisStats.Infrastructure.Foundry;

namespace SpillerAstralisStats.Tests;

#pragma warning disable OPENAI001 // Tests exercise the approved Responses API integration.

public sealed class FoundryMatchBriefingClientTests
{
    private static readonly Uri Endpoint = new("https://example-resource.openai.azure.com/openai/v1/");

    [Fact]
    public async Task Sends_responses_request_with_strict_schema_and_storage_disabled()
    {
        var handler = new RecordingHandler(_ => JsonResponse(HttpStatusCode.OK, CompletedResponse()));
        var client = CreateClient(handler);
        var request = Request();

        var result = await client.GenerateAsync(request, CancellationToken.None);

        Assert.Null(result.Refusal);
        Assert.Contains("Astralis", result.OutputText, StringComparison.Ordinal);
        Assert.Equal(new Uri(Endpoint, "responses"), handler.RequestUri);

        using var body = JsonDocument.Parse(Assert.IsType<string>(handler.RequestBody));
        var root = body.RootElement;
        Assert.Equal("briefing-deployment", root.GetProperty("model").GetString());
        Assert.Equal(request.Instructions, root.GetProperty("instructions").GetString());
        Assert.False(root.GetProperty("store").GetBoolean());
        Assert.Equal("user", root.GetProperty("input")[0].GetProperty("role").GetString());
        Assert.Equal(request.InputJson, root.GetProperty("input")[0].GetProperty("content")[0].GetProperty("text").GetString());

        var format = root.GetProperty("text").GetProperty("format");
        Assert.Equal("json_schema", format.GetProperty("type").GetString());
        Assert.Equal(request.SchemaName, format.GetProperty("name").GetString());
        Assert.True(format.GetProperty("strict").GetBoolean());
        Assert.True(JsonElement.DeepEquals(
            JsonDocument.Parse(request.JsonSchema).RootElement,
            format.GetProperty("schema")));
        Assert.Equal("https://ai.azure.com/.default", FoundryMatchBriefingClient.AuthenticationScope);
    }

    [Fact]
    public async Task Returns_provider_refusal_for_application_handling()
    {
        var handler = new RecordingHandler(_ => JsonResponse(HttpStatusCode.OK, RefusalResponse()));
        var client = CreateClient(handler);

        var result = await client.GenerateAsync(Request(), CancellationToken.None);

        Assert.Equal("I cannot create this briefing.", result.Refusal);
        Assert.Empty(result.OutputText ?? string.Empty);
    }

    [Fact]
    public async Task Translates_http_errors_without_exposing_response_content()
    {
        var handler = new RecordingHandler(_ => JsonResponse(
            HttpStatusCode.BadRequest,
            """{"error":{"message":"Sensitive provider detail","type":"invalid_request_error","code":"bad_request"}}"""));
        var client = CreateClient(handler);

        var exception = await Assert.ThrowsAsync<MatchBriefingProviderException>(
            () => client.GenerateAsync(Request(), CancellationToken.None));

        Assert.Equal(400, exception.StatusCode);
        Assert.DoesNotContain("Sensitive provider detail", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Propagates_cancellation_to_the_http_transport()
    {
        var handler = new RecordingHandler(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return JsonResponse(HttpStatusCode.OK, CompletedResponse());
        });
        var client = CreateClient(handler);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.GenerateAsync(Request(), cancellation.Token));
    }

    private static FoundryMatchBriefingClient CreateClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler);
        var responsesClient = new ResponsesClient(
            new ApiKeyCredential("test-key"),
            new ResponsesClientOptions
            {
                Endpoint = Endpoint,
                Transport = new HttpClientPipelineTransport(httpClient)
            });

        return new FoundryMatchBriefingClient(responsesClient, "briefing-deployment");
    }

    private static MatchBriefingModelRequest Request() => new(
        "Write only in Danish.",
        """{"recentForm":{"wins":3,"losses":2}}""",
        "match_briefing",
        MatchBriefingContract.JsonSchema);

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json) => new(statusCode)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    private static string CompletedResponse() => """
        {
          "id": "resp_test",
          "object": "response",
          "created_at": 1787890000,
          "status": "completed",
          "model": "briefing-deployment",
          "output": [
            {
              "id": "msg_test",
              "type": "message",
              "status": "completed",
              "role": "assistant",
              "content": [
                {
                  "type": "output_text",
                  "text": "{\"headline\":\"Astralis møder G2\",\"summary\":\"Historisk briefing.\",\"keyPoints\":[\"Et historisk punkt.\"]}",
                  "annotations": []
                }
              ]
            }
          ],
          "parallel_tool_calls": false,
          "tools": []
        }
        """;

    private static string RefusalResponse() => """
        {
          "id": "resp_refusal",
          "object": "response",
          "created_at": 1787890000,
          "status": "completed",
          "model": "briefing-deployment",
          "output": [
            {
              "id": "msg_refusal",
              "type": "message",
              "status": "completed",
              "role": "assistant",
              "content": [
                {
                  "type": "refusal",
                  "refusal": "I cannot create this briefing."
                }
              ]
            }
          ],
          "parallel_tool_calls": false,
          "tools": []
        }
        """;

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responseFactory;

        public RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
            : this((request, _) => Task.FromResult(responseFactory(request)))
        {
        }

        public RecordingHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responseFactory)
        {
            this.responseFactory = responseFactory;
        }

        public Uri? RequestUri { get; private set; }

        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            RequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return await responseFactory(request, cancellationToken);
        }
    }
}

#pragma warning restore OPENAI001
