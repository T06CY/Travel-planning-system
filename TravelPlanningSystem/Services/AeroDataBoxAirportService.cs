using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace TravelPlanningSystem.Services;

public sealed record AirportSuggestion(
    string AirportCode,
    string AirportName,
    string City,
    string CountryCode);

public sealed record AirportSearchResult(
    bool IsLiveData,
    string Message,
    IReadOnlyList<AirportSuggestion> Airports);

public interface IAirportLookupService
{
    Task<AirportSearchResult> SearchAsync(
        string term,
        CancellationToken cancellationToken = default);
}

public sealed class AeroDataBoxAirportService(
    HttpClient httpClient,
    IOptions<RealTimeFlightOptions> options,
    IMemoryCache cache,
    ILogger<AeroDataBoxAirportService> logger)
    : IAirportLookupService
{
    public async Task<AirportSearchResult> SearchAsync(
        string term,
        CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        var cleanedTerm = term.Trim();

        if (cleanedTerm.Length < 3)
        {
            return new AirportSearchResult(
                false,
                "Enter at least 3 characters.",
                Array.Empty<AirportSuggestion>());
        }

        if (!settings.Enabled ||
            string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            return new AirportSearchResult(
                false,
                "AeroDataBox is not configured.",
                Array.Empty<AirportSuggestion>());
        }

        var cacheKey =
            $"AeroDataBox:Airport:{cleanedTerm.ToUpperInvariant()}";

        if (cache.TryGetValue(
                cacheKey,
                out AirportSearchResult? cached) &&
            cached is not null)
        {
            return cached;
        }

        try
        {
            var relativePath =
                "/airports/search/term" +
                $"?q={Uri.EscapeDataString(cleanedTerm)}" +
                "&limit=15" +
                "&withFlightInfoOnly=true" +
                "&withSearchByCode=true";

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                BuildUri(settings.BaseUrl, relativePath));

            request.Headers.TryAddWithoutValidation(
                "X-RapidAPI-Key",
                settings.ApiKey.Trim());
            request.Headers.TryAddWithoutValidation(
                "X-RapidAPI-Host",
                string.IsNullOrWhiteSpace(settings.ApiHost)
                    ? "aerodatabox.p.rapidapi.com"
                    : settings.ApiHost.Trim());
            request.Headers.TryAddWithoutValidation(
                "Accept",
                "application/json");

            using var response = await httpClient.SendAsync(
                request,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "AeroDataBox airport search returned HTTP {StatusCode}.",
                    (int)response.StatusCode);

                return new AirportSearchResult(
                    false,
                    "Live airport search is unavailable.",
                    Array.Empty<AirportSuggestion>());
            }

            var body = await response.Content.ReadAsStringAsync(
                cancellationToken);
            var airports = ParseAirports(body);
            var result = new AirportSearchResult(
                true,
                airports.Count > 0
                    ? "Airport suggestions loaded from AeroDataBox."
                    : "AeroDataBox found no matching airports.",
                airports);

            cache.Set(
                cacheKey,
                result,
                TimeSpan.FromMinutes(Math.Clamp(
                    settings.CacheMinutes,
                    1,
                    30)));

            return result;
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "AeroDataBox airport search is unavailable.");

            return new AirportSearchResult(
                false,
                "Live airport search is unavailable.",
                Array.Empty<AirportSuggestion>());
        }
    }

    private static List<AirportSuggestion> ParseAirports(
        string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var results = new List<AirportSuggestion>();

        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty("items", out var items) ||
            items.ValueKind != JsonValueKind.Array)
        {
            return results;
        }

        foreach (var item in items.EnumerateArray())
        {
            var code = ReadText(item, "iata");
            var name = ReadText(item, "name");
            var city = ReadText(item, "municipalityName");

            if (string.IsNullOrWhiteSpace(code) ||
                string.IsNullOrWhiteSpace(name) ||
                string.IsNullOrWhiteSpace(city))
            {
                continue;
            }

            results.Add(new AirportSuggestion(
                code.ToUpperInvariant(),
                name,
                city,
                ReadText(item, "countryCode") ?? string.Empty));
        }

        return results
            .GroupBy(
                airport => airport.AirportCode,
                StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
    }

    private static string? ReadText(
        JsonElement element,
        string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(propertyName, out var value) ||
            value.ValueKind is JsonValueKind.Null or
                JsonValueKind.Undefined)
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : value.ToString();
    }

    private static Uri BuildUri(
        string baseUrl,
        string relativePath) =>
        new(
            new Uri(baseUrl.TrimEnd('/') + "/"),
            relativePath.TrimStart('/'));
}
