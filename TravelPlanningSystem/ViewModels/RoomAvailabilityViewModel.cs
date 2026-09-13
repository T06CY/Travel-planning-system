namespace TravelPlanningSystem.ViewModels;

public class RoomAvailabilityViewModel
{
    public int HotelRoomId { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public DateTime SelectedDate { get; set; }
    public int TotalRooms { get; set; }
    public int OccupiedRooms { get; set; }
    public int AvailableRooms => Math.Max(0, TotalRooms - OccupiedRooms);
    public IReadOnlyList<RoomAvailabilitySlotViewModel> Slots { get; set; } = Array.Empty<RoomAvailabilitySlotViewModel>();
}

public class RoomAvailabilitySlotViewModel
{
    public string ReservationReference { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public string Status { get; set; } = string.Empty;
}
