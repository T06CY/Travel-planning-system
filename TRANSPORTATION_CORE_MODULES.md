# Transportation Core Modules - Feature Reference

## 📋 Overview
The Transportation service consists of three core modules, each with specific features and responsibilities. This document serves as a reference for planned features and development scope.

---

## 🔴 Core Module 1: Route Discovery & Catalog
**Purpose:** Enable customers to search, explore, and discover transportation routes and available trips.

### Features:
- ✅ **Route Search Engine**
  - Search by origin, destination, date
  - Filter by time, price, vehicle type
  - Advanced filtering options

- ✅ **Trip Listing & Details**
  - Display available trips in catalog
  - Show trip details (departure time, duration, stops, vehicle info)
  - Trip amenities and policies

- ✅ **AJAX Search/Filtering & Sorting**
  - Real-time search results without page reload
  - Dynamic filtering (price range, departure time, duration, ratings)
  - Sort by price, duration, departure time, ratings

- ✅ **Real-Time Seat Availability**
  - Display available seats for each trip
  - Update seat count dynamically
  - Show seat class availability

- ✅ **Trip Status Board**
  - Display trip status (Scheduled, On-time, Delayed, Cancelled)
  - Show current passenger count vs capacity
  - Last update timestamp

- ✅ **Review & Rating**
  - Display average ratings on trip listing
  - Show recent customer reviews
  - Filter by rating

---

## 🟢 Core Module 2: Ticketing & Booking
**Purpose:** Enable customers to book transportation tickets with complete customization and checkout experience.

### Features:
- ✅ **Interactive Seat Selection**
  - Visual seat map (bus/coach layout)
  - Click-to-select seat interaction
  - Show selected seats with price breakdown
  - Seat preferences (window, aisle, front, back)

- ✅ **Passenger Roster Details**
  - Collect passenger information (name, contact, ID)
  - Multiple passenger support
  - Special requirements (mobility, dietary)

- ✅ **Baggage & Add-on Manager**
  - Add baggage allowance
  - Extra baggage purchase
  - Additional services (meal, seat upgrade, insurance)
  - Price adjustment display

- ✅ **Promo Code Application**
  - Enter and validate promo codes
  - Display discount amount
  - Show final price after discount
  - Handle expired/invalid codes

- ✅ **Booking Checkout**
  - Order summary (route, passengers, total price)
  - Payment method selection
  - Terms & conditions acceptance
  - One-click booking confirmation

- ✅ **E-Ticket Generation**
  - Generate digital ticket (PDF/PNG)
  - QR code for check-in
  - Ticket details (confirmation number, barcode)
  - Download and email options

- ✅ **Booking History & Cancellation**
  - View all past and upcoming bookings
  - Cancellation policy and refund display
  - Submit cancellation request
  - Track cancellation status

---

## 🔵 Core Module 3: Transportation Operations (Admin)
**Purpose:** Enable administrators to manage fleet, routes, schedules, and business operations.

### Features:
- ✅ **Fleet Management (CRUD)**
  - Create/Read/Update/Delete vehicles
  - Vehicle specifications (capacity, model, amenities)
  - Vehicle assignment to routes
  - Maintenance schedule and status tracking

- ✅ **Route & Schedule Orchestration**
  - Create and manage transportation routes
  - Schedule trips (date, time, frequency)
  - Set pricing (base fare, dynamic pricing)
  - Manage route stops and waypoints

- ✅ **Crew Assignment**
  - Assign drivers and conductors to trips
  - Track crew availability and schedules
  - Crew shift management
  - Substitute crew management

- ✅ **Promo Code & Discount Manager**
  - Create promotional codes
  - Set discount rules (percentage, fixed amount)
  - Define usage limits (usage count, date range, max discount)
  - Track active and expired promos

- ✅ **Booking & Manifest Management**
  - View all bookings for a trip
  - Generate passenger manifest (checklist for boarding)
  - Manage cancellations and refunds
  - Track revenue by trip/route

- ✅ **Analytics Dashboard**
  - Revenue analytics (daily, weekly, monthly)
  - Occupancy rate by route/trip
  - Popular routes and peak hours
  - Customer feedback and ratings trends
  - Cancellation rate analysis

---

## 📊 Module Relationships

```
┌─────────────────────────────────────────────────────────┐
│         Core Module 1: Route Discovery & Catalog        │
│  (Customer-facing: Search, Browse, Discover, Compare)   │
└──────────────────────┬──────────────────────────────────┘
					   │ (Uses)
					   ↓
┌─────────────────────────────────────────────────────────┐
│       Core Module 2: Ticketing & Booking                │
│  (Customer-facing: Select, Book, Pay, Confirm, Track)   │
└──────────────────────┬──────────────────────────────────┘
					   │ (Uses)
					   ↓
┌─────────────────────────────────────────────────────────┐
│   Core Module 3: Transportation Operations (Admin)      │
│  (Admin-facing: Manage, Orchestrate, Monitor, Analyze)  │
└─────────────────────────────────────────────────────────┘
```

---

## 🎯 Development Flow

### Phase 1: Foundation (Module 1)
- Implement Route Search Engine
- Create Trip Listing & Details
- Add Real-Time Seat Availability
- Build Trip Status Board

### Phase 2: Booking Experience (Module 2)
- Build Interactive Seat Selection
- Implement Passenger Roster
- Add Baggage & Add-ons Manager
- Integrate Promo Code Application

### Phase 3: Operations (Module 3)
- Create Fleet Management CRUD
- Build Route & Schedule Orchestration
- Implement Analytics Dashboard

### Phase 4: Post-Booking
- E-Ticket Generation
- Booking History & Cancellation
- Review & Rating System
- Crew Assignment

---

## 📝 Feature Priority Levels

```
🔴 HIGH PRIORITY (MVP)
  - Route Search Engine
  - Trip Listing & Details
  - Interactive Seat Selection
  - Booking Checkout
  - E-Ticket Generation

🟡 MEDIUM PRIORITY
  - AJAX Filtering & Sorting
  - Passenger Roster Details
  - Baggage & Add-on Manager
  - Promo Code Application
  - Fleet Management (CRUD)

🟢 LOW PRIORITY (Enhancement)
  - Real-Time Seat Availability (advanced)
  - Trip Status Board
  - Crew Assignment
  - Analytics Dashboard
  - Review & Rating
  - Booking History & Cancellation
```

---

## ✅ Status Tracking

| Module | Feature | Status | Priority |
|--------|---------|--------|----------|
| 1 | Route Search Engine | ✅ Completed | HIGH |
| 1 | Trip Listing & Details | ✅ Completed | HIGH |
| 1 | AJAX Search/Filtering & Sorting | ✅ Completed | MEDIUM |
| 1 | Real-Time Seat Availability | ✅ Completed | MEDIUM |
| 1 | Trip Status Board | ✅ Completed | LOW |
| 1 | Review & Rating | ✅ Completed | LOW |
| 2 | Interactive Seat Selection | 🔲 Pending | HIGH |
| 2 | Passenger Roster Details | 🔲 Pending | MEDIUM |
| 2 | Baggage & Add-on Manager | 🔲 Pending | MEDIUM |
| 2 | Promo Code Application | 🔲 Pending | MEDIUM |
| 2 | Booking Checkout | 🔲 Pending | HIGH |
| 2 | E-Ticket Generation | 🔲 Pending | HIGH |
| 2 | Booking History & Cancellation | 🔲 Pending | LOW |
| 3 | Fleet Management (CRUD) | 🔲 Pending | MEDIUM |
| 3 | Route & Schedule Orchestration | 🔲 Pending | MEDIUM |
| 3 | Crew Assignment | 🔲 Pending | LOW |
| 3 | Promo Code & Discount Manager | 🔲 Pending | MEDIUM |
| 3 | Booking & Manifest Management | 🔲 Pending | LOW |
| 3 | Analytics Dashboard | 🔲 Pending | LOW |

---

## 🔗 Cross-Module Dependencies

```
Module 1 → Module 2:
  - Trip data must be available for booking
  - Seat availability must be linked
  - Pricing information passes through

Module 2 → Module 3:
  - Booking data feeds into manifest
  - Payment/refund info to operations
  - Customer data for crew reference

Module 3 ← Module 2:
  - Admin controls seat availability
  - Admin sets pricing and promotions
  - Admin manages trip status
```

---

## 📌 Important Notes

- **User Request Priority**: Features are only implemented when explicitly requested by user
- **Design Consistency**: All features must follow Home page color scheme and layout (see TRANSPORTATION_RULES_REFERENCE.md)
- **Read-Only Access**: Can access other modules but cannot modify their source code
- **Scope Limitation**: Only Transportation module development (no scope creep to other modules)

---

**Last Updated:** Upon Core Modules Documentation  
**Status:** 📋 Reference Document - Ready for Feature Development
