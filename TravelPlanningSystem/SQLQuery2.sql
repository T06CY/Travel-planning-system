/* =========================================================
   FIX MISSING FLIGHT COLUMNS
   Local database workaround
   ========================================================= */

-- 1. FlightBookingSegments - CabinClass
IF COL_LENGTH('FlightBookingSegments', 'CabinClass') IS NULL
BEGIN
    ALTER TABLE FlightBookingSegments
    ADD CabinClass NVARCHAR(30) NOT NULL
        CONSTRAINT DF_FlightBookingSegments_CabinClass
        DEFAULT 'Economy';

    PRINT 'CabinClass added successfully.';
END
ELSE
BEGIN
    PRINT 'CabinClass already exists.';
END
GO


-- 2. Flights - DiscountPercent
IF COL_LENGTH('Flights', 'DiscountPercent') IS NULL
BEGIN
    ALTER TABLE Flights
    ADD DiscountPercent DECIMAL(5,2) NOT NULL
        CONSTRAINT DF_Flights_DiscountPercent
        DEFAULT 0;

    PRINT 'DiscountPercent added successfully.';
END
ELSE
BEGIN
    PRINT 'DiscountPercent already exists.';
END
GO


-- 3. Flights - EstimatedArrivalTime
IF COL_LENGTH('Flights', 'EstimatedArrivalTime') IS NULL
BEGIN
    ALTER TABLE Flights
    ADD EstimatedArrivalTime DATETIME2 NULL;

    PRINT 'EstimatedArrivalTime added successfully.';
END
ELSE
BEGIN
    PRINT 'EstimatedArrivalTime already exists.';
END
GO


-- 4. Flights - EstimatedDepartureTime
IF COL_LENGTH('Flights', 'EstimatedDepartureTime') IS NULL
BEGIN
    ALTER TABLE Flights
    ADD EstimatedDepartureTime DATETIME2 NULL;

    PRINT 'EstimatedDepartureTime added successfully.';
END
ELSE
BEGIN
    PRINT 'EstimatedDepartureTime already exists.';
END
GO


/* =========================================================
   VERIFY THE COLUMNS
   ========================================================= */

SELECT
    TABLE_NAME,
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE
    (TABLE_NAME = 'FlightBookingSegments'
        AND COLUMN_NAME = 'CabinClass')
    OR
    (TABLE_NAME = 'Flights'
        AND COLUMN_NAME IN (
            'DiscountPercent',
            'EstimatedArrivalTime',
            'EstimatedDepartureTime'
        ))
ORDER BY TABLE_NAME, COLUMN_NAME;