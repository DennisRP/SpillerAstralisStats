using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SpillerAstralisStats.Configuration;

namespace SpillerAstralisStats.Infrastructure.PandaScore;

internal sealed class PandaScoreMatchClient(HttpClient httpClient, IOptions<PandaScoreOptions> options)
{
    private const long AstralisTeamId = 3209;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<PandaScoreMatchDto>> GetAllMatchesAsync(CancellationToken cancellationToken)
    {
        var allMatches = new Dictionary<long, PandaScoreMatchDto>();
        var page = 1;

        while (true)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"teams/{AstralisTeamId}/matches?page={page}&per_page=100");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.Value.ApiKey);

            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new PandaScoreResponseException((int)response.StatusCode);
            }

            var debug = await response.Content.ReadAsStringAsync();

            await using var content = await response.Content.ReadAsStreamAsync(cancellationToken);
            List<PandaScoreMatchDto> matches;
            try
            {
                matches = await JsonSerializer.DeserializeAsync<List<PandaScoreMatchDto>>(content, JsonOptions, cancellationToken)
                    ?? throw new PandaScorePayloadException();
            }
            catch (JsonException exception)
            {
                throw new PandaScorePayloadException(exception);
            }

            if (matches.Count == 0)
            {
                return allMatches.Values.ToArray();
            }

            foreach (var match in matches)
            {
                allMatches.TryAdd(match.Id, match);
            }

            page++;
        }
    }
}

public sealed class PandaScoreResponseException(int statusCode) : Exception($"PandaScore returned HTTP status {statusCode}.")
{
    public int StatusCode { get; } = statusCode;
}

public sealed class PandaScorePayloadException(Exception? innerException = null) : Exception("PandaScore returned an invalid payload.", innerException);
