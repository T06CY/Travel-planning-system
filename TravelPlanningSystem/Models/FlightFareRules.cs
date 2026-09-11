namespace TravelPlanningSystem.Models;

public static class FlightFareRules
{
    public const string Economy = "Economy";
    public const string PremiumEconomy = "Premium Economy";
    public const string Business = "Business";

    public static readonly string[] CabinClasses =
        { Economy, PremiumEconomy, Business };

    public static string Normalize(string? cabinClass) => cabinClass?.Trim() switch
    {
        PremiumEconomy => PremiumEconomy,
        Business => Business,
        _ => Economy
    };

    public static bool IsValid(string? cabinClass) =>
        CabinClasses.Contains(cabinClass, StringComparer.Ordinal);

    public static decimal GetPrice(decimal economyPrice, string? cabinClass, decimal discountPercent = 0)
    {
        var multiplier = Normalize(cabinClass) switch
        {
            PremiumEconomy => 1.35m,
            Business => 1.85m,
            _ => 1m
        };

        var gross = economyPrice * multiplier;
        var discount = Math.Clamp(discountPercent, 0m, 100m);
        return Math.Round(gross * (1m - discount / 100m), 2, MidpointRounding.AwayFromZero);
    }

    public static int GetCapacity(int seatCapacity, string? cabinClass)
    {
        if (seatCapacity <= 0) return 0;

        var business = seatCapacity >= 60 ? Math.Min(24, RoundDownToRow(seatCapacity / 7)) : 0;
        var premium = seatCapacity >= 90 ? Math.Min(30, RoundDownToRow(seatCapacity / 6)) : 0;
        var economy = Math.Max(0, seatCapacity - business - premium);

        return Normalize(cabinClass) switch
        {
            Business => business,
            PremiumEconomy => premium,
            _ => economy
        };
    }

    public static int GetDisplayedAvailability(
        int seatCapacity,
        int availableSeats,
        string? cabinClass)
    {
        var cabinCapacity = GetCapacity(seatCapacity, cabinClass);
        if (cabinCapacity == 0 || seatCapacity <= 0 || availableSeats <= 0) return 0;

        var ratio = Math.Clamp((decimal)availableSeats / seatCapacity, 0m, 1m);
        return Math.Min(cabinCapacity, Math.Max(1, (int)Math.Floor(cabinCapacity * ratio)));
    }

    public static bool IsSeatInCabin(string seatNumber, int seatCapacity, string? cabinClass)
    {
        if (string.IsNullOrWhiteSpace(seatNumber) || seatNumber.Length < 2) return false;
        if (!int.TryParse(seatNumber[..^1], out var row)) return false;

        var businessRows = GetCapacity(seatCapacity, Business) / 6;
        var premiumRows = GetCapacity(seatCapacity, PremiumEconomy) / 6;
        var selectedClass = Normalize(cabinClass);

        return selectedClass switch
        {
            Business => businessRows > 0 && row <= businessRows,
            PremiumEconomy => premiumRows > 0 && row > businessRows && row <= businessRows + premiumRows,
            _ => row > businessRows + premiumRows
        };
    }

    public static string GetSeatType(string? seatNumber)
    {
        if (string.IsNullOrWhiteSpace(seatNumber)) return "Not assigned";

        return char.ToUpperInvariant(seatNumber.Trim()[^1]) switch
        {
            'A' or 'F' => "Window",
            'C' or 'D' => "Aisle",
            'B' or 'E' => "Middle",
            _ => "Seat"
        };
    }

    private static int RoundDownToRow(int seats) => Math.Max(6, seats / 6 * 6);
}
