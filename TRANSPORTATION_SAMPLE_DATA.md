# Transportation Sample Data - Quick Setup Guide

## 🌱 How to Seed Sample Data

Add this code to your `Program.cs` file after the database migration. Add it **after building the host** but **before `app.Run()`**:

```csharp
// After: var app = builder.Build();
// Before: app.Run();

using (var scope = app.Services.CreateScope())
{
	var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
	await SeedTransportationDataAsync(context);
}

app.Run();

// Add this method at the end of Program.cs, before the final closing brace
async Task SeedTransportationDataAsync(AppDbContext context)
{
	// Only seed if routes don't exist
	if (context.Routes.Any())
		return;

	// Sample Routes
	var routes = new List<TravelPlanningSystem.Models.Transportation.Route>
	{
		new() {
			Origin = "Kuala Lumpur",
			Destination = "Selangor",
			DistanceKm = 45,
			EstimatedDurationHours = 1.5,
			Stops = "Shah Alam, Petaling Jaya",
			Description = "Direct route from KL city center to Selangor",
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		},
		new() {
			Origin = "Kuala Lumpur",
			Destination = "Penang",
			DistanceKm = 380,
			EstimatedDurationHours = 5,
			Stops = "Ipoh, Taiping",
			Description = "Express service to Penang",
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		},
		new() {
			Origin = "Kuala Lumpur",
			Destination = "Melaka",
			DistanceKm = 140,
			EstimatedDurationHours = 2,
			Stops = "None",
			Description = "Direct route to historic Melaka",
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		},
		new() {
			Origin = "Selangor",
			Destination = "Pahang",
			DistanceKm = 210,
			EstimatedDurationHours = 3.5,
			Stops = "Kuantan",
			Description = "Scenic route to Pahang",
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		},
		new() {
			Origin = "Penang",
			Destination = "Kedah",
			DistanceKm = 85,
			EstimatedDurationHours = 1.5,
			Stops = "Butterworth",
			Description = "Short route to Kedah",
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		}
	};

	context.Routes.AddRange(routes);
	await context.SaveChangesAsync();

	// Sample Vehicles
	var vehicles = new List<TravelPlanningSystem.Models.Transportation.Vehicle>
	{
		new() {
			LicensePlate = "KL-001-A",
			VehicleModel = "Mercedes Sprinter",
			VehicleType = "Coach",
			SeatingCapacity = 50,
			ManufactureYear = 2023,
			Amenities = "WiFi, Air Conditioning, USB Charging, Toilet, Reclining Seats",
			RegistrationNumber = "WP-KL-001",
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		},
		new() {
			LicensePlate = "KL-002-B",
			VehicleModel = "Volvo B11R",
			VehicleType = "Bus",
			SeatingCapacity = 45,
			ManufactureYear = 2022,
			Amenities = "WiFi, Air Conditioning, USB Charging, Power Outlets",
			RegistrationNumber = "WP-KL-002",
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		},
		new() {
			LicensePlate = "KL-003-C",
			VehicleModel = "Toyota Hiace",
			VehicleType = "Van",
			SeatingCapacity = 15,
			ManufactureYear = 2023,
			Amenities = "Air Conditioning, Power Windows",
			RegistrationNumber = "WP-KL-003",
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		},
		new() {
			LicensePlate = "KL-004-D",
			VehicleModel = "MAN Lion's Coach",
			VehicleType = "Coach",
			SeatingCapacity = 55,
			ManufactureYear = 2021,
			Amenities = "WiFi, Air Conditioning, Toilet, Power Outlets, Entertainment System",
			RegistrationNumber = "WP-KL-004",
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		},
		new() {
			LicensePlate = "KL-005-E",
			VehicleModel = "Scania K410",
			VehicleType = "Bus",
			SeatingCapacity = 48,
			ManufactureYear = 2023,
			Amenities = "WiFi, Air Conditioning, USB Charging, Toilet",
			RegistrationNumber = "WP-KL-005",
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		}
	};

	context.Vehicles.AddRange(vehicles);
	await context.SaveChangesAsync();

	// Sample Trips
	var trips = new List<TravelPlanningSystem.Models.Transportation.Trip>
	{
		// KL to Selangor - Today
		new() {
			RouteId = routes[0].RouteId,
			VehicleId = vehicles[2].VehicleId,
			DepartureTime = DateTime.UtcNow.AddHours(2).Date.AddHours(8),
			ArrivalTime = DateTime.UtcNow.AddHours(2).Date.AddHours(9.5),
			BaseFare = 25.00m,
			DiscountPercentage = 0,
			AvailableSeats = 12,
			TotalSeats = 15,
			Status = "Scheduled",
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		},
		new() {
			RouteId = routes[0].RouteId,
			VehicleId = vehicles[2].VehicleId,
			DepartureTime = DateTime.UtcNow.AddHours(2).Date.AddHours(14),
			ArrivalTime = DateTime.UtcNow.AddHours(2).Date.AddHours(15.5),
			BaseFare = 25.00m,
			DiscountPercentage = 10,
			AvailableSeats = 5,
			TotalSeats = 15,
			Status = "Scheduled",
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		},

		// KL to Penang - Tomorrow
		new() {
			RouteId = routes[1].RouteId,
			VehicleId = vehicles[0].VehicleId,
			DepartureTime = DateTime.UtcNow.AddDays(1).Date.AddHours(8),
			ArrivalTime = DateTime.UtcNow.AddDays(1).Date.AddHours(13),
			BaseFare = 55.00m,
			DiscountPercentage = 0,
			AvailableSeats = 32,
			TotalSeats = 50,
			Status = "Scheduled",
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		},
		new() {
			RouteId = routes[1].RouteId,
			VehicleId = vehicles[3].VehicleId,
			DepartureTime = DateTime.UtcNow.AddDays(1).Date.AddHours(20),
			ArrivalTime = DateTime.UtcNow.AddDays(2).Date.AddHours(1),
			BaseFare = 50.00m,
			DiscountPercentage = 5,
			AvailableSeats = 10,
			TotalSeats = 55,
			Status = "Scheduled",
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		},

		// KL to Melaka - Today
		new() {
			RouteId = routes[2].RouteId,
			VehicleId = vehicles[1].VehicleId,
			DepartureTime = DateTime.UtcNow.AddHours(2).Date.AddHours(10),
			ArrivalTime = DateTime.UtcNow.AddHours(2).Date.AddHours(12),
			BaseFare = 35.00m,
			DiscountPercentage = 0,
			AvailableSeats = 15,
			TotalSeats = 45,
			Status = "On-time",
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		},
		new() {
			RouteId = routes[2].RouteId,
			VehicleId = vehicles[1].VehicleId,
			DepartureTime = DateTime.UtcNow.AddHours(2).Date.AddHours(16),
			ArrivalTime = DateTime.UtcNow.AddHours(2).Date.AddHours(18),
			BaseFare = 35.00m,
			DiscountPercentage = 15,
			AvailableSeats = 8,
			TotalSeats = 45,
			Status = "Scheduled",
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		},

		// Selangor to Pahang - Day after tomorrow
		new() {
			RouteId = routes[3].RouteId,
			VehicleId = vehicles[4].VehicleId,
			DepartureTime = DateTime.UtcNow.AddDays(2).Date.AddHours(7),
			ArrivalTime = DateTime.UtcNow.AddDays(2).Date.AddHours(10.5),
			BaseFare = 42.00m,
			DiscountPercentage = 0,
			AvailableSeats = 20,
			TotalSeats = 48,
			Status = "Scheduled",
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		},

		// Penang to Kedah - Today
		new() {
			RouteId = routes[4].RouteId,
			VehicleId = vehicles[2].VehicleId,
			DepartureTime = DateTime.UtcNow.AddHours(2).Date.AddHours(9),
			ArrivalTime = DateTime.UtcNow.AddHours(2).Date.AddHours(10.5),
			BaseFare = 20.00m,
			DiscountPercentage = 0,
			AvailableSeats = 0,
			TotalSeats = 15,
			Status = "Scheduled",
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		}
	};

	context.Trips.AddRange(trips);
	await context.SaveChangesAsync();

	// Sample Seats for first trip
	if (trips.Any())
	{
		var firstTrip = trips[0];
		var seats = new List<TravelPlanningSystem.Models.Transportation.Seat>();

		for (int i = 1; i <= firstTrip.TotalSeats; i++)
		{
			string seatNumber = $"{(i - 1) / 3 + 1}{(char)('A' + (i - 1) % 3)}";
			seats.Add(new TravelPlanningSystem.Models.Transportation.Seat
			{
				TripId = firstTrip.TripId,
				SeatNumber = seatNumber,
				SeatClass = i <= 5 ? "Premium" : "Economy",
				SeatType = i % 2 == 0 ? "Window" : "Aisle",
				IsAvailable = i > 3, // First 3 seats are booked
				Status = i > 3 ? "Available" : "Booked",
				CreatedAt = DateTime.UtcNow
			});
		}

		context.Seats.AddRange(seats);
	}

	await context.SaveChangesAsync();

	Console.WriteLine("✅ Transportation sample data seeded successfully!");
}
```

---

## 📊 Sample Data Overview

### Routes (5 total)
1. **Kuala Lumpur → Selangor** (45 km, 1.5 hours)
2. **Kuala Lumpur → Penang** (380 km, 5 hours)
3. **Kuala Lumpur → Melaka** (140 km, 2 hours)
4. **Selangor → Pahang** (210 km, 3.5 hours)
5. **Penang → Kedah** (85 km, 1.5 hours)

### Vehicles (5 total)
- Mercedes Sprinter Coach (50 seats) - Premium
- Volvo B11R Bus (45 seats) - Standard
- Toyota Hiace Van (15 seats) - Economy
- MAN Lion's Coach (55 seats) - Premium
- Scania K410 Bus (48 seats) - Standard

### Trips (8 total)
- **Today**: 5 trips departing at various times
- **Tomorrow**: 2 long-distance trips
- **Day after tomorrow**: 1 trip

**Features demonstrated:**
- ✅ Different statuses (Scheduled, On-time)
- ✅ Various discount percentages (0%, 5%, 10%, 15%)
- ✅ Full, partial, and empty seats
- ✅ Different vehicle types and capacities
- ✅ Varying departure times

---

## ✨ Testing Scenarios

After seeding, you can test:

1. **Search by Origin/Destination**
   - Filter "Kuala Lumpur" → "Penang"
   - Should show 2 trips

2. **Price Filtering**
   - Filter price range RM 20-50
   - Should show various KL-Selangor and KL-Melaka trips

3. **Available Seats Only**
   - Check "Only available seats"
   - Should hide the fully booked Penang-Kedah trip

4. **Sort by Price (Low to High)**
   - Penang-Kedah (RM 20) should appear first
   - Penang trips (RM 50-55) should appear last

5. **View Trip Details**
   - Click any trip to see full details
   - Verify seat availability progress bar
   - Check vehicle information

---

## 🔄 Clear & Re-seed

To clear and re-seed data, delete the seeding code logic and re-run the migration:

```powershell
# Remove last migration (if not yet deployed to production)
Remove-Migration

# Re-add and update
Add-Migration AddTransportationModule
Update-Database

# Run application to re-seed
```

---

**Ready to test!** 🚀
