using Microsoft.EntityFrameworkCore;
using TravelPlanningSystem.Models;

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

        var adventure = await context.ActivityCategories
            .FirstAsync(c => c.Name == "Adventure");

        var culture = await context.ActivityCategories
            .FirstAsync(c => c.Name == "Culture");

        var nature = await context.ActivityCategories
            .FirstAsync(c => c.Name == "Nature");

        var food = await context.ActivityCategories
            .FirstAsync(c => c.Name == "Food & Dining");

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
                Photo = "/images/activities/uploads/kl-heritage.jpg",
                Caption = "Kuala Lumpur heritage experience"
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
                Description =
                    "Taste famous local dishes while learning about Penang's multicultural food heritage.",
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
                Description =
                    "Enjoy an exciting beginner-friendly rafting adventure surrounded by beautiful tropical scenery.",
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
                Description =
                    "Explore Melaka's historic streets, colonial landmarks and famous heritage attractions with a local guide.",
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
                Description =
                    "Visit scenic tea plantations, enjoy cool mountain air and learn about Malaysia's tea production.",
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
                Description =
                    "Enjoy panoramic city views from one of Kuala Lumpur's most iconic observation attractions.",
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
                Description =
                    "Spend an exciting day enjoying water attractions, rides and adventure experiences.",
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
                Description =
                    "Discover beautiful limestone caves, temples and peaceful gardens around Ipoh.",
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
                Description =
                    "Discover Sarawak wildlife and observe orangutans in a protected natural environment.",
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
            var activity = await context.Activities
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
                    ActivityCategoryId = seed.CategoryId,
                    IsFeatured = seed.Featured,
                    IsActive = true
                };

                context.Activities.Add(activity);

                await context.SaveChangesAsync();
            }

            var existingPhoto = await context.ActivityPhotos
                .FirstOrDefaultAsync(p =>
                    p.ActivityId == activity.ActivityId &&
                    p.IsPrimary);

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


            await EnsureFutureSessionsAsync(
                context,
                activity
            );
        }

        await context.SaveChangesAsync();
    }

    private static async Task EnsureFutureSessionsAsync(
        AppDbContext context,
        Activity activity)
    {
        var existingFutureSessions =
            await context.ActivitySessions
                .Where(s =>
                    s.ActivityId == activity.ActivityId &&
                    s.IsActive &&
                    s.SessionDate >= DateTime.Today)
                .OrderBy(s => s.SessionDate)
                .ToListAsync();

        // Always maintain at least 6 future sessions
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
                    .Max(s => s.SessionDate)
                : DateTime.Today;

        for (var i = 1; i <= sessionsNeeded; i++)
        {
            var sessionDate =
                lastDate.AddDays(i * 2);

            var startTime =
                new TimeSpan(9, 0, 0);

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
}