# 🚀 Core Module 1 - Quick Start Guide

## ⚡ 5-Minute Setup

Follow these steps to get Route Discovery & Catalog running:

### Step 1️⃣: Create Database Migration (30 seconds)

Open **Package Manager Console** in Visual Studio:

```powershell
Add-Migration AddTransportationModule -Project TravelPlanningSystem
```

You should see:
```
Build started...
...
Migration file created: Migrations\20240101000000_AddTransportationModule.cs
```

### Step 2️⃣: Update Database (30 seconds)

Still in Package Manager Console:

```powershell
Update-Database
```

Wait for completion (should show "Done" message).

### Step 3️⃣: Add Sample Data (1 minute)

Open `Program.cs` and find this section:
```csharp
var app = builder.Build();

// ← Add the code HERE
// var app = builder.Build(); <- before this line

app.Run();
```

Copy and paste this entire code block **between** `var app = builder.Build();` and `app.Run();`:

```csharp
// Seed Transportation Sample Data
using (var scope = app.Services.CreateScope())
{
	var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
	await SeedTransportationDataAsync(context);
}

// Add this method at the END of Program.cs (before final closing brace)
async Task SeedTransportationDataAsync(AppDbContext context)
{
	if (context.Routes.Any())
		return; // Already seeded

	// Routes
	var routes = new List<TravelPlanningSystem.Models.Transportation.Route>
	{
		new() { Origin = "Kuala Lumpur", Destination = "Selangor", DistanceKm = 45, EstimatedDurationHours = 1.5, Stops = "Shah Alam, Petaling Jaya", IsActive = true, CreatedAt = DateTime.UtcNow },
		new() { Origin = "Kuala Lumpur", Destination = "Penang", DistanceKm = 380, EstimatedDurationHours = 5, Stops = "Ipoh, Taiping", IsActive = true, CreatedAt = DateTime.UtcNow },
		new() { Origin = "Kuala Lumpur", Destination = "Melaka", DistanceKm = 140, EstimatedDurationHours = 2, Stops = "None", IsActive = true, CreatedAt = DateTime.UtcNow },
	};
	context.Routes.AddRange(routes);
	await context.SaveChangesAsync();

	// Vehicles
	var vehicles = new List<TravelPlanningSystem.Models.Transportation.Vehicle>
	{
		new() { LicensePlate = "KL-001-A", VehicleModel = "Mercedes Sprinter", VehicleType = "Coach", SeatingCapacity = 50, ManufactureYear = 2023, Amenities = "WiFi, Air Conditioning, USB Charging, Toilet", IsActive = true, CreatedAt = DateTime.UtcNow },
		new() { LicensePlate = "KL-002-B", VehicleModel = "Volvo B11R", VehicleType = "Bus", SeatingCapacity = 45, ManufactureYear = 2022, Amenities = "WiFi, Air Conditioning, Power Outlets", IsActive = true, CreatedAt = DateTime.UtcNow },
		new() { LicensePlate = "KL-003-C", VehicleModel = "Toyota Hiace", VehicleType = "Van", SeatingCapacity = 15, ManufactureYear = 2023, Amenities = "Air Conditioning", IsActive = true, CreatedAt = DateTime.UtcNow },
	};
	context.Vehicles.AddRange(vehicles);
	await context.SaveChangesAsync();

	// Trips
	var trips = new List<TravelPlanningSystem.Models.Transportation.Trip>
	{
		new() { RouteId = routes[0].RouteId, VehicleId = vehicles[2].VehicleId, DepartureTime = DateTime.UtcNow.AddHours(2).Date.AddHours(8), ArrivalTime = DateTime.UtcNow.AddHours(2).Date.AddHours(9.5), BaseFare = 25.00m, DiscountPercentage = 0, AvailableSeats = 12, TotalSeats = 15, Status = "Scheduled", IsActive = true, CreatedAt = DateTime.UtcNow },
		new() { RouteId = routes[1].RouteId, VehicleId = vehicles[0].VehicleId, DepartureTime = DateTime.UtcNow.AddDays(1).Date.AddHours(8), ArrivalTime = DateTime.UtcNow.AddDays(1).Date.AddHours(13), BaseFare = 55.00m, DiscountPercentage = 0, AvailableSeats = 32, TotalSeats = 50, Status = "Scheduled", IsActive = true, CreatedAt = DateTime.UtcNow },
		new() { RouteId = routes[2].RouteId, VehicleId = vehicles[1].VehicleId, DepartureTime = DateTime.UtcNow.AddHours(2).Date.AddHours(10), ArrivalTime = DateTime.UtcNow.AddHours(2).Date.AddHours(12), BaseFare = 35.00m, DiscountPercentage = 0, AvailableSeats = 15, TotalSeats = 45, Status = "On-time", IsActive = true, CreatedAt = DateTime.UtcNow },
	};
	context.Trips.AddRange(trips);
	await context.SaveChangesAsync();

	System.Console.WriteLine("✅ Transportation data seeded!");
}
```

### Step 4️⃣: Run Application (2 minutes)

Press **F5** or click **Start Debugging** in Visual Studio.

Wait for the application to build and start. You should see:
```
✅ Transportation data seeded!
```

### Step 5️⃣: Test It! (30 seconds)

1. Go to **Home page** - you'll see a "Transportation" service card
2. Click on the Transportation card or go to **`http://localhost:xxxx/Transportation`**
3. You should see 3 trips displayed!

---

## 🎯 What You Can Test

### ✅ Search & Filter
- Select "Kuala Lumpur" as origin
- Select "Penang" as destination
- Click Search button
- Should show 1 trip

### ✅ Filter by Price
- Set Min Price: 30, Max Price: 50
- Apply Filters
- Should show Melaka and Selangor trips

### ✅ View Details
- Click "View Details" on any trip
- See full trip information
- Check seat availability bar
- View trip summary in sidebar

### ✅ Sort Options
- Change "Sort By" dropdown
- Try: Price Low→High, Price High→Low, Rating
- Results should reorder accordingly

---

## 📍 Navigation

After setup, you can access:

| Page | URL | Description |
|------|-----|-------------|
| Home | `/` | Home page with Transportation card |
| Transportation | `/Transportation` | Trip listing & search |
| Trip Details | `/Transportation/Details/{id}` | Detailed trip view |
| Via Navbar | Top menu | "Transport" link |

---

## 🔍 What's Displayed

### Trip Cards Show:
```
KL → Selangor                         Fri, Nov 22
08:00 ──────→ 09:30 [1.5h]
Toyota Hiace • 12 seats left
⭐ 4.5 (8 reviews)
RM 25.00 → [View Details]
```

### Filters Available:
- Origin ✅
- Destination ✅
- Departure Date ✅
- Price Range ✅
- Vehicle Type ✅
- Minimum Rating ✅
- Seat Availability ✅
- Departure Time ✅

---

## 🐛 Troubleshooting

| Issue | Solution |
|-------|----------|
| "No trips found" | Run sample data script from Step 3 |
| Styling looks broken | Clear browser cache (Ctrl+Shift+Delete) |
| Navigation link missing | Restart application |
| Database error | Ensure migrations were run (Step 1-2) |
| Can't find Transportation page | Check URL: `/Transportation` |

---

## 📚 Full Documentation

For detailed information, see:
- 📋 **TRANSPORTATION_MODULE_1_SETUP.md** - Complete setup guide
- 🌱 **TRANSPORTATION_SAMPLE_DATA.md** - Extended sample data
- 📖 **MODULE_1_COMPLETION_SUMMARY.md** - Full feature list
- 🚨 **TRANSPORTATION_RULES_REFERENCE.md** - Development rules
- 🗺️ **TRANSPORTATION_CORE_MODULES.md** - Module roadmap

---

## ✨ What's Next?

After testing Module 1, we can build **Core Module 2: Ticketing & Booking**:
- Interactive seat selection
- Passenger information
- Promo codes & discounts
- Booking checkout
- E-ticket generation

---

**🎉 Enjoy!** Start at Step 1️⃣ and you'll have a working transportation search in 5 minutes!
