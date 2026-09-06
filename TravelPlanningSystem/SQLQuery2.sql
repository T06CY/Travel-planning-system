UPDATE dbo.Airlines
SET LogoUrl = '/images/flights/ThaiAirways.png'
WHERE AirlineCode = 'TG';

UPDATE dbo.Airlines
SET LogoUrl = '/images/flights/JapanAirlines.png'
WHERE AirlineCode = 'JL';

UPDATE f
SET f.FlightLogoUrl = a.LogoUrl
FROM dbo.Flights f
INNER JOIN dbo.Airlines a
    ON f.AirlineId = a.AirlineId
WHERE a.AirlineCode IN ('TG', 'JL');