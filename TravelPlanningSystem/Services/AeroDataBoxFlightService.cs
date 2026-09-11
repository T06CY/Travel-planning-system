using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace TravelPlanningSystem.Services;

public sealed class RealTimeFlightOptions
{
    public const string SectionName = "RealTimeFlights";

    public bool Enabled { get; set; }
    public string BaseUrl { get; set; } =
        "https://aerodatabox.p.rapidapi.com";
    public string ApiHost { get; set; } =
        "aerodatabox.p.rapidapi.com";
    public string ApiKey { get; set; } = string.Empty;
    public int MaxResults { get; set; } = 12;
    public int CacheMinutes { get; set; } = 10;
}

public sealed record RealTimeFlightOffer(
    string ExternalOfferId,
    string AirlineCode,
    string AirlineName,
    string FlightNumber,
    string OriginAirportCode,
    string DestinationAirportCode,
    DateTime DepartureTime,
    DateTime ArrivalTime,
    decimal? PriceMyr,
    int? AvailableSeats,
    string AircraftModel,
    string FlightStatus);

public sealed record RealTimeFlightSearchResult(
    bool IsConfigured,
    bool WasSuccessful,
    string Message,
    DateTime CheckedAtUtc,
    IReadOnlyList<RealTimeFlightOffer> Offers);

public interface IRealTimeFlightService
{
    Task<RealTimeFlightSearchResult> SearchAsync(
        string originAirportCode,
        string destinationAirportCode,
        DateTime departureDate,
        int passengers,
        CancellationToken cancellationToken = default);
}

public sealed class AeroDataBoxFlightService(
    HttpClient httpClient,
    IOptions<RealTimeFlightOptions> options,
    IMemoryCache cache,
    ILogger<AeroDataBoxFlightService> logger)
    : IRealTimeFlightService
{
    public async Task<RealTimeFlightSearchResult> SearchAsync(
        string originAirportCode,
        string destinationAirportCode,
        DateTime departureDate,
        int passengers,
        CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        var checkedAt = DateTime.UtcNow;

        if (!settings.Enabled ||
            string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            return new RealTimeFlightSearchResult(
                false,
                false,
                "AeroDataBox is not configured. Showing database schedules.",
                checkedAt,
                Array.Empty<RealTimeFlightOffer>());
        }

        var origin = originAirportCode.Trim().ToUpperInvariant();
        var destination =
            destinationAirportCode.Trim().ToUpperInvariant();
        var cacheKey =
            $"AeroDataBox:{origin}:{destination}:" +
            $"{departureDate:yyyy-MM-dd}";

        if (cache.TryGetValue(
                cacheKey,
                out RealTimeFlightSearchResult? cached) &&
            cached is not null)
        {
            return cached with
            {
                Message =
                    "AeroDataBox schedule/status loaded from cache; prices and seats come from the database."
            };
        }

        try
        {
            // The entry-level AeroDataBox FIDS endpoint permits a maximum
            // 12-hour range, so two requests cover the complete travel day.
            var windows = new[]
            {
                (
                    departureDate.Date,
                    departureDate.Date.AddHours(12)),
                (
                    departureDate.Date.AddHours(12),
                    departureDate.Date.AddDays(1).AddMinutes(-1))
            };

            var offers = new List<RealTimeFlightOffer>();

            foreach (var (fromLocal, toLocal) in windows)
            {
                var windowResult = await RequestWindowAsync(
                    settings,
                    origin,
                    destination,
                    fromLocal,
                    toLocal,
                    cancellationToken);

                if (!windowResult.Success)
                {
                    return new RealTimeFlightSearchResult(
                        true,
                        false,
                        string.IsNullOrWhiteSpace(windowResult.Error)
                            ? "AeroDataBox could not return results. Showing database schedules."
                            : $"AeroDataBox error: {windowResult.Error}. Showing database schedules.",
                        checkedAt,
                        Array.Empty<RealTimeFlightOffer>());
                }

                offers.AddRange(windowResult.Offers);
            }

            var finalOffers = offers
                .Where(offer =>
                    offer.OriginAirportCode.Equals(
                        origin,
                        StringComparison.OrdinalIgnoreCase) &&
                    offer.DestinationAirportCode.Equals(
                        destination,
                        StringComparison.OrdinalIgnoreCase) &&
                    offer.DepartureTime.Date == departureDate.Date)
                .GroupBy(
                    offer => offer.ExternalOfferId,
                    StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .OrderBy(offer => offer.DepartureTime)
                .Take(Math.Clamp(settings.MaxResults, 1, 100))
                .ToList();

            var result = new RealTimeFlightSearchResult(
                true,
                true,
                finalOffers.Count > 0
                    ? "Live AeroDataBox schedule/status refreshed; prices and seats come from the database."
                    : "AeroDataBox returned no matching flight. Showing database schedules.",
                checkedAt,
                finalOffers);

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
                "AeroDataBox is unavailable.");

            return new RealTimeFlightSearchResult(
                true,
                false,
                "AeroDataBox is temporarily unavailable. Showing database schedules.",
                checkedAt,
                Array.Empty<RealTimeFlightOffer>());
        }
    }

    private async Task<WindowResult> RequestWindowAsync(
        RealTimeFlightOptions settings,
        string origin,
        string destination,
        DateTime fromLocal,
        DateTime toLocal,
        CancellationToken cancellationToken)
    {
        var fromText = fromLocal.ToString(
            "yyyy-MM-dd'T'HH:mm",
            CultureInfo.InvariantCulture);
        var toText = toLocal.ToString(
            "yyyy-MM-dd'T'HH:mm",
            CultureInfo.InvariantCulture);

        var relativePath =
            $"/flights/airports/iata/{Uri.EscapeDataString(origin)}/" +
            $"{fromText}/{toText}" +
            "?direction=Departure" +
            "&withLeg=true" +
            "&withCancelled=true" +
            "&withCodeshared=true" +
            "&withCargo=false" +
            "&withPrivate=false" +
            "&withLocation=false";

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
        var body = await response.Content.ReadAsStringAsync(
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = ReadProviderError(body) ??
                (response.StatusCode switch
                {
                    System.Net.HttpStatusCode.Unauthorized =>
                        "the RapidAPI key is invalid",
                    System.Net.HttpStatusCode.Forbidden =>
                        "the AeroDataBox endpoint is not included in the selected RapidAPI plan",
                    (System.Net.HttpStatusCode)429 =>
                        "the RapidAPI request quota has been reached",
                    _ =>
                        $"request failed with HTTP {(int)response.StatusCode}"
                });

            logger.LogWarning(
                "AeroDataBox returned {StatusCode}: {ProviderError}",
                (int)response.StatusCode,
                error);

            return new WindowResult(
                false,
                error,
                Array.Empty<RealTimeFlightOffer>());
        }

        return new WindowResult(
            true,
            null,
            ParseFlights(body, origin, destination));
    }

    private static List<RealTimeFlightOffer> ParseFlights(
        string json,
        string requestedOrigin,
        string requestedDestination)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var results = new List<RealTimeFlightOffer>();

        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty("departures", out var departures) ||
            departures.ValueKind != JsonValueKind.Array)
        {
            return results;
        }

        foreach (var item in departures.EnumerateArray())
        {
            if (!TryGetObject(item, "departure", out var departure) ||
                !TryGetObject(item, "arrival", out var arrival))
            {
                continue;
            }

            var origin = ReadAirportCode(departure) ?? requestedOrigin;
            var destination =
                ReadAirportCode(arrival) ?? string.Empty;

            if (!destination.Equals(
                    requestedDestination,
                    StringComparison.OrdinalIgnoreCase) ||
                !TryReadMovementTime(
                    departure,
                    out var departureTime) ||
                !TryReadMovementTime(
                    arrival,
                    out var arrivalTime))
            {
                continue;
            }

            var flightNumber =
                ReadText(item, "number")?.Replace(" ", "") ??
                "XX000";

            JsonElement airline = default;
            var hasAirline = TryGetObject(
                item,
                "airline",
                out airline);

            var airlineCode = hasAirline
                ? ReadText(airline, "iata") ??
                  ReadText(airline, "icao")
                : null;

            airlineCode ??= new string(
                flightNumber
                    .TakeWhile(char.IsLetter)
                    .ToArray());

            if (string.IsNullOrWhiteSpace(airlineCode))
            {
                airlineCode = "XX";
            }

            var aircraftModel = "Aircraft";

            if (TryGetObject(item, "aircraft", out var aircraft))
            {
                aircraftModel =
                    ReadText(aircraft, "model") ??
                    "Aircraft";
            }

            var status =
                ReadText(item, "status") ??
                "Scheduled";

            results.Add(new RealTimeFlightOffer(
                $"{flightNumber}-{departureTime:yyyyMMddHHmmss}",
                airlineCode.ToUpperInvariant(),
                hasAirline
                    ? ReadText(airline, "name") ?? airlineCode
                    : airlineCode,
                flightNumber.ToUpperInvariant(),
                origin.ToUpperInvariant(),
                destination.ToUpperInvariant(),
                departureTime,
                arrivalTime,
                null,
                null,
                aircraftModel,
                status));
        }

        return results;
    }

    private static bool TryReadMovementTime(
        JsonElement movement,
        out DateTime result)
    {
        foreach (var propertyName in new[]
        {
            "revisedTime",
            "predictedTime",
            "scheduledTime"
        })
        {
            if (!TryGetObject(
                    movement,
                    propertyName,
                    out var time))
            {
                continue;
            }

            var value =
                ReadText(time, "local") ??
                ReadText(time, "utc");

            if (TryReadDateTime(value, out result))
            {
                return true;
            }
        }

        result = default;
        return false;
    }

    private static string? ReadAirportCode(JsonElement movement)
    {
        if (!TryGetObject(movement, "airport", out var airport))
        {
            return null;
        }

        return ReadText(airport, "iata") ??
               ReadText(airport, "icao");
    }

    private static string? ReadProviderError(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (root.ValueKind == JsonValueKind.String)
            {
                return root.GetString();
            }

            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            foreach (var propertyName in new[]
            {
                "message",
                "detail",
                "error",
                "reason",
                "title"
            })
            {
                if (!root.TryGetProperty(
                        propertyName,
                        out var value))
                {
                    continue;
                }

                if (value.ValueKind == JsonValueKind.String)
                {
                    return value.GetString();
                }

                if (value.ValueKind == JsonValueKind.Object)
                {
                    return ReadText(value, "message") ??
                           ReadText(value, "detail") ??
                           ReadText(value, "reason");
                }
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }

    private static bool TryGetObject(
        JsonElement element,
        string propertyName,
        out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty(propertyName, out value) &&
            value.ValueKind == JsonValueKind.Object)
        {
            return true;
        }

        value = default;
        return false;
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

    private static bool TryReadDateTime(
        string? value,
        out DateTime result)
    {
        if (DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var dateTimeOffset))
        {
            result = DateTime.SpecifyKind(
                dateTimeOffset.DateTime,
                DateTimeKind.Unspecified);
            return true;
        }

        if (DateTime.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces,
                out var localDateTime))
        {
            result = DateTime.SpecifyKind(
                localDateTime,
                DateTimeKind.Unspecified);
            return true;
        }

        result = default;
        return false;
    }

    private static Uri BuildUri(
        string baseUrl,
        string relativePath) =>
        new(
            new Uri(baseUrl.TrimEnd('/') + "/"),
            relativePath.TrimStart('/'));

    private sealed record WindowResult(
        bool Success,
        string? Error,
        IReadOnlyList<RealTimeFlightOffer> Offers);
}
