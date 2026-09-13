IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722193233_InitialCreate'
)
BEGIN
    CREATE TABLE [ActivityCategories] (
        [ActivityCategoryId] int NOT NULL IDENTITY,
        [Name] nvarchar(60) NOT NULL,
        [Description] nvarchar(250) NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_ActivityCategories] PRIMARY KEY ([ActivityCategoryId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722193233_InitialCreate'
)
BEGIN
    CREATE TABLE [Activities] (
        [ActivityId] int NOT NULL IDENTITY,
        [ActivityName] nvarchar(120) NOT NULL,
        [Destination] nvarchar(80) NOT NULL,
        [Location] nvarchar(180) NOT NULL,
        [Description] nvarchar(2000) NOT NULL,
        [PricePerPerson] decimal(10,2) NOT NULL,
        [DurationHours] float NOT NULL,
        [MinimumParticipants] int NOT NULL,
        [MaximumParticipants] int NOT NULL,
        [MinimumAge] int NOT NULL,
        [IncludedItems] nvarchar(300) NULL,
        [WhatToBring] nvarchar(300) NULL,
        [IsFeatured] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ActivityCategoryId] int NOT NULL,
        CONSTRAINT [PK_Activities] PRIMARY KEY ([ActivityId]),
        CONSTRAINT [FK_Activities_ActivityCategories_ActivityCategoryId] FOREIGN KEY ([ActivityCategoryId]) REFERENCES [ActivityCategories] ([ActivityCategoryId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722193233_InitialCreate'
)
BEGIN
    CREATE TABLE [ActivityPhotos] (
        [ActivityPhotoId] int NOT NULL IDENTITY,
        [ActivityId] int NOT NULL,
        [PhotoUrl] nvarchar(350) NOT NULL,
        [Caption] nvarchar(120) NULL,
        [IsPrimary] bit NOT NULL,
        [DisplayOrder] int NOT NULL,
        CONSTRAINT [PK_ActivityPhotos] PRIMARY KEY ([ActivityPhotoId]),
        CONSTRAINT [FK_ActivityPhotos_Activities_ActivityId] FOREIGN KEY ([ActivityId]) REFERENCES [Activities] ([ActivityId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722193233_InitialCreate'
)
BEGIN
    CREATE TABLE [ActivitySessions] (
        [ActivitySessionId] int NOT NULL IDENTITY,
        [ActivityId] int NOT NULL,
        [SessionDate] datetime2 NOT NULL,
        [StartTime] time NOT NULL,
        [EndTime] time NOT NULL,
        [Capacity] int NOT NULL,
        [AvailableSlots] int NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_ActivitySessions] PRIMARY KEY ([ActivitySessionId]),
        CONSTRAINT [FK_ActivitySessions_Activities_ActivityId] FOREIGN KEY ([ActivityId]) REFERENCES [Activities] ([ActivityId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722193233_InitialCreate'
)
BEGIN
    CREATE TABLE [ActivityBookings] (
        [ActivityBookingId] int NOT NULL IDENTITY,
        [BookingReference] nvarchar(20) NOT NULL,
        [UserId] int NOT NULL,
        [ActivitySessionId] int NOT NULL,
        [ParticipantCount] int NOT NULL,
        [PricePerPerson] decimal(10,2) NOT NULL,
        [TotalAmount] decimal(12,2) NOT NULL,
        [BookingDate] datetime2 NOT NULL,
        [ContactName] nvarchar(100) NOT NULL,
        [ContactEmail] nvarchar(120) NOT NULL,
        [ContactPhone] nvarchar(30) NOT NULL,
        [BookingStatus] int NOT NULL,
        [CancellationReason] nvarchar(400) NULL,
        [CancelledAt] datetime2 NULL,
        CONSTRAINT [PK_ActivityBookings] PRIMARY KEY ([ActivityBookingId]),
        CONSTRAINT [FK_ActivityBookings_ActivitySessions_ActivitySessionId] FOREIGN KEY ([ActivitySessionId]) REFERENCES [ActivitySessions] ([ActivitySessionId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722193233_InitialCreate'
)
BEGIN
    CREATE TABLE [ActivityReviews] (
        [ActivityReviewId] int NOT NULL IDENTITY,
        [ActivityId] int NOT NULL,
        [ActivityBookingId] int NOT NULL,
        [UserId] int NOT NULL,
        [Rating] int NOT NULL,
        [Comment] nvarchar(800) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsVisible] bit NOT NULL,
        [ActivityBookingId1] int NULL,
        CONSTRAINT [PK_ActivityReviews] PRIMARY KEY ([ActivityReviewId]),
        CONSTRAINT [FK_ActivityReviews_Activities_ActivityId] FOREIGN KEY ([ActivityId]) REFERENCES [Activities] ([ActivityId]) ON DELETE CASCADE,
        CONSTRAINT [FK_ActivityReviews_ActivityBookings_ActivityBookingId] FOREIGN KEY ([ActivityBookingId]) REFERENCES [ActivityBookings] ([ActivityBookingId]),
        CONSTRAINT [FK_ActivityReviews_ActivityBookings_ActivityBookingId1] FOREIGN KEY ([ActivityBookingId1]) REFERENCES [ActivityBookings] ([ActivityBookingId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722193233_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Activities_ActivityCategoryId] ON [Activities] ([ActivityCategoryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722193233_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ActivityBookings_ActivitySessionId] ON [ActivityBookings] ([ActivitySessionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722193233_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ActivityPhotos_ActivityId] ON [ActivityPhotos] ([ActivityId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722193233_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ActivityReviews_ActivityBookingId] ON [ActivityReviews] ([ActivityBookingId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722193233_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_ActivityReviews_ActivityBookingId1] ON [ActivityReviews] ([ActivityBookingId1]) WHERE [ActivityBookingId1] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722193233_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ActivityReviews_ActivityId] ON [ActivityReviews] ([ActivityId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722193233_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ActivitySessions_ActivityId] ON [ActivitySessions] ([ActivityId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722193233_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260722193233_InitialCreate', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821101448_UpdateModels'
)
BEGIN
    CREATE TABLE [StaffRoles] (
        [RoleId] uniqueidentifier NOT NULL,
        [RoleName] nvarchar(50) NOT NULL,
        CONSTRAINT [PK_StaffRoles] PRIMARY KEY ([RoleId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821101448_UpdateModels'
)
BEGIN
    CREATE TABLE [Users] (
        [UserId] uniqueidentifier NOT NULL,
        [Email] nvarchar(150) NOT NULL,
        [PasswordHash] nvarchar(max) NOT NULL,
        [OAuthProvider] nvarchar(50) NULL,
        [FirstName] nvarchar(100) NOT NULL,
        [LastName] nvarchar(100) NOT NULL,
        [PhoneNumber] nvarchar(20) NULL,
        [DateOfBirth] datetime2 NULL,
        [PreferredCurrency] nvarchar(3) NOT NULL,
        [PreferredLanguage] nvarchar(10) NOT NULL,
        [LoyaltyTier] nvarchar(20) NOT NULL,
        [RewardPoints] int NOT NULL,
        [AccountStatus] nvarchar(20) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([UserId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821101448_UpdateModels'
)
BEGIN
    CREATE TABLE [StaffUsers] (
        [StaffId] uniqueidentifier NOT NULL,
        [Email] nvarchar(150) NOT NULL,
        [PasswordHash] nvarchar(max) NOT NULL,
        [MfaSecret] nvarchar(100) NULL,
        [FirstName] nvarchar(100) NOT NULL,
        [LastName] nvarchar(100) NOT NULL,
        [Department] nvarchar(100) NOT NULL,
        [ManagedPropertyId] uniqueidentifier NULL,
        [AccessLevel] int NOT NULL,
        [LastLoginIp] nvarchar(45) NULL,
        [Status] nvarchar(20) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [RoleId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_StaffUsers] PRIMARY KEY ([StaffId]),
        CONSTRAINT [FK_StaffUsers_StaffRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [StaffRoles] ([RoleId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821101448_UpdateModels'
)
BEGIN
    CREATE UNIQUE INDEX [IX_StaffUsers_Email] ON [StaffUsers] ([Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821101448_UpdateModels'
)
BEGIN
    CREATE INDEX [IX_StaffUsers_RoleId] ON [StaffUsers] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821101448_UpdateModels'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821101448_UpdateModels'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260821101448_UpdateModels', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821175314_AddProfilePicToUsers'
)
BEGIN
    ALTER TABLE [Users] ADD [ProfilePic] nvarchar(250) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821175314_AddProfilePicToUsers'
)
BEGIN
    ALTER TABLE [Users] ADD [ProfilePictureUrl] nvarchar(250) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260821175314_AddProfilePicToUsers'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260821175314_AddProfilePicToUsers', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822100119_AddHotelModule'
)
BEGIN
    CREATE TABLE [HotelRooms] (
        [HotelRoomId] int NOT NULL IDENTITY,
        [HotelName] nvarchar(120) NOT NULL,
        [RoomName] nvarchar(120) NOT NULL,
        [Destination] nvarchar(80) NOT NULL,
        [Address] nvarchar(180) NOT NULL,
        [Description] nvarchar(2000) NOT NULL,
        [PricePerNight] decimal(10,2) NOT NULL,
        [Capacity] int NOT NULL,
        [TotalRooms] int NOT NULL,
        [Amenities] nvarchar(500) NULL,
        [StarRating] int NOT NULL,
        [IsFeatured] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_HotelRooms] PRIMARY KEY ([HotelRoomId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822100119_AddHotelModule'
)
BEGIN
    CREATE TABLE [HotelReservations] (
        [HotelReservationId] int NOT NULL IDENTITY,
        [ReservationReference] nvarchar(20) NOT NULL,
        [UserId] int NOT NULL,
        [HotelRoomId] int NOT NULL,
        [CheckInDate] datetime2 NOT NULL,
        [CheckOutDate] datetime2 NOT NULL,
        [GuestCount] int NOT NULL,
        [PricePerNight] decimal(10,2) NOT NULL,
        [TotalAmount] decimal(12,2) NOT NULL,
        [ReservationDate] datetime2 NOT NULL,
        [ContactName] nvarchar(100) NOT NULL,
        [ContactEmail] nvarchar(120) NOT NULL,
        [ContactPhone] nvarchar(30) NOT NULL,
        [ReservationStatus] int NOT NULL,
        [CancellationReason] nvarchar(400) NULL,
        [CancelledAt] datetime2 NULL,
        CONSTRAINT [PK_HotelReservations] PRIMARY KEY ([HotelReservationId]),
        CONSTRAINT [FK_HotelReservations_HotelRooms_HotelRoomId] FOREIGN KEY ([HotelRoomId]) REFERENCES [HotelRooms] ([HotelRoomId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822100119_AddHotelModule'
)
BEGIN
    CREATE TABLE [HotelRoomPhotos] (
        [HotelRoomPhotoId] int NOT NULL IDENTITY,
        [HotelRoomId] int NOT NULL,
        [PhotoUrl] nvarchar(350) NOT NULL,
        [Caption] nvarchar(120) NULL,
        [IsPrimary] bit NOT NULL,
        [DisplayOrder] int NOT NULL,
        CONSTRAINT [PK_HotelRoomPhotos] PRIMARY KEY ([HotelRoomPhotoId]),
        CONSTRAINT [FK_HotelRoomPhotos_HotelRooms_HotelRoomId] FOREIGN KEY ([HotelRoomId]) REFERENCES [HotelRooms] ([HotelRoomId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822100119_AddHotelModule'
)
BEGIN
    CREATE TABLE [HotelReviews] (
        [HotelReviewId] int NOT NULL IDENTITY,
        [HotelRoomId] int NOT NULL,
        [HotelReservationId] int NOT NULL,
        [UserId] int NOT NULL,
        [Rating] int NOT NULL,
        [Comment] nvarchar(800) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [IsVisible] bit NOT NULL,
        CONSTRAINT [PK_HotelReviews] PRIMARY KEY ([HotelReviewId]),
        CONSTRAINT [FK_HotelReviews_HotelReservations_HotelReservationId] FOREIGN KEY ([HotelReservationId]) REFERENCES [HotelReservations] ([HotelReservationId]),
        CONSTRAINT [FK_HotelReviews_HotelRooms_HotelRoomId] FOREIGN KEY ([HotelRoomId]) REFERENCES [HotelRooms] ([HotelRoomId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822100119_AddHotelModule'
)
BEGIN
    CREATE INDEX [IX_HotelReservations_HotelRoomId] ON [HotelReservations] ([HotelRoomId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822100119_AddHotelModule'
)
BEGIN
    CREATE UNIQUE INDEX [IX_HotelReviews_HotelReservationId] ON [HotelReviews] ([HotelReservationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822100119_AddHotelModule'
)
BEGIN
    CREATE INDEX [IX_HotelReviews_HotelRoomId] ON [HotelReviews] ([HotelRoomId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822100119_AddHotelModule'
)
BEGIN
    CREATE INDEX [IX_HotelRoomPhotos_HotelRoomId] ON [HotelRoomPhotos] ([HotelRoomId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260822100119_AddHotelModule'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260822100119_AddHotelModule', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824055120_AddRoomTypeToHotelRooms'
)
BEGIN
    ALTER TABLE [HotelReservations] ADD [CheckInTime] time NOT NULL DEFAULT '00:00:00';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824055120_AddRoomTypeToHotelRooms'
)
BEGIN
    ALTER TABLE [HotelReservations] ADD [CheckOutTime] time NOT NULL DEFAULT '00:00:00';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824055120_AddRoomTypeToHotelRooms'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260824055120_AddRoomTypeToHotelRooms', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904000100_EnsureHotelRoomType'
)
BEGIN

    IF COL_LENGTH('HotelRooms', 'RoomType') IS NULL
    BEGIN
        ALTER TABLE [HotelRooms]
        ADD [RoomType] nvarchar(30) NOT NULL
            CONSTRAINT [DF_HotelRooms_RoomType] DEFAULT N'Master Room';
    END
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904000100_EnsureHotelRoomType'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260904000100_EnsureHotelRoomType', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904080238_AutoAdd_PendingModelChanges_20260904'
)
BEGIN
    IF EXISTS (
        SELECT * FROM sys.columns
        WHERE [object_id] = OBJECT_ID(N'[dbo].[HotelRooms]') AND [name] = N'RoomType'
    )
    BEGIN
        -- First, drop the default constraint if it exists
        IF EXISTS (
            SELECT * FROM sys.default_constraints
            WHERE parent_object_id = OBJECT_ID(N'[dbo].[HotelRooms]')
            AND name = N'DF_HotelRooms_RoomType'
        )
        BEGIN
            ALTER TABLE [HotelRooms] DROP CONSTRAINT [DF_HotelRooms_RoomType]
        END

        -- Then drop the column
        ALTER TABLE [HotelRooms] DROP COLUMN [RoomType]
    END
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904080238_AutoAdd_PendingModelChanges_20260904'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260904080238_AutoAdd_PendingModelChanges_20260904', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904113618_AddHotelRoomType'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260904113618_AddHotelRoomType', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906150000_AddAirlineReservationModule'
)
BEGIN
    CREATE TABLE [Airlines] (
        [AirlineId] int NOT NULL IDENTITY,
        [AirlineCode] nvarchar(10) NOT NULL,
        [AirlineName] nvarchar(100) NOT NULL,
        [Country] nvarchar(80) NOT NULL,
        [LogoUrl] nvarchar(350) NULL,
        [SupportEmail] nvarchar(150) NULL,
        [ContactNumber] nvarchar(30) NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Airlines] PRIMARY KEY ([AirlineId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906150000_AddAirlineReservationModule'
)
BEGIN
    CREATE TABLE [Airports] (
        [AirportId] int NOT NULL IDENTITY,
        [AirportCode] nvarchar(5) NOT NULL,
        [AirportName] nvarchar(100) NOT NULL,
        [City] nvarchar(80) NOT NULL,
        [Country] nvarchar(80) NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Airports] PRIMARY KEY ([AirportId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906150000_AddAirlineReservationModule'
)
BEGIN
    CREATE TABLE [FlightBookings] (
        [FlightBookingId] int NOT NULL IDENTITY,
        [BookingReference] nvarchar(20) NOT NULL,
        [UserEmail] nvarchar(150) NOT NULL,
        [TripType] nvarchar(20) NOT NULL,
        [ContactName] nvarchar(120) NOT NULL,
        [ContactEmail] nvarchar(150) NOT NULL,
        [ContactPhone] nvarchar(30) NOT NULL,
        [BookingDate] datetime2 NOT NULL,
        [TotalAmount] decimal(12,2) NOT NULL,
        [Status] int NOT NULL,
        [CancellationReason] nvarchar(400) NULL,
        [CancelledAt] datetime2 NULL,
        CONSTRAINT [PK_FlightBookings] PRIMARY KEY ([FlightBookingId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906150000_AddAirlineReservationModule'
)
BEGIN
    CREATE TABLE [Flights] (
        [FlightId] int NOT NULL IDENTITY,
        [AirlineId] int NOT NULL,
        [FlightNumber] nvarchar(15) NOT NULL,
        [From] nvarchar(80) NOT NULL,
        [To] nvarchar(80) NOT NULL,
        [DepartureTime] datetime2 NOT NULL,
        [ArrivalTime] datetime2 NOT NULL,
        [Price] decimal(10,2) NOT NULL,
        [SeatCapacity] int NOT NULL,
        [AvailableSeats] int NOT NULL,
        [AircraftModel] nvarchar(80) NOT NULL,
        [Status] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Flights] PRIMARY KEY ([FlightId]),
        CONSTRAINT [FK_Flights_Airlines_AirlineId] FOREIGN KEY ([AirlineId]) REFERENCES [Airlines] ([AirlineId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906150000_AddAirlineReservationModule'
)
BEGIN
    CREATE TABLE [FlightPassengers] (
        [FlightPassengerId] int NOT NULL IDENTITY,
        [FlightBookingId] int NOT NULL,
        [FirstName] nvarchar(80) NOT NULL,
        [LastName] nvarchar(80) NOT NULL,
        [PassportNumber] nvarchar(30) NOT NULL,
        [Nationality] nvarchar(60) NOT NULL,
        [DateOfBirth] datetime2 NOT NULL,
        [SeatNumber] nvarchar(5) NOT NULL,
        [MealPreference] nvarchar(40) NOT NULL,
        CONSTRAINT [PK_FlightPassengers] PRIMARY KEY ([FlightPassengerId]),
        CONSTRAINT [FK_FlightPassengers_FlightBookings_FlightBookingId] FOREIGN KEY ([FlightBookingId]) REFERENCES [FlightBookings] ([FlightBookingId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906150000_AddAirlineReservationModule'
)
BEGIN
    CREATE TABLE [FlightBookingSegments] (
        [FlightBookingSegmentId] int NOT NULL IDENTITY,
        [FlightBookingId] int NOT NULL,
        [FlightId] int NOT NULL,
        [SegmentOrder] int NOT NULL,
        [PricePerPassenger] decimal(10,2) NOT NULL,
        CONSTRAINT [PK_FlightBookingSegments] PRIMARY KEY ([FlightBookingSegmentId]),
        CONSTRAINT [FK_FlightBookingSegments_FlightBookings_FlightBookingId] FOREIGN KEY ([FlightBookingId]) REFERENCES [FlightBookings] ([FlightBookingId]) ON DELETE CASCADE,
        CONSTRAINT [FK_FlightBookingSegments_Flights_FlightId] FOREIGN KEY ([FlightId]) REFERENCES [Flights] ([FlightId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906150000_AddAirlineReservationModule'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Airlines_AirlineCode] ON [Airlines] ([AirlineCode]) WHERE [AirlineCode] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906150000_AddAirlineReservationModule'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Airports_AirportCode] ON [Airports] ([AirportCode]) WHERE [AirportCode] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906150000_AddAirlineReservationModule'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_FlightBookings_BookingReference] ON [FlightBookings] ([BookingReference]) WHERE [BookingReference] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906150000_AddAirlineReservationModule'
)
BEGIN
    CREATE INDEX [IX_Flights_AirlineId] ON [Flights] ([AirlineId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906150000_AddAirlineReservationModule'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Flights_FlightNumber_DepartureTime] ON [Flights] ([FlightNumber], [DepartureTime]) WHERE [FlightNumber] IS NOT NULL AND [DepartureTime] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906150000_AddAirlineReservationModule'
)
BEGIN
    CREATE INDEX [IX_FlightPassengers_FlightBookingId] ON [FlightPassengers] ([FlightBookingId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906150000_AddAirlineReservationModule'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_FlightBookingSegments_FlightBookingId_SegmentOrder] ON [FlightBookingSegments] ([FlightBookingId], [SegmentOrder]) WHERE [FlightBookingId] IS NOT NULL AND [SegmentOrder] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906150000_AddAirlineReservationModule'
)
BEGIN
    CREATE INDEX [IX_FlightBookingSegments_FlightId] ON [FlightBookingSegments] ([FlightId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906150000_AddAirlineReservationModule'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260906150000_AddAirlineReservationModule', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906170000_AddReturnSeatNumber'
)
BEGIN
    ALTER TABLE [FlightPassengers] ADD [ReturnSeatNumber] nvarchar(5) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906170000_AddReturnSeatNumber'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260906170000_AddReturnSeatNumber', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906180000_AddFlightLogoUrl'
)
BEGIN
    ALTER TABLE [Flights] ADD [FlightLogoUrl] nvarchar(350) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906180000_AddFlightLogoUrl'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260906180000_AddFlightLogoUrl', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906180341_AddTransportationModule'
)
BEGIN
    CREATE TABLE [Routes] (
        [RouteId] int NOT NULL IDENTITY,
        [Origin] nvarchar(100) NOT NULL,
        [Destination] nvarchar(100) NOT NULL,
        [DistanceKm] decimal(10,2) NOT NULL,
        [EstimatedDurationHours] float NOT NULL,
        [Description] nvarchar(1000) NULL,
        [Stops] nvarchar(500) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Routes] PRIMARY KEY ([RouteId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906180341_AddTransportationModule'
)
BEGIN
    CREATE TABLE [Vehicles] (
        [VehicleId] int NOT NULL IDENTITY,
        [LicensePlate] nvarchar(50) NOT NULL,
        [VehicleModel] nvarchar(100) NOT NULL,
        [VehicleType] nvarchar(50) NOT NULL,
        [SeatingCapacity] int NOT NULL,
        [ManufactureYear] int NOT NULL,
        [Amenities] nvarchar(500) NULL,
        [RegistrationNumber] nvarchar(500) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Vehicles] PRIMARY KEY ([VehicleId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906180341_AddTransportationModule'
)
BEGIN
    CREATE TABLE [Trips] (
        [TripId] int NOT NULL IDENTITY,
        [RouteId] int NOT NULL,
        [VehicleId] int NOT NULL,
        [DepartureTime] datetime2 NOT NULL,
        [ArrivalTime] datetime2 NOT NULL,
        [BaseFare] decimal(10,2) NOT NULL,
        [DiscountPercentage] decimal(5,2) NOT NULL,
        [AvailableSeats] int NOT NULL,
        [TotalSeats] int NOT NULL,
        [Status] nvarchar(50) NOT NULL,
        [Notes] nvarchar(500) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Trips] PRIMARY KEY ([TripId]),
        CONSTRAINT [FK_Trips_Routes_RouteId] FOREIGN KEY ([RouteId]) REFERENCES [Routes] ([RouteId]),
        CONSTRAINT [FK_Trips_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicles] ([VehicleId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906180341_AddTransportationModule'
)
BEGIN
    CREATE TABLE [Seats] (
        [SeatId] int NOT NULL IDENTITY,
        [TripId] int NOT NULL,
        [SeatNumber] nvarchar(10) NOT NULL,
        [SeatClass] nvarchar(20) NULL,
        [SeatType] nvarchar(50) NULL,
        [IsAvailable] bit NOT NULL,
        [Status] nvarchar(50) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Seats] PRIMARY KEY ([SeatId]),
        CONSTRAINT [FK_Seats_Trips_TripId] FOREIGN KEY ([TripId]) REFERENCES [Trips] ([TripId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906180341_AddTransportationModule'
)
BEGIN
    CREATE TABLE [TransportationReviews] (
        [ReviewId] int NOT NULL IDENTITY,
        [TripId] int NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [Rating] int NOT NULL,
        [Title] nvarchar(500) NULL,
        [Comment] nvarchar(2000) NULL,
        [IsVisible] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_TransportationReviews] PRIMARY KEY ([ReviewId]),
        CONSTRAINT [FK_TransportationReviews_Trips_TripId] FOREIGN KEY ([TripId]) REFERENCES [Trips] ([TripId]),
        CONSTRAINT [FK_TransportationReviews_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906180341_AddTransportationModule'
)
BEGIN
    CREATE INDEX [IX_Seats_TripId] ON [Seats] ([TripId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906180341_AddTransportationModule'
)
BEGIN
    CREATE INDEX [IX_TransportationReviews_TripId] ON [TransportationReviews] ([TripId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906180341_AddTransportationModule'
)
BEGIN
    CREATE INDEX [IX_TransportationReviews_UserId] ON [TransportationReviews] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906180341_AddTransportationModule'
)
BEGIN
    CREATE INDEX [IX_Trips_RouteId] ON [Trips] ([RouteId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906180341_AddTransportationModule'
)
BEGIN
    CREATE INDEX [IX_Trips_VehicleId] ON [Trips] ([VehicleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906180341_AddTransportationModule'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260906180341_AddTransportationModule', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906184707_RestoreRoomTypeColumn'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260906184707_RestoreRoomTypeColumn', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906195019_FixActivityReviewRelationship'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260906195019_FixActivityReviewRelationship', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906200000_AddPassengerType'
)
BEGIN
    ALTER TABLE [FlightPassengers] ADD [PassengerType] nvarchar(20) NOT NULL DEFAULT N'Adult';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906200000_AddPassengerType'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260906200000_AddPassengerType', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906201504_RebuildModelSnapshot'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260906201504_RebuildModelSnapshot', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906204928_AddTransportationBookingModels'
)
BEGIN
    CREATE TABLE [TransportationBookings] (
        [BookingId] int NOT NULL IDENTITY,
        [BookingReference] nvarchar(50) NOT NULL,
        [TripId] int NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [ContactName] nvarchar(100) NOT NULL,
        [ContactEmail] nvarchar(150) NOT NULL,
        [ContactPhone] nvarchar(30) NOT NULL,
        [BaseFareTotal] decimal(18,2) NOT NULL,
        [BaggageFeeTotal] decimal(18,2) NOT NULL,
        [InsuranceFeeTotal] decimal(18,2) NOT NULL,
        [PromoCode] nvarchar(50) NULL,
        [DiscountAmount] decimal(18,2) NOT NULL,
        [TotalAmount] decimal(18,2) NOT NULL,
        [BookingStatus] nvarchar(30) NOT NULL,
        [PaymentStatus] nvarchar(30) NOT NULL,
        [PaymentMethod] nvarchar(50) NOT NULL,
        [BookingDate] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_TransportationBookings] PRIMARY KEY ([BookingId]),
        CONSTRAINT [FK_TransportationBookings_Trips_TripId] FOREIGN KEY ([TripId]) REFERENCES [Trips] ([TripId]) ON DELETE CASCADE,
        CONSTRAINT [FK_TransportationBookings_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906204928_AddTransportationBookingModels'
)
BEGIN
    CREATE TABLE [TransportationPassengers] (
        [PassengerId] int NOT NULL IDENTITY,
        [BookingId] int NOT NULL,
        [FullName] nvarchar(100) NOT NULL,
        [IdNumber] nvarchar(50) NOT NULL,
        [PassengerType] nvarchar(20) NOT NULL,
        [SeatId] int NULL,
        [SeatNumber] nvarchar(10) NOT NULL,
        [BaggageOption] nvarchar(50) NOT NULL,
        [BaggagePrice] decimal(18,2) NOT NULL,
        [HasTravelInsurance] bit NOT NULL,
        [InsurancePrice] decimal(18,2) NOT NULL,
        [SpecialRequests] nvarchar(255) NULL,
        CONSTRAINT [PK_TransportationPassengers] PRIMARY KEY ([PassengerId]),
        CONSTRAINT [FK_TransportationPassengers_Seats_SeatId] FOREIGN KEY ([SeatId]) REFERENCES [Seats] ([SeatId]),
        CONSTRAINT [FK_TransportationPassengers_TransportationBookings_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [TransportationBookings] ([BookingId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906204928_AddTransportationBookingModels'
)
BEGIN
    CREATE INDEX [IX_TransportationBookings_TripId] ON [TransportationBookings] ([TripId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906204928_AddTransportationBookingModels'
)
BEGIN
    CREATE INDEX [IX_TransportationBookings_UserId] ON [TransportationBookings] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906204928_AddTransportationBookingModels'
)
BEGIN
    CREATE INDEX [IX_TransportationPassengers_BookingId] ON [TransportationPassengers] ([BookingId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906204928_AddTransportationBookingModels'
)
BEGIN
    CREATE INDEX [IX_TransportationPassengers_SeatId] ON [TransportationPassengers] ([SeatId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906204928_AddTransportationBookingModels'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260906204928_AddTransportationBookingModels', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906210000_CompleteAirlineImages'
)
BEGIN
    UPDATE Airlines SET LogoUrl = CASE AirlineCode
                WHEN 'MH' THEN '/images/flights/MalaysiaAirlines.png'
                WHEN 'AK' THEN '/images/flights/AirAsia.png'
                WHEN 'OD' THEN '/images/flights/BatikAir.png'
                WHEN 'SQ' THEN '/images/flights/SingaporeAirlines.png'
                WHEN 'TG' THEN '/images/flights/ThaiAirways.png'
                WHEN 'JL' THEN '/images/flights/JapanAirlines.png'
                ELSE LogoUrl END;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906210000_CompleteAirlineImages'
)
BEGIN
    UPDATE f SET FlightLogoUrl = a.LogoUrl
                FROM Flights f INNER JOIN Airlines a ON f.AirlineId = a.AirlineId
                WHERE a.LogoUrl IS NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906210000_CompleteAirlineImages'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260906210000_CompleteAirlineImages', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907025028_BackfillHotelRoomRoomTypes'
)
BEGIN

                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HotelRooms]') AND name = 'RoomType')
                BEGIN
                    ALTER TABLE [dbo].[HotelRooms] ADD [RoomType] nvarchar(max) NULL;
                END

                EXEC(N'UPDATE [HotelRooms] SET [RoomType] = N''Master Room'' WHERE [RoomType] IS NULL;');
            
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907025028_BackfillHotelRoomRoomTypes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260907025028_BackfillHotelRoomRoomTypes', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907032403_AddPassengerBoardingStatus'
)
BEGIN
    ALTER TABLE [TransportationPassengers] ADD [BoardedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907032403_AddPassengerBoardingStatus'
)
BEGIN
    ALTER TABLE [TransportationPassengers] ADD [IsBoarded] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907032403_AddPassengerBoardingStatus'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260907032403_AddPassengerBoardingStatus', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907062151_AddActivityPaymentDetails'
)
BEGIN
    ALTER TABLE [ActivityBookings] ADD [AddonFee] decimal(10,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907062151_AddActivityPaymentDetails'
)
BEGIN
    ALTER TABLE [ActivityBookings] ADD [DiscountAmount] decimal(10,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907062151_AddActivityPaymentDetails'
)
BEGIN
    ALTER TABLE [ActivityBookings] ADD [PaymentMethod] nvarchar(50) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907062151_AddActivityPaymentDetails'
)
BEGIN
    ALTER TABLE [ActivityBookings] ADD [PaymentStatus] nvarchar(30) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907062151_AddActivityPaymentDetails'
)
BEGIN
    ALTER TABLE [ActivityBookings] ADD [PromoCode] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907062151_AddActivityPaymentDetails'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260907062151_AddActivityPaymentDetails', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907062401_AddHotelReservationPaymentFields'
)
BEGIN
    ALTER TABLE [HotelReservations] ADD [PaymentMethod] nvarchar(50) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907062401_AddHotelReservationPaymentFields'
)
BEGIN
    ALTER TABLE [HotelReservations] ADD [PaymentStatus] nvarchar(30) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907062401_AddHotelReservationPaymentFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260907062401_AddHotelReservationPaymentFields', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907071356_SyncModelWithDatabase'
)
BEGIN
    ALTER TABLE [FlightBookings] ADD [AddonFee] decimal(12,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907071356_SyncModelWithDatabase'
)
BEGIN
    ALTER TABLE [FlightBookings] ADD [BaggageOption] nvarchar(50) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907071356_SyncModelWithDatabase'
)
BEGIN
    ALTER TABLE [FlightBookings] ADD [DiscountAmount] decimal(12,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907071356_SyncModelWithDatabase'
)
BEGIN
    ALTER TABLE [FlightBookings] ADD [HasTravelInsurance] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907071356_SyncModelWithDatabase'
)
BEGIN
    ALTER TABLE [FlightBookings] ADD [PaymentMethod] nvarchar(50) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907071356_SyncModelWithDatabase'
)
BEGIN
    ALTER TABLE [FlightBookings] ADD [PaymentStatus] nvarchar(30) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907071356_SyncModelWithDatabase'
)
BEGIN
    ALTER TABLE [FlightBookings] ADD [PromoCode] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907071356_SyncModelWithDatabase'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260907071356_SyncModelWithDatabase', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907071633_AddHotelCities'
)
BEGIN
    CREATE TABLE [HotelCities] (
        [HotelCityId] int NOT NULL IDENTITY,
        [Name] nvarchar(80) NOT NULL,
        CONSTRAINT [PK_HotelCities] PRIMARY KEY ([HotelCityId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907071633_AddHotelCities'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260907071633_AddHotelCities', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907072916_AddLoginLockoutFields'
)
BEGIN
    ALTER TABLE [Users] ADD [FailedLoginAttempts] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907072916_AddLoginLockoutFields'
)
BEGIN
    ALTER TABLE [Users] ADD [LockoutUntil] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907072916_AddLoginLockoutFields'
)
BEGIN
    ALTER TABLE [StaffUsers] ADD [FailedLoginAttempts] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907072916_AddLoginLockoutFields'
)
BEGIN
    ALTER TABLE [StaffUsers] ADD [LockoutUntil] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907072916_AddLoginLockoutFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260907072916_AddLoginLockoutFields', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907074009_AddStaffProfilePicture'
)
BEGIN
    ALTER TABLE [StaffUsers] ADD [ProfilePic] nvarchar(250) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907074009_AddStaffProfilePicture'
)
BEGIN
    ALTER TABLE [StaffUsers] ADD [ProfilePictureUrl] nvarchar(250) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907074009_AddStaffProfilePicture'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260907074009_AddStaffProfilePicture', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907075204_AddHotelReservationRewardPointsFlag'
)
BEGIN
    ALTER TABLE [HotelReservations] ADD [RewardPointsAwarded] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907075204_AddHotelReservationRewardPointsFlag'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260907075204_AddHotelReservationRewardPointsFlag', N'10.0.10');
END;

COMMIT;
GO

