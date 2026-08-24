using Microsoft.EntityFrameworkCore;
using TravelPlanningSystem.Models;
using System.Security.Cryptography;
using System.Text;

namespace TravelPlanningSystem.Data;

public static class SeedData
{
    public static async Task InitializeAsync(AppDbContext context)
    {
        if (!await context.ActivityCategories.AnyAsync())
        {
            context.ActivityCategories.AddRange(
                new ActivityCategory
                {
                    Name = "Adventure",
                    Description = "Exciting outdoor experiences"
                },
                new ActivityCategory
                {
                    Name = "Culture",
                    Description = "Local heritage and cultural discovery"
                },
                new ActivityCategory
                {
                    Name = "Nature",
                    Description = "Nature, wildlife and scenic experiences"
                },
                new ActivityCategory
                {
                    Name = "Food & Dining",
                    Description = "Local culinary activities"
                }
            );

            await context.SaveChangesAsync();
        }

        // --- Seed user and staff data for testing ---
        // Helper to compute a SHA256 hash for password storage (simple test-only hashing)
        static string HashPassword(string pwd)
        {
            var bytes = Encoding.UTF8.GetBytes(pwd);
            var hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash);
        }

        // Seed application users
        if (!await context.Users.AnyAsync())
        {
            context.Users.AddRange(
                new ApplicationUser
                {
                    UserId = Guid.NewGuid(), // <-- ADDED
                    Email = "yong.kq@example.com",
                    PasswordHash = HashPassword("UserPass123!"),
                    FirstName = "Yong",
                    LastName = "Kai Quan",
                    PreferredCurrency = "USD",
                    PreferredLanguage = "en-US",
                    LoyaltyTier = "Gold",
                    RewardPoints = 1200,
                    AccountStatus = "Active", // <-- ADDED
                    CreatedAt = DateTime.UtcNow // <-- ADDED
                },
                new ApplicationUser
                {
                    UserId = Guid.NewGuid(), // <-- ADDED
                    Email = "jane.traveler@example.com",
                    PasswordHash = HashPassword("Traveler456!"),
                    FirstName = "Jane",
                    LastName = "Traveler",
                    PreferredCurrency = "MYR",
                    PreferredLanguage = "en-US",
                    LoyaltyTier = "Member",
                    RewardPoints = 150,
                    AccountStatus = "Active", // <-- ADDED
                    CreatedAt = DateTime.UtcNow // <-- ADDED
                }
            );

            await context.SaveChangesAsync();
        }

        // Seed staff roles and staff users
        if (!await context.StaffRoles.AnyAsync())
        {
            var adminRole = new StaffRole { RoleId = Guid.NewGuid(), RoleName = "Administrator" };
            var supportRole = new StaffRole { RoleId = Guid.NewGuid(), RoleName = "Support" };
            context.StaffRoles.AddRange(adminRole, supportRole);
            await context.SaveChangesAsync();
        }

        if (!await context.StaffUsers.AnyAsync())
        {
            var adminRole = await context.StaffRoles.FirstAsync(r => r.RoleName == "Administrator");
            var supportRole = await context.StaffRoles.FirstAsync(r => r.RoleName == "Support");

            context.StaffUsers.AddRange(
                new StaffUser
                {
                    StaffId = Guid.NewGuid(), // <-- ADDED
                    Email = "admin@travel.test",
                    PasswordHash = HashPassword("AdminPass123!"),
                    FirstName = "System",
                    LastName = "Admin",
                    Department = "Operations",
                    RoleId = adminRole.RoleId,
                    AccessLevel = 10,
                    Status = "Active", // <-- ADDED
                    CreatedAt = DateTime.UtcNow // <-- ADDED
                },
                new StaffUser
                {
                    StaffId = Guid.NewGuid(), // <-- ADDED
                    Email = "support@travel.test",
                    PasswordHash = HashPassword("SupportPass123!"),
                    FirstName = "Support",
                    LastName = "Agent",
                    Department = "Customer Support",
                    RoleId = supportRole.RoleId,
                    AccessLevel = 2,
                    Status = "Active", // <-- ADDED
                    CreatedAt = DateTime.UtcNow // <-- ADDED
                }
            );

            await context.SaveChangesAsync();
        }


        var adventure = await context.ActivityCategories.FirstAsync(c => c.Name == "Adventure");
        var culture = await context.ActivityCategories.FirstAsync(c => c.Name == "Culture");
        var nature = await context.ActivityCategories.FirstAsync(c => c.Name == "Nature");
        var food = await context.ActivityCategories.FirstAsync(c => c.Name == "Food & Dining");

        var activitySeeds = new[]
        {
            new
            {
                Name = "Kuala Lumpur Heritage Walk",
                Destination = "Kuala Lumpur",
                Location = "Merdeka Square",
                Description = "Discover important landmarks, hidden streets and local stories with a friendly licensed guide.",
                Price = 45m,
                Duration = 3.0,
                Min = 1,
                Max = 20,
                Age = 0,
                Included = "Licensed guide",
                Bring = "Comfortable shoes, umbrella",
                CategoryId = culture.ActivityCategoryId,
                Featured = true,
                Photo = "/images/activities/uploads/kl-heritage.jpg",
                Caption = "Kuala Lumpur heritage experience"
            },
            new
            {
                Name = "Langkawi Island Hopping",
                Destination = "Langkawi",
                Location = "Telaga Harbour, Langkawi",
                Description = "Explore beautiful islands, crystal-clear water and scenic beaches on a guided island-hopping experience.",
                Price = 95m,
                Duration = 4.0,
                Min = 1,
                Max = 30,
                Age = 5,
                Included = "Boat transfer, guide, life jacket",
                Bring = "Sunscreen, towel, drinking water",
                CategoryId = nature.ActivityCategoryId,
                Featured = true,
                Photo = "/images/activities/uploads/langkawi-island.jpg",
                Caption = "Langkawi island hopping"
            },
            new
            {
                Name = "Penang Street Food Tour",
                Destination = "George Town",
                Location = "Lebuh Chulia, Penang",
                Description = "Taste famous local dishes while learning about Penang's multicultural food heritage.",
                Price = 128m,
                Duration = 3.5,
                Min = 1,
                Max = 16,
                Age = 8,
                Included = "Food tasting, local guide",
                Bring = "Comfortable shoes and an empty stomach",
                CategoryId = food.ActivityCategoryId,
                Featured = true,
                Photo = "/images/activities/uploads/penang-food.jpg",
                Caption = "Penang street food tour"
            },
            new
            {
                Name = "Sabah River Rafting",
                Destination = "Kota Kinabalu",
                Location = "Kiulu River, Sabah",
                Description = "Enjoy an exciting beginner-friendly rafting adventure surrounded by beautiful tropical scenery.",
                Price = 165m,
                Duration = 5.0,
                Min = 2,
                Max = 24,
                Age = 10,
                Included = "Equipment, instructor, lunch, insurance",
                Bring = "Change of clothes and secure footwear",
                CategoryId = adventure.ActivityCategoryId,
                Featured = true,
                Photo = "/images/activities/uploads/sabah-rafting.jpg",
                Caption = "Sabah river rafting"
            },
            new
            {
                Name = "Melaka Historical Discovery Tour",
                Destination = "Melaka",
                Location = "Dutch Square, Melaka",
                Description = "Explore Melaka's historic streets, colonial landmarks and famous heritage attractions with a local guide.",
                Price = 65m,
                Duration = 3.0,
                Min = 1,
                Max = 20,
                Age = 0,
                Included = "Local guide, heritage route",
                Bring = "Comfortable shoes, drinking water",
                CategoryId = culture.ActivityCategoryId,
                Featured = false,
                Photo = "/images/activities/uploads/melaka-history.jpg",
                Caption = "Melaka historical tour"
            },
            new
            {
                Name = "Cameron Highlands Tea Experience",
                Destination = "Cameron Highlands",
                Location = "Brinchang, Pahang",
                Description = "Visit scenic tea plantations, enjoy cool mountain air and learn about Malaysia's tea production.",
                Price = 78m,
                Duration = 4.0,
                Min = 1,
                Max = 25,
                Age = 0,
                Included = "Guided plantation visit, tea tasting",
                Bring = "Light jacket and camera",
                CategoryId = nature.ActivityCategoryId,
                Featured = true,
                Photo = "/images/activities/uploads/cameron-tea.jpg",
                Caption = "Cameron Highlands tea plantation"
            },
            new
            {
                Name = "KL Tower Sky Experience",
                Destination = "Kuala Lumpur",
                Location = "KL Tower",
                Description = "Enjoy panoramic city views from one of Kuala Lumpur's most iconic observation attractions.",
                Price = 85m,
                Duration = 2.0,
                Min = 1,
                Max = 40,
                Age = 0,
                Included = "Observation deck admission",
                Bring = "Camera",
                CategoryId = culture.ActivityCategoryId,
                Featured = false,
                Photo = "/images/activities/uploads/kl-tower.jpg",
                Caption = "KL Tower city view"
            },
            new
            {
                Name = "Sunway Lagoon Adventure Day",
                Destination = "Selangor",
                Location = "Sunway Lagoon",
                Description = "Spend an exciting day enjoying water attractions, rides and adventure experiences.",
                Price = 190m,
                Duration = 8.0,
                Min = 1,
                Max = 50,
                Age = 5,
                Included = "Theme park admission",
                Bring = "Swimwear, sunscreen, extra clothes",
                CategoryId = adventure.ActivityCategoryId,
                Featured = true,
                Photo = "/images/activities/uploads/sunway-lagoon.jpg",
                Caption = "Sunway Lagoon adventure"
            },
            new
            {
                Name = "Ipoh Cave Temple Discovery",
                Destination = "Ipoh",
                Location = "Kek Lok Tong, Ipoh",
                Description = "Discover beautiful limestone caves, temples and peaceful gardens around Ipoh.",
                Price = 55m,
                Duration = 3.0,
                Min = 1,
                Max = 20,
                Age = 0,
                Included = "Local guide",
                Bring = "Comfortable walking shoes",
                CategoryId = culture.ActivityCategoryId,
                Featured = false,
                Photo = "/images/activities/uploads/ipoh-cave.jpg",
                Caption = "Ipoh cave temple"
            },
            new
            {
                Name = "Kuching Wildlife Experience",
                Destination = "Kuching",
                Location = "Semenggoh Wildlife Centre",
                Description = "Discover Sarawak wildlife and observe orangutans in a protected natural environment.",
                Price = 110m,
                Duration = 4.0,
                Min = 1,
                Max = 18,
                Age = 5,
                Included = "Entrance ticket, guide, transport",
                Bring = "Water, insect repellent, camera",
                CategoryId = nature.ActivityCategoryId,
                Featured = true,
                Photo = "/images/activities/uploads/kuching-wildlife.jpg",
                Caption = "Kuching wildlife experience"
            }
        };

        foreach (var seed in activitySeeds)
        {
            var activity = await context.Activities.FirstOrDefaultAsync(a => a.ActivityName == seed.Name);

            if (activity == null)
            {
                activity = new Activity
                {
                    ActivityName = seed.Name,
                    Destination = seed.Destination,
                    Location = seed.Location,
                    Description = seed.Description,
                    PricePerPerson = seed.Price,
                    DurationHours = seed.Duration,
                    MinimumParticipants = seed.Min,
                    MaximumParticipants = seed.Max,
                    MinimumAge = seed.Age,
                    IncludedItems = seed.Included,
                    WhatToBring = seed.Bring,
                    ActivityCategoryId = seed.CategoryId,
                    IsFeatured = seed.Featured,
                    IsActive = true
                };

                context.Activities.Add(activity);
                await context.SaveChangesAsync();
            }

            var existingPhoto = await context.ActivityPhotos.FirstOrDefaultAsync(p => p.ActivityId == activity.ActivityId && p.IsPrimary);

            if (existingPhoto == null)
            {
                context.ActivityPhotos.Add(
                    new ActivityPhoto
                    {
                        ActivityId = activity.ActivityId,
                        PhotoUrl = seed.Photo,
                        Caption = seed.Caption,
                        IsPrimary = true,
                        DisplayOrder = 0
                    }
                );
            }
            else
            {
                // Update old photo path automatically
                existingPhoto.PhotoUrl = seed.Photo;
                existingPhoto.Caption = seed.Caption;
            }

            await context.SaveChangesAsync();

            await EnsureFutureSessionsAsync(context, activity);
        }

        // Hotel demo data powers the hotel catalogue, availability search, reservations and reviews.
        var hotelSeeds = new[]
        {
            new { Hotel = "The Lakehouse Cameron Highlands", Destination = "Cameron Highlands", Address = "30th Mile, Jalan Ringlet - Sg Koyan, 39000 Ringlet, Pahang", Stars = 4 },
            new { Hotel = "Cameron Highlands Resort", Destination = "Cameron Highlands", Address = "By The Golf Course, Brinchang, 39000 Tanah Rata, Pahang", Stars = 5 },
            new { Hotel = "Zenith Hotel Cameron", Destination = "Cameron Highlands", Address = "Jalan Majlis, 39000 Tanah Rata, Pahang", Stars = 4 },
            new { Hotel = "Copthorne Cameron Highlands", Destination = "Cameron Highlands", Address = "Kea Farm, Brinchang, 39100 Tanah Rata, Pahang", Stars = 4 },
            new { Hotel = "The Smokehouse Hotel & Restaurant", Destination = "Cameron Highlands", Address = "By the Golf Course, Tanah Rata, 39000 Tanah Rata, Pahang", Stars = 4 },
            new { Hotel = "Hotel Malaysia", Destination = "George Town", Address = "7, Jalan Penang, 10000 George Town, Pulau Pinang", Stars = 3 },
            new { Hotel = "Eastern & Oriental Hotel", Destination = "George Town", Address = "10, Lebuh Farquhar, 10200 George Town, Pulau Pinang", Stars = 5 },
            new { Hotel = "The Prestige Hotel Penang", Destination = "George Town", Address = "8, Gat Lebuh Gereja, 10300 George Town, Pulau Pinang", Stars = 5 },
            new { Hotel = "JEN Penang Georgetown by Shangri-La", Destination = "George Town", Address = "Magazine Road, George Town, 10300 George Town, Pulau Pinang", Stars = 4 },
            new { Hotel = "G Hotel Gurney", Destination = "George Town", Address = "168A, Persiaran Gurney, 10250 George Town, Pulau Pinang", Stars = 5 },
            new { Hotel = "Seeds Hotel Danau Kota PV12", Destination = "Kuala Lumpur", Address = "21, Jln PV12, Taman Danau Kota, Setapak, 53300 Kuala Lumpur", Stars = 3 },
            new { Hotel = "Mandarin Oriental, Kuala Lumpur", Destination = "Kuala Lumpur", Address = "Kuala Lumpur City Centre, 50088 Kuala Lumpur", Stars = 5 },
            new { Hotel = "The Ritz-Carlton, Kuala Lumpur", Destination = "Kuala Lumpur", Address = "168, Jalan Imbi, Bukit Bintang, 55100 Kuala Lumpur", Stars = 5 },
            new { Hotel = "Shangri-La Kuala Lumpur", Destination = "Kuala Lumpur", Address = "11, Jalan Sultan Ismail, 50250 Kuala Lumpur", Stars = 5 },
            new { Hotel = "The St. Regis Kuala Lumpur", Destination = "Kuala Lumpur", Address = "No 6, Jalan Stesen Sentral 2, Kuala Lumpur Sentral, 50470 Kuala Lumpur", Stars = 5 },
            new { Hotel = "Hotel Seri Malaysia Langkawi", Destination = "Langkawi", Address = "Lot PT 214 & 215, Mukim Kedawang, Pantai Cenang, 07000 Langkawi, Kedah", Stars = 3 },
            new { Hotel = "PARKROYAL Langkawi Resort", Destination = "Langkawi", Address = "Lot 60199, Pantai Tengah, Bandar Padang Matsirat, 07000 Langkawi, Kedah", Stars = 5 },
            new { Hotel = "The Ritz-Carlton, Langkawi", Destination = "Langkawi", Address = "Jalan Pantai Kok, Teluk Nibong, 07000 Langkawi, Kedah", Stars = 5 },
            new { Hotel = "Pelangi Beach Resort & Spa, Langkawi", Destination = "Langkawi", Address = "Pantai Cenang, 07000 Langkawi, Kedah", Stars = 5 },
            new { Hotel = "Berjaya Langkawi Resort", Destination = "Langkawi", Address = "Karong Berkunci 200, Burau Bay, 07000 Langkawi, Kedah", Stars = 5 },
            new { Hotel = "Hotel Seri Malaysia Melaka", Destination = "Melaka", Address = "Lebuh Ayer Keroh, Bandar Melaka, 75450 Melaka", Stars = 3 },
            new { Hotel = "Hatten Hotel Melaka", Destination = "Melaka", Address = "Hatten Square, Jalan Merdeka, Bandar Hilir, 75000 Melaka", Stars = 5 },
            new { Hotel = "DoubleTree by Hilton Melaka", Destination = "Melaka", Address = "Hatten City, Jalan Melaka Raya 23, 75000 Melaka", Stars = 5 },
            new { Hotel = "Courtyard by Marriott Melaka", Destination = "Melaka", Address = "Lorong Haji Bachee, Kampung Bukit China, 75100 Melaka", Stars = 4 },
            new { Hotel = "The Majestic Malacca Hotel", Destination = "Melaka", Address = "188, Jalan Bunga Raya, 75100 Melaka", Stars = 5 }
        };

        foreach (var seed in hotelSeeds)
        {
            var room = await context.HotelRooms.FirstOrDefaultAsync(r => r.HotelName == seed.Hotel);
            if (room == null)
            {
                room = new HotelRoom
                {
                    HotelName = seed.Hotel, RoomName = "Standard Room", Destination = seed.Destination,
                    Address = seed.Address, Description = "A comfortable hotel stay in a convenient location.",
                    PricePerNight = 120m + (seed.Stars * 75m), Capacity = 2, TotalRooms = 8,
                    StarRating = seed.Stars, IsFeatured = seed.Stars >= 5, IsActive = true,
                    Amenities = "Wi-Fi, Air conditioning, Breakfast, Parking"
                };
                context.HotelRooms.Add(room);
                await context.SaveChangesAsync();
            }

            room.Destination = seed.Destination;
            room.Address = seed.Address;
            room.StarRating = seed.Stars;

            var photos = await context.HotelRoomPhotos.Where(p => p.HotelRoomId == room.HotelRoomId).ToListAsync();
            if (photos.Count == 0)
            {
                context.HotelRoomPhotos.Add(new HotelRoomPhoto { HotelRoomId = room.HotelRoomId, PhotoUrl = "/images/hotels/uploads/no-image.jpg", Caption = "Demo hotel placeholder", IsPrimary = true, DisplayOrder = 0 });
            }
            else if (photos.All(p => p.Caption == room.RoomName || p.Caption == "Hotel ambience"))
            {
                foreach (var photo in photos)
                {
                    photo.PhotoUrl = "/images/hotels/uploads/no-image.jpg";
                    photo.Caption = "Demo hotel placeholder";
                }
                photos[0].IsPrimary = true;
            }
        }

        await context.SaveChangesAsync();

        // One past reviewed stay and one forthcoming stay make both reservation states visible in the UI.
        var reviewRoom = await context.HotelRooms.FirstAsync(r => r.HotelName == "PARKROYAL Langkawi Resort");
        if (!await context.HotelReservations.AnyAsync(r => r.ReservationReference == "HTLDEMO001"))
        {
            var completed = new HotelReservation { ReservationReference = "HTLDEMO001", UserId = 1, HotelRoomId = reviewRoom.HotelRoomId, CheckInDate = DateTime.Today.AddDays(-20), CheckOutDate = DateTime.Today.AddDays(-17), GuestCount = 2, PricePerNight = reviewRoom.PricePerNight, TotalAmount = reviewRoom.PricePerNight * 3, ReservationDate = DateTime.Today.AddDays(-35), ContactName = "Yong Kai Quan", ContactEmail = "yong.kq@example.com", ContactPhone = "012-3456789", ReservationStatus = HotelReservationStatus.Completed };
            context.HotelReservations.Add(completed);
            await context.SaveChangesAsync();
            context.HotelReviews.Add(new HotelReview { HotelRoomId = reviewRoom.HotelRoomId, HotelReservationId = completed.HotelReservationId, UserId = 1, Rating = 5, Comment = "Lovely beach location, attentive staff and a very comfortable room." });
        }

        var upcomingRoom = await context.HotelRooms.FirstAsync(r => r.HotelName == "Mandarin Oriental, Kuala Lumpur");
        if (!await context.HotelReservations.AnyAsync(r => r.ReservationReference == "HTLDEMO002"))
        {
            context.HotelReservations.Add(new HotelReservation { ReservationReference = "HTLDEMO002", UserId = 1, HotelRoomId = upcomingRoom.HotelRoomId, CheckInDate = DateTime.Today.AddDays(14), CheckOutDate = DateTime.Today.AddDays(17), GuestCount = 2, PricePerNight = upcomingRoom.PricePerNight, TotalAmount = upcomingRoom.PricePerNight * 3, ReservationDate = DateTime.Today, ContactName = "Yong Kai Quan", ContactEmail = "yong.kq@example.com", ContactPhone = "012-3456789", ReservationStatus = HotelReservationStatus.Confirmed });
        }

        await context.SaveChangesAsync();
    }

    private static async Task EnsureFutureSessionsAsync(AppDbContext context, Activity activity)
    {
        var existingFutureSessions = await context.ActivitySessions
            .Where(s =>
                s.ActivityId == activity.ActivityId &&
                s.IsActive &&
                s.SessionDate >= DateTime.Today)
            .OrderBy(s => s.SessionDate)
            .ToListAsync();

        // Always maintain at least 6 future sessions
        var sessionsNeeded = Math.Max(0, 6 - existingFutureSessions.Count);

        if (sessionsNeeded == 0)
        {
            return;
        }

        var lastDate = existingFutureSessions.Any()
            ? existingFutureSessions.Max(s => s.SessionDate)
            : DateTime.Today;

        for (var i = 1; i <= sessionsNeeded; i++)
        {
            var sessionDate = lastDate.AddDays(i * 2);
            var startTime = new TimeSpan(9, 0, 0);
            var duration = TimeSpan.FromHours(activity.DurationHours);

            var exists = await context.ActivitySessions
                .AnyAsync(s =>
                    s.ActivityId == activity.ActivityId &&
                    s.SessionDate.Date == sessionDate.Date &&
                    s.StartTime == startTime);

            if (exists)
            {
                continue;
            }

            context.ActivitySessions.Add(
                new ActivitySession
                {
                    ActivityId = activity.ActivityId,
                    SessionDate = sessionDate.Date,
                    StartTime = startTime,
                    EndTime = startTime.Add(duration),
                    Capacity = activity.MaximumParticipants,
                    AvailableSlots = activity.MaximumParticipants,
                    IsActive = true
                }
            );
        }

        await context.SaveChangesAsync();
    }
}
