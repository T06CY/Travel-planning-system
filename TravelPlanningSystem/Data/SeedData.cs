using Microsoft.EntityFrameworkCore;
using TravelPlanningSystem.Models;
using System.Security.Cryptography;
using System.Text;

namespace TravelPlanningSystem.Data;

public static class SeedData
{
    public static async Task InitializeAsync(AppDbContext context)
    {
        // =========================================================
        // ACTIVITY CATEGORIES
        // =========================================================

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


        // =========================================================
        // PASSWORD HASH HELPER
        // =========================================================

        static string HashPassword(string pwd)
        {
            var bytes = Encoding.UTF8.GetBytes(pwd);
            var hash = SHA256.HashData(bytes);

            return Convert.ToHexString(hash);
        }


        // =========================================================
        // APPLICATION USERS
        // =========================================================

        if (!await context.Users.AnyAsync())
        {
            context.Users.AddRange(
                new ApplicationUser
                {
                    UserId = Guid.NewGuid(),
                    Email = "yong.kq@example.com",
                    PasswordHash = HashPassword("UserPass123!"),
                    FirstName = "Yong",
                    LastName = "Kai Quan",
                    PreferredCurrency = "USD",
                    PreferredLanguage = "en-US",
                    LoyaltyTier = "Gold",
                    RewardPoints = 1200,
                    AccountStatus = "Active",
                    CreatedAt = DateTime.UtcNow
                },

                new ApplicationUser
                {
                    UserId = Guid.NewGuid(),
                    Email = "jane.traveler@example.com",
                    PasswordHash = HashPassword("Traveler456!"),
                    FirstName = "Jane",
                    LastName = "Traveler",
                    PreferredCurrency = "MYR",
                    PreferredLanguage = "en-US",
                    LoyaltyTier = "Member",
                    RewardPoints = 150,
                    AccountStatus = "Active",
                    CreatedAt = DateTime.UtcNow
                }
            );

            await context.SaveChangesAsync();
        }


        // =========================================================
        // STAFF ROLES
        // =========================================================

        if (!await context.StaffRoles.AnyAsync())
        {
            var adminRole = new StaffRole
            {
                RoleId = Guid.NewGuid(),
                RoleName = "Administrator"
            };

            var supportRole = new StaffRole
            {
                RoleId = Guid.NewGuid(),
                RoleName = "Support"
            };

            context.StaffRoles.AddRange(
                adminRole,
                supportRole
            );

            await context.SaveChangesAsync();
        }


        // =========================================================
        // STAFF USERS
        // =========================================================

        if (!await context.StaffUsers.AnyAsync())
        {
            var adminRole =
                await context.StaffRoles
                    .FirstAsync(r =>
                        r.RoleName == "Administrator");

            var supportRole =
                await context.StaffRoles
                    .FirstAsync(r =>
                        r.RoleName == "Support");

            context.StaffUsers.AddRange(
                new StaffUser
                {
                    StaffId = Guid.NewGuid(),
                    Email = "admin@travel.test",
                    PasswordHash =
                        HashPassword("AdminPass123!"),
                    FirstName = "System",
                    LastName = "Admin",
                    Department = "Operations",
                    RoleId = adminRole.RoleId,
                    AccessLevel = 10,
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow
                },

                new StaffUser
                {
                    StaffId = Guid.NewGuid(),
                    Email = "support@travel.test",
                    PasswordHash =
                        HashPassword("SupportPass123!"),
                    FirstName = "Support",
                    LastName = "Agent",
                    Department = "Customer Support",
                    RoleId = supportRole.RoleId,
                    AccessLevel = 2,
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow
                }
            );

            await context.SaveChangesAsync();
        }

        // Airline records, airports and rolling future flight schedules.
        await SeedAirlineReservationAsync(context);


        // =========================================================
        // GET ACTIVITY CATEGORIES
        // =========================================================

        var adventure =
            await context.ActivityCategories
                .FirstAsync(c =>
                    c.Name == "Adventure");

        var culture =
            await context.ActivityCategories
                .FirstAsync(c =>
                    c.Name == "Culture");

        var nature =
            await context.ActivityCategories
                .FirstAsync(c =>
                    c.Name == "Nature");

        var food =
            await context.ActivityCategories
                .FirstAsync(c =>
                    c.Name == "Food & Dining");


        // =========================================================
        // ACTIVITY SEED DATA
        // =========================================================

        var activitySeeds = new[]
        {
            new
            {
                Name = "Kuala Lumpur Heritage Walk",
                Destination = "Kuala Lumpur",
                Location = "Merdeka Square",
                Description =
                    "Discover important landmarks, hidden streets and local stories with a friendly licensed guide.",
                Price = 45m,
                Duration = 3.0,
                Min = 1,
                Max = 20,
                Age = 0,
                Included = "Licensed guide",
                Bring = "Comfortable shoes, umbrella",
                CategoryId = culture.ActivityCategoryId,
                Featured = true,
                Photo =
                    "/images/activities/uploads/kl-heritage.jpg",
                Caption =
                    "Kuala Lumpur heritage experience"
            },

            new
            {
                Name = "Langkawi Island Hopping",
                Destination = "Langkawi",
                Location = "Telaga Harbour, Langkawi",
                Description =
                    "Explore beautiful islands, crystal-clear water and scenic beaches on a guided island-hopping experience.",
                Price = 95m,
                Duration = 4.0,
                Min = 1,
                Max = 30,
                Age = 5,
                Included =
                    "Boat transfer, guide, life jacket",
                Bring =
                    "Sunscreen, towel, drinking water",
                CategoryId = nature.ActivityCategoryId,
                Featured = true,
                Photo =
                    "/images/activities/uploads/langkawi-island.jpg",
                Caption =
                    "Langkawi island hopping"
            },

            new
            {
                Name = "Penang Street Food Tour",
                Destination = "George Town",
                Location = "Lebuh Chulia, Penang",
                Description =
                    "Taste famous local dishes while learning about Penang's multicultural food heritage.",
                Price = 128m,
                Duration = 3.5,
                Min = 1,
                Max = 16,
                Age = 8,
                Included =
                    "Food tasting, local guide",
                Bring =
                    "Comfortable shoes and an empty stomach",
                CategoryId = food.ActivityCategoryId,
                Featured = true,
                Photo =
                    "/images/activities/uploads/penang-food.jpg",
                Caption =
                    "Penang street food tour"
            },

            new
            {
                Name = "Sabah River Rafting",
                Destination = "Kota Kinabalu",
                Location = "Kiulu River, Sabah",
                Description =
                    "Enjoy an exciting beginner-friendly rafting adventure surrounded by beautiful tropical scenery.",
                Price = 165m,
                Duration = 5.0,
                Min = 2,
                Max = 24,
                Age = 10,
                Included =
                    "Equipment, instructor, lunch, insurance",
                Bring =
                    "Change of clothes and secure footwear",
                CategoryId = adventure.ActivityCategoryId,
                Featured = true,
                Photo =
                    "/images/activities/uploads/sabah-rafting.jpg",
                Caption =
                    "Sabah river rafting"
            },

            new
            {
                Name = "Melaka Historical Discovery Tour",
                Destination = "Melaka",
                Location = "Dutch Square, Melaka",
                Description =
                    "Explore Melaka's historic streets, colonial landmarks and famous heritage attractions with a local guide.",
                Price = 65m,
                Duration = 3.0,
                Min = 1,
                Max = 20,
                Age = 0,
                Included =
                    "Local guide, heritage route",
                Bring =
                    "Comfortable shoes, drinking water",
                CategoryId = culture.ActivityCategoryId,
                Featured = false,
                Photo =
                    "/images/activities/uploads/melaka-history.jpg",
                Caption =
                    "Melaka historical tour"
            },

            new
            {
                Name =
                    "Cameron Highlands Tea Experience",
                Destination =
                    "Cameron Highlands",
                Location =
                    "Brinchang, Pahang",
                Description =
                    "Visit scenic tea plantations, enjoy cool mountain air and learn about Malaysia's tea production.",
                Price = 78m,
                Duration = 4.0,
                Min = 1,
                Max = 25,
                Age = 0,
                Included =
                    "Guided plantation visit, tea tasting",
                Bring =
                    "Light jacket and camera",
                CategoryId = nature.ActivityCategoryId,
                Featured = true,
                Photo =
                    "/images/activities/uploads/cameron-tea.jpg",
                Caption =
                    "Cameron Highlands tea plantation"
            },

            new
            {
                Name =
                    "KL Tower Sky Experience",
                Destination =
                    "Kuala Lumpur",
                Location =
                    "KL Tower",
                Description =
                    "Enjoy panoramic city views from one of Kuala Lumpur's most iconic observation attractions.",
                Price = 85m,
                Duration = 2.0,
                Min = 1,
                Max = 40,
                Age = 0,
                Included =
                    "Observation deck admission",
                Bring =
                    "Camera",
                CategoryId = culture.ActivityCategoryId,
                Featured = false,
                Photo =
                    "/images/activities/uploads/kl-tower.jpg",
                Caption =
                    "KL Tower city view"
            },

            new
            {
                Name =
                    "Sunway Lagoon Adventure Day",
                Destination =
                    "Selangor",
                Location =
                    "Sunway Lagoon",
                Description =
                    "Spend an exciting day enjoying water attractions, rides and adventure experiences.",
                Price = 190m,
                Duration = 8.0,
                Min = 1,
                Max = 50,
                Age = 5,
                Included =
                    "Theme park admission",
                Bring =
                    "Swimwear, sunscreen, extra clothes",
                CategoryId = adventure.ActivityCategoryId,
                Featured = true,
                Photo =
                    "/images/activities/uploads/sunway-lagoon.jpg",
                Caption =
                    "Sunway Lagoon adventure"
            },

            new
            {
                Name =
                    "Ipoh Cave Temple Discovery",
                Destination =
                    "Ipoh",
                Location =
                    "Kek Lok Tong, Ipoh",
                Description =
                    "Discover beautiful limestone caves, temples and peaceful gardens around Ipoh.",
                Price = 55m,
                Duration = 3.0,
                Min = 1,
                Max = 20,
                Age = 0,
                Included =
                    "Local guide",
                Bring =
                    "Comfortable walking shoes",
                CategoryId = culture.ActivityCategoryId,
                Featured = false,
                Photo =
                    "/images/activities/uploads/ipoh-cave.jpg",
                Caption =
                    "Ipoh cave temple"
            },

            new
            {
                Name =
                    "Kuching Wildlife Experience",
                Destination =
                    "Kuching",
                Location =
                    "Semenggoh Wildlife Centre",
                Description =
                    "Discover Sarawak wildlife and observe orangutans in a protected natural environment.",
                Price = 110m,
                Duration = 4.0,
                Min = 1,
                Max = 18,
                Age = 5,
                Included =
                    "Entrance ticket, guide, transport",
                Bring =
                    "Water, insect repellent, camera",
                CategoryId = nature.ActivityCategoryId,
                Featured = true,
                Photo =
                    "/images/activities/uploads/kuching-wildlife.jpg",
                Caption =
                    "Kuching wildlife experience"
            }
        };


        // =========================================================
        // CREATE / UPDATE ACTIVITIES
        // =========================================================

        foreach (var seed in activitySeeds)
        {
            var activity =
                await context.Activities
                    .FirstOrDefaultAsync(a =>
                        a.ActivityName == seed.Name);

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
                    ActivityCategoryId =
                        seed.CategoryId,
                    IsFeatured = seed.Featured,
                    IsActive = true
                };

                context.Activities.Add(activity);

                await context.SaveChangesAsync();
            }


            var existingPhoto =
                await context.ActivityPhotos
                    .FirstOrDefaultAsync(p =>
                        p.ActivityId ==
                            activity.ActivityId &&
                        p.IsPrimary);

            if (existingPhoto == null)
            {
                context.ActivityPhotos.Add(
                    new ActivityPhoto
                    {
                        ActivityId =
                            activity.ActivityId,
                        PhotoUrl =
                            seed.Photo,
                        Caption =
                            seed.Caption,
                        IsPrimary =
                            true,
                        DisplayOrder =
                            0
                    }
                );
            }
            else
            {
                existingPhoto.PhotoUrl =
                    seed.Photo;

                existingPhoto.Caption =
                    seed.Caption;
            }

            await context.SaveChangesAsync();

            await EnsureFutureSessionsAsync(
                context,
                activity
            );
        }


        // =========================================================
        // HOTEL SEED DATA
        // 25 HOTELS WITH DIFFERENT PRICES
        // =========================================================

        var hotelSeeds = new[]
        {
            new
            {
                Hotel = "The Lakehouse Cameron Highlands",
                Destination = "Cameron Highlands",
                Address = "30th Mile, Jalan Ringlet - Sg Koyan, 39000 Ringlet, Pahang",
                Stars = 4,
                Price = 380m,
                Photo = "/images/hotels/uploads/224a8e98dfc746dba24df4f82f765cbe.jpg"
            },

            new
            {
                Hotel = "Cameron Highlands Resort",
                Destination = "Cameron Highlands",
                Address = "By The Golf Course, Brinchang, 39000 Tanah Rata, Pahang",
                Stars = 5,
                Price = 495m,
                Photo = "/images/hotels/uploads/2672403677854a46b231d007a3639bf4.jpg"
            },

            new
            {
                Hotel = "Zenith Hotel Cameron",
                Destination = "Cameron Highlands",
                Address = "Jalan Majlis, 39000 Tanah Rata, Pahang",
                Stars = 4,
                Price = 350m,
                Photo = "/images/hotels/uploads/2690fe66a9444f5f98881b8f4bcd726f.jpg"
            },

            new
            {
                Hotel = "Copthorne Cameron Highlands",
                Destination = "Cameron Highlands",
                Address = "Kea Farm, Brinchang, 39100 Tanah Rata, Pahang",
                Stars = 4,
                Price = 420m,
                Photo = "/images/hotels/uploads/2a5b7574315142108faffb0fefb1cc31.jpg"
            },

            new
            {
                Hotel = "The Smokehouse Hotel & Restaurant",
                Destination = "Cameron Highlands",
                Address = "By the Golf Course, Tanah Rata, 39000 Tanah Rata, Pahang",
                Stars = 4,
                Price = 460m,
                Photo = "/images/hotels/uploads/31110e3aecfb4f74bd0cafb513c2cb4e.jpg"
            },

            new
            {
                Hotel = "Hotel Malaysia",
                Destination = "George Town",
                Address = "7, Jalan Penang, 10000 George Town, Pulau Pinang",
                Stars = 3,
                Price = 210m,
                Photo = "/images/hotels/uploads/3355a4d9d6e34195a7b354aab1aa7d5c.jpg"
            },

            new
            {
                Hotel = "Eastern & Oriental Hotel",
                Destination = "George Town",
                Address = "10, Lebuh Farquhar, 10200 George Town, Pulau Pinang",
                Stars = 5,
                Price = 650m,
                Photo = "/images/hotels/uploads/489406cce2f749e5a02f0880d8ea545f.jpg"
            },

            new
            {
                Hotel = "The Prestige Hotel Penang",
                Destination = "George Town",
                Address = "8, Gat Lebuh Gereja, 10300 George Town, Pulau Pinang",
                Stars = 5,
                Price = 520m,
                Photo = "/images/hotels/uploads/49ab4ccd7c6747e693a4d5185a2f4025.jpg"
            },

            new
            {
                Hotel = "JEN Penang Georgetown by Shangri-La",
                Destination = "George Town",
                Address = "Magazine Road, George Town, 10300 George Town, Pulau Pinang",
                Stars = 4,
                Price = 390m,
                Photo = "/images/hotels/uploads/4f8b4acbaf92496eb508a144ec40cba9.jpg"
            },

            new
            {
                Hotel = "G Hotel Gurney",
                Destination = "George Town",
                Address = "168A, Persiaran Gurney, 10250 George Town, Pulau Pinang",
                Stars = 5,
                Price = 580m,
                Photo = "/images/hotels/uploads/545d1cfa0d6743988c2754861f6bdd0a.jpg"
            },

            new
            {
                Hotel = "Seeds Hotel Danau Kota PV12",
                Destination = "Kuala Lumpur",
                Address = "21, Jln PV12, Taman Danau Kota, Setapak, 53300 Kuala Lumpur",
                Stars = 3,
                Price = 150m,
                Photo = "/images/hotels/uploads/687b707519b849a1b530af8721e9d20f.jpg"
            },

            new
            {
                Hotel = "Mandarin Oriental, Kuala Lumpur",
                Destination = "Kuala Lumpur",
                Address = "Kuala Lumpur City Centre, 50088 Kuala Lumpur",
                Stars = 5,
                Price = 720m,
                Photo = "/images/hotels/uploads/6edf0b954f5f43c0a875353857495208.jpg"
            },

            new
            {
                Hotel = "The Ritz-Carlton, Kuala Lumpur",
                Destination = "Kuala Lumpur",
                Address = "168, Jalan Imbi, Bukit Bintang, 55100 Kuala Lumpur",
                Stars = 5,
                Price = 850m,
                Photo = "/images/hotels/uploads/7ac69f4be5454b9f96b9f399c3f8c9c4.jpg"
            },

            new
            {
                Hotel = "Shangri-La Kuala Lumpur",
                Destination = "Kuala Lumpur",
                Address = "11, Jalan Sultan Ismail, 50250 Kuala Lumpur",
                Stars = 5,
                Price = 620m,
                Photo = "/images/hotels/uploads/815fc99d23ef4ce7979ae530e136f20f.jpg"
            },

            new
            {
                Hotel = "The St. Regis Kuala Lumpur",
                Destination = "Kuala Lumpur",
                Address = "No 6, Jalan Stesen Sentral 2, Kuala Lumpur Sentral, 50470 Kuala Lumpur",
                Stars = 5,
                Price = 950m,
                Photo = "/images/hotels/uploads/8980f952fde74d01a50d430f1885df96.jpg"
            },

            new
            {
                Hotel = "Hotel Seri Malaysia Langkawi",
                Destination = "Langkawi",
                Address = "Lot PT 214 & 215, Mukim Kedawang, Pantai Cenang, 07000 Langkawi, Kedah",
                Stars = 3,
                Price = 230m,
                Photo = "/images/hotels/uploads/8bef0e971efb4076828b5ca3928e32ff.jpg"
            },

            new
            {
                Hotel = "PARKROYAL Langkawi Resort",
                Destination = "Langkawi",
                Address = "Lot 60199, Pantai Tengah, Bandar Padang Matsirat, 07000 Langkawi, Kedah",
                Stars = 5,
                Price = 680m,
                Photo = "/images/hotels/uploads/940a3b87093c4f9793cf6bccfbf77899.jpg"
            },

            new
            {
                Hotel = "The Ritz-Carlton, Langkawi",
                Destination = "Langkawi",
                Address = "Jalan Pantai Kok, Teluk Nibong, 07000 Langkawi, Kedah",
                Stars = 5,
                Price = 1200m,
                Photo = "/images/hotels/uploads/9638b54996dc465cb0030ae88a8253b6.jpg"
            },

            new
            {
                Hotel = "Pelangi Beach Resort & Spa, Langkawi",
                Destination = "Langkawi",
                Address = "Pantai Cenang, 07000 Langkawi, Kedah",
                Stars = 5,
                Price = 750m,
                Photo = "/images/hotels/uploads/9d0214db17764ec18f479552863de5d8.jpg"
            },

            new
            {
                Hotel = "Berjaya Langkawi Resort",
                Destination = "Langkawi",
                Address = "Karong Berkunci 200, Burau Bay, 07000 Langkawi, Kedah",
                Stars = 5,
                Price = 560m,
                Photo = "/images/hotels/uploads/a4db4c783b4f4360aa46c84cbc152664.jpg"
            },

            new
            {
                Hotel = "Hotel Seri Malaysia Melaka",
                Destination = "Melaka",
                Address = "Lebuh Ayer Keroh, Bandar Melaka, 75450 Melaka",
                Stars = 3,
                Price = 180m,
                Photo = "/images/hotels/uploads/aca0165c7c39499382212eea7a5513bb.jpg"
            },

            new
            {
                Hotel = "Hatten Hotel Melaka",
                Destination = "Melaka",
                Address = "Hatten Square, Jalan Merdeka, Bandar Hilir, 75000 Melaka",
                Stars = 5,
                Price = 450m,
                Photo = "/images/hotels/uploads/b911b00322464a079d7b13598a8b05ad.jpg"
            },

            new
            {
                Hotel = "DoubleTree by Hilton Melaka",
                Destination = "Melaka",
                Address = "Hatten City, Jalan Melaka Raya 23, 75000 Melaka",
                Stars = 5,
                Price = 520m,
                Photo = "/images/hotels/uploads/ba2f1ea081714bc597867d7b9f714828.jpg"
            },

            new
            {
                Hotel = "Courtyard by Marriott Melaka",
                Destination = "Melaka",
                Address = "Lorong Haji Bachee, Kampung Bukit China, 75100 Melaka",
                Stars = 4,
                Price = 410m,
                Photo = "/images/hotels/uploads/c6e394129dc04958b8c5278c86825cfa.jpg"
            },

            new
            {
                Hotel = "The Majestic Malacca Hotel",
                Destination = "Melaka",
                Address = "188, Jalan Bunga Raya, 75100 Melaka",
                Stars = 5,
                Price = 590m,
                Photo = "/images/hotels/uploads/e880e6c7df474cec8a757122c1134d9f.jpg"
            }
        };


        // =========================================================
        // CREATE / UPDATE HOTEL ROOMS
        // =========================================================

        foreach (var seed in hotelSeeds)
        {
            var room =
                await context.HotelRooms
                    .FirstOrDefaultAsync(r =>
                        r.HotelName == seed.Hotel);

            if (room == null)
            {
                room = new HotelRoom
                {
                    HotelName =
                        seed.Hotel,

                    RoomName =
                        "Standard Room",

                    RoomType =
                        "Standard Room",

                    Destination =
                        seed.Destination,

                    Address =
                        seed.Address,

                    Description =
                        "A comfortable hotel stay in a convenient location.",

                    PricePerNight =
                        seed.Price,

                    Capacity =
                        2,

                    TotalRooms =
                        8,

                    StarRating =
                        seed.Stars,

                    IsFeatured =
                        seed.Stars >= 5,

                    IsActive =
                        true,

                    Amenities =
                        "Wi-Fi, Air conditioning, Breakfast, Parking"
                };

                context.HotelRooms.Add(room);

                await context.SaveChangesAsync();
            }


            // =====================================================
            // UPDATE EXISTING HOTEL DATA
            // =====================================================

            room.Destination =
                seed.Destination;

            room.Address =
                seed.Address;

            room.StarRating =
                seed.Stars;

            // IMPORTANT:
            // Update price even if room already exists.
            room.PricePerNight =
                seed.Price;


            if (string.IsNullOrWhiteSpace(
                    room.RoomType))
            {
                room.RoomType =
                    "Standard Room";
            }


            // =====================================================
            // HOTEL PHOTOS
            // =====================================================

            var photos =
                await context.HotelRoomPhotos
                    .Where(p =>
                        p.HotelRoomId ==
                            room.HotelRoomId)
                    .ToListAsync();


            if (photos.Count == 0)
            {
                context.HotelRoomPhotos.Add(
                    new HotelRoomPhoto
                    {
                        HotelRoomId =
                            room.HotelRoomId,

                        PhotoUrl =
                            seed.Photo,

                        Caption =
                            seed.Hotel,

                        IsPrimary =
                            true,

                        DisplayOrder =
                            0
                    }
                );
            }
            else
            {
                // Keep the existing record but point it to the seeded hotel photo.
                // This also replaces the old no-image.jpg placeholder.
                var primaryPhoto = photos
                    .OrderByDescending(p => p.IsPrimary)
                    .ThenBy(p => p.DisplayOrder)
                    .First();

                primaryPhoto.PhotoUrl =
                    seed.Photo;

                primaryPhoto.Caption =
                    seed.Hotel;

                primaryPhoto.IsPrimary =
                    true;

                primaryPhoto.DisplayOrder =
                    0;
            }
        }


        await context.SaveChangesAsync();


        // =========================================================
        // COMPLETED HOTEL RESERVATION DEMO
        // =========================================================

        var reviewRoom =
            await context.HotelRooms
                .FirstAsync(r =>
                    r.HotelName ==
                    "PARKROYAL Langkawi Resort");


        if (!await context.HotelReservations
            .AnyAsync(r =>
                r.ReservationReference ==
                "HTLDEMO001"))
        {
            var completed =
                new HotelReservation
                {
                    ReservationReference =
                        "HTLDEMO001",

                    UserId =
                        1,

                    HotelRoomId =
                        reviewRoom.HotelRoomId,

                    CheckInDate =
                        DateTime.Today.AddDays(-20),

                    CheckOutDate =
                        DateTime.Today.AddDays(-17),

                    GuestCount =
                        2,

                    PricePerNight =
                        reviewRoom.PricePerNight,

                    TotalAmount =
                        reviewRoom.PricePerNight * 3,

                    ReservationDate =
                        DateTime.Today.AddDays(-35),

                    ContactName =
                        "Yong Kai Quan",

                    ContactEmail =
                        "yong.kq@example.com",

                    ContactPhone =
                        "012-3456789",

                    ReservationStatus =
                        HotelReservationStatus.Completed
                };


            context.HotelReservations.Add(
                completed
            );

            await context.SaveChangesAsync();


            context.HotelReviews.Add(
                new HotelReview
                {
                    HotelRoomId =
                        reviewRoom.HotelRoomId,

                    HotelReservationId =
                        completed.HotelReservationId,

                    UserId =
                        1,

                    Rating =
                        5,

                    Comment =
                        "Lovely beach location, attentive staff and a very comfortable room."
                }
            );
        }


        // =========================================================
        // UPCOMING HOTEL RESERVATION DEMO
        // =========================================================

        var upcomingRoom =
            await context.HotelRooms
                .FirstAsync(r =>
                    r.HotelName ==
                    "Mandarin Oriental, Kuala Lumpur");


        if (!await context.HotelReservations
            .AnyAsync(r =>
                r.ReservationReference ==
                "HTLDEMO002"))
        {
            context.HotelReservations.Add(
                new HotelReservation
                {
                    ReservationReference =
                        "HTLDEMO002",

                    UserId =
                        1,

                    HotelRoomId =
                        upcomingRoom.HotelRoomId,

                    CheckInDate =
                        DateTime.Today.AddDays(14),

                    CheckOutDate =
                        DateTime.Today.AddDays(17),

                    GuestCount =
                        2,

                    PricePerNight =
                        upcomingRoom.PricePerNight,

                    TotalAmount =
                        upcomingRoom.PricePerNight * 3,

                    ReservationDate =
                        DateTime.Today,

                    ContactName =
                        "Yong Kai Quan",

                    ContactEmail =
                        "yong.kq@example.com",

                    ContactPhone =
                        "012-3456789",

                    ReservationStatus =
                        HotelReservationStatus.Confirmed
                }
            );
        }


        await context.SaveChangesAsync();
    }


    // =============================================================
    // ENSURE FUTURE ACTIVITY SESSIONS
    // =============================================================

    private static async Task EnsureFutureSessionsAsync(
        AppDbContext context,
        Activity activity)
    {
        var existingFutureSessions =
            await context.ActivitySessions
                .Where(s =>
                    s.ActivityId ==
                        activity.ActivityId &&
                    s.IsActive &&
                    s.SessionDate >=
                        DateTime.Today)
                .OrderBy(s =>
                    s.SessionDate)
                .ToListAsync();


        // Always maintain at least 6 future sessions.
        var sessionsNeeded =
            Math.Max(
                0,
                6 - existingFutureSessions.Count
            );


        if (sessionsNeeded == 0)
        {
            return;
        }


        var lastDate =
            existingFutureSessions.Any()
                ? existingFutureSessions
                    .Max(s =>
                        s.SessionDate)
                : DateTime.Today;


        for (var i = 1;
             i <= sessionsNeeded;
             i++)
        {
            var sessionDate =
                lastDate.AddDays(i * 2);


            var startTime =
                new TimeSpan(
                    9,
                    0,
                    0
                );


            var duration =
                TimeSpan.FromHours(
                    activity.DurationHours
                );


            var exists =
                await context.ActivitySessions
                    .AnyAsync(s =>
                        s.ActivityId ==
                            activity.ActivityId &&
                        s.SessionDate.Date ==
                            sessionDate.Date &&
                        s.StartTime ==
                            startTime);


            if (exists)
            {
                continue;
            }


            context.ActivitySessions.Add(
                new ActivitySession
                {
                    ActivityId =
                        activity.ActivityId,

                    SessionDate =
                        sessionDate.Date,

                    StartTime =
                        startTime,

                    EndTime =
                        startTime.Add(duration),

                    Capacity =
                        activity.MaximumParticipants,

                    AvailableSlots =
                        activity.MaximumParticipants,

                    IsActive =
                        true
                }
            );
        }


        await context.SaveChangesAsync();
    }


    // =============================================================
    // AIRLINE RESERVATION SEED DATA
    // =============================================================

    private static async Task SeedAirlineReservationAsync(
        AppDbContext context)
    {
        if (!await context.Airlines.AnyAsync())
        {
            context.Airlines.AddRange(
                new Airline
                {
                    AirlineCode = "MH",
                    AirlineName = "Malaysia Airlines",
                    Country = "Malaysia",
                    LogoUrl = "/images/flights/MalaysiaAirlines.png",
                    IsActive = true
                },
                new Airline
                {
                    AirlineCode = "AK",
                    AirlineName = "AirAsia",
                    Country = "Malaysia",
                    LogoUrl = "/images/flights/AirAsia.png",
                    IsActive = true
                },
                new Airline
                {
                    AirlineCode = "OD",
                    AirlineName = "Batik Air Malaysia",
                    Country = "Malaysia",
                    LogoUrl = "/images/flights/BatikAir.png",
                    IsActive = true
                },
                new Airline
                {
                    AirlineCode = "SQ",
                    AirlineName = "Singapore Airlines",
                    Country = "Singapore",
                    LogoUrl = "/images/flights/SingaporeAirlines.png",
                    IsActive = true
                },
                new Airline
                {
                    AirlineCode = "TG",
                    AirlineName = "Thai Airways",
                    Country = "Thailand",
                    LogoUrl = "/images/flights/ThaiAirways.png",
                    IsActive = true
                },
                new Airline
                {
                    AirlineCode = "JL",
                    AirlineName = "Japan Airlines",
                    Country = "Japan",
                    LogoUrl = "/images/flights/JapanAirlines.png",
                    IsActive = true
                }
            );

            await context.SaveChangesAsync();
        }

        if (!await context.Airports.AnyAsync())
        {
            context.Airports.AddRange(
                new Airport
                {
                    AirportCode = "KUL",
                    AirportName = "Kuala Lumpur International Airport",
                    City = "Kuala Lumpur",
                    Country = "Malaysia"
                },
                new Airport
                {
                    AirportCode = "PEN",
                    AirportName = "Penang International Airport",
                    City = "Penang",
                    Country = "Malaysia"
                },
                new Airport
                {
                    AirportCode = "JHB",
                    AirportName = "Senai International Airport",
                    City = "Johor Bahru",
                    Country = "Malaysia"
                },
                new Airport
                {
                    AirportCode = "KCH",
                    AirportName = "Kuching International Airport",
                    City = "Kuching",
                    Country = "Malaysia"
                },
                new Airport
                {
                    AirportCode = "BKI",
                    AirportName = "Kota Kinabalu International Airport",
                    City = "Kota Kinabalu",
                    Country = "Malaysia"
                },
                new Airport
                {
                    AirportCode = "LGK",
                    AirportName = "Langkawi International Airport",
                    City = "Langkawi",
                    Country = "Malaysia"
                },
                new Airport
                {
                    AirportCode = "SDK",
                    AirportName = "Sandakan Airport",
                    City = "Sandakan",
                    Country = "Malaysia"
                },
                new Airport
                {
                    AirportCode = "TWU",
                    AirportName = "Tawau Airport",
                    City = "Tawau",
                    Country = "Malaysia"
                },
                new Airport
                {
                    AirportCode = "SIN",
                    AirportName = "Singapore Changi Airport",
                    City = "Singapore",
                    Country = "Singapore"
                },
                new Airport
                {
                    AirportCode = "BKK",
                    AirportName = "Suvarnabhumi Airport",
                    City = "Bangkok",
                    Country = "Thailand"
                },
                new Airport
                {
                    AirportCode = "CGK",
                    AirportName = "Soekarno-Hatta International Airport",
                    City = "Jakarta",
                    Country = "Indonesia"
                },
                new Airport
                {
                    AirportCode = "DPS",
                    AirportName = "Ngurah Rai International Airport",
                    City = "Bali",
                    Country = "Indonesia"
                },
                new Airport
                {
                    AirportCode = "SGN",
                    AirportName = "Tan Son Nhat International Airport",
                    City = "Ho Chi Minh City",
                    Country = "Vietnam"
                },
                new Airport
                {
                    AirportCode = "MNL",
                    AirportName = "Ninoy Aquino International Airport",
                    City = "Manila",
                    Country = "Philippines"
                },
                new Airport
                {
                    AirportCode = "NRT",
                    AirportName = "Narita International Airport",
                    City = "Tokyo",
                    Country = "Japan"
                },
                new Airport
                {
                    AirportCode = "ICN",
                    AirportName = "Incheon International Airport",
                    City = "Seoul",
                    Country = "South Korea"
                },
                new Airport
                {
                    AirportCode = "HKG",
                    AirportName = "Hong Kong International Airport",
                    City = "Hong Kong",
                    Country = "Hong Kong"
                },
                new Airport
                {
                    AirportCode = "TPE",
                    AirportName = "Taiwan Taoyuan International Airport",
                    City = "Taipei",
                    Country = "Taiwan"
                }
            );

            await context.SaveChangesAsync();
        }

        // Remove expired demonstration schedules only when they have no
        // reservation history. Booked flights remain for Booking History.
        var expiredUnbookedFlights = await context.Flights
            .Where(f =>
                f.DepartureTime < DateTime.Today &&
                !f.BookingSegments.Any())
            .ToListAsync();

        if (expiredUnbookedFlights.Count > 0)
        {
            context.Flights.RemoveRange(expiredUnbookedFlights);
            await context.SaveChangesAsync();
        }

        var airlines = await context.Airlines
            .Where(a => a.IsActive)
            .OrderBy(a => a.AirlineId)
            .ToListAsync();

        if (airlines.Count == 0)
        {
            return;
        }

        var routes = new[]
        {
        new { From = "Kuala Lumpur", To = "Penang", Minutes = 60, Price = 159m },
        new { From = "Kuala Lumpur", To = "Johor Bahru", Minutes = 55, Price = 149m },
        new { From = "Kuala Lumpur", To = "Kuching", Minutes = 110, Price = 269m },
        new { From = "Kuala Lumpur", To = "Kota Kinabalu", Minutes = 160, Price = 359m },
        new { From = "Kuala Lumpur", To = "Langkawi", Minutes = 70, Price = 189m },
        new { From = "Kuala Lumpur", To = "Singapore", Minutes = 70, Price = 249m },
        new { From = "Kuala Lumpur", To = "Bangkok", Minutes = 130, Price = 329m },
        new { From = "Kuala Lumpur", To = "Jakarta", Minutes = 125, Price = 349m },
        new { From = "Kuala Lumpur", To = "Bali", Minutes = 180, Price = 499m },
        new { From = "Kuala Lumpur", To = "Ho Chi Minh City", Minutes = 120, Price = 329m },
        new { From = "Kuala Lumpur", To = "Manila", Minutes = 235, Price = 599m },
        new { From = "Kuala Lumpur", To = "Tokyo", Minutes = 420, Price = 1299m },
        new { From = "Kuala Lumpur", To = "Seoul", Minutes = 390, Price = 1199m },
        new { From = "Kuala Lumpur", To = "Hong Kong", Minutes = 240, Price = 699m },
        new { From = "Kuala Lumpur", To = "Taipei", Minutes = 285, Price = 799m },
        new { From = "Singapore", To = "Bangkok", Minutes = 150, Price = 459m },
        new { From = "Bangkok", To = "Tokyo", Minutes = 360, Price = 1299m },
        new { From = "Kota Kinabalu", To = "Sandakan", Minutes = 50, Price = 139m },
        new { From = "Kota Kinabalu", To = "Tawau", Minutes = 55, Price = 159m },
        new { From = "Kuala Lumpur", To = "Hanoi", Minutes = 185, Price = 429m },
        new { From = "Kuala Lumpur", To = "Chennai", Minutes = 205, Price = 549m },
        new { From = "Kuala Lumpur", To = "Perth", Minutes = 330, Price = 899m },
        new { From = "Kuala Lumpur", To = "Sydney", Minutes = 500, Price = 1399m },
        new { From = "Kuala Lumpur", To = "Shanghai", Minutes = 300, Price = 999m },
        new { From = "Singapore", To = "Bali", Minutes = 160, Price = 399m },
        new { From = "Singapore", To = "Seoul", Minutes = 390, Price = 1099m },
        new { From = "Penang", To = "Singapore", Minutes = 80, Price = 299m },
        new { From = "Johor Bahru", To = "Kota Kinabalu", Minutes = 155, Price = 399m }
    };

        var existingScheduleKeys = (await context.Flights
            .AsNoTracking()
            .Where(f => f.DepartureTime >= DateTime.Today)
            .Select(f => new
            {
                f.FlightNumber,
                f.DepartureTime
            })
            .ToListAsync())
            .Select(f => $"{f.FlightNumber}|{f.DepartureTime.Ticks}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Maintain 21 future days. No fixed 2026 date can become stale.
        for (var day = 1; day <= 21; day++)
        {
            var scheduleDate = DateTime.Today.AddDays(day);

            for (var routeIndex = 0; routeIndex < routes.Length; routeIndex++)
            {
                var route = routes[routeIndex];

                var dailyFlightCount = route.From == "Kuala Lumpur" && route.To == "Kota Kinabalu" ? 4 : 1;

                for (var direction = 0; direction < 2; direction++)
                {
                    var from =
                        direction == 0 ? route.From : route.To;

                    var to =
                        direction == 0 ? route.To : route.From;

                    for (var flightIndex = 0; flightIndex < dailyFlightCount; flightIndex++)
                    {
                        var airline =
                            airlines[(routeIndex + direction + flightIndex) % airlines.Count];

                        var flightNumber =
                            $"{airline.AirlineCode}{100 + routeIndex * 2 + direction + flightIndex * 10}";

                        var hour = direction == 0
                            ? 7 + routeIndex % 6 + flightIndex * 3
                            : 14 + routeIndex % 6 + flightIndex * 3;

                        var minute = (routeIndex * 10) % 60;

                        var departure = scheduleDate
                            .AddHours(hour)
                            .AddMinutes(minute);

                    var scheduleKey =
                        $"{flightNumber}|{departure.Ticks}";

                        if (existingScheduleKeys.Contains(scheduleKey))
                        {
                            continue;
                        }

                    var capacity =
                        150 + routeIndex % 4 * 20;

                        context.Flights.Add(
                            new Flight
                            {
                                AirlineId = airline.AirlineId,
                                FlightNumber = flightNumber,
                                From = from,
                                To = to,
                                DepartureTime = departure,
                                ArrivalTime = departure.AddMinutes(route.Minutes),
                                Price = route.Price + direction * 20m + flightIndex * 15m,
                                SeatCapacity = capacity,
                                AvailableSeats = capacity,
                                AircraftModel = route.Minutes > 300
                                    ? "Airbus A330"
                                    : "Airbus A320",
                                FlightLogoUrl = airline.LogoUrl,
                                Status = FlightStatus.Scheduled,
                                IsActive = true,
                                CreatedAt = DateTime.UtcNow
                            }
                        );

                        existingScheduleKeys.Add(scheduleKey);
                    }
                }
            }
        }

        await context.SaveChangesAsync();
    }
}
