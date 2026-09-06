# TravelMate Airline Reservation Module

This package contains only the airline reservation and flight-management files.

Included features:

- Airline and airport data
- Flight search and multi-city search
- One-way and return bookings
- Required visual seat selection
- Separate outbound and return seats
- Adult/Child passenger validation
- Country-aware phone validation
- Admin flight CRUD and per-flight images
- Admin booking and seat management
- Flight-related EF Core migrations

Copy the folders into the matching locations in the complete TravelMate project.
The module uses the existing `ApplicationUser`, authentication, shared layout,
and application database configuration from that project. Run `Update-Database`
after copying the migration files.
