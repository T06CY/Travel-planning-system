# Transportation Module Development Rules

## 📋 Overview
This document defines the constraints and guidelines for developing the Transportation service in the Travel Planning System web and mobile application.

---

## 🚫 Rule 1: Protected Files (Do Not Modify)
Only the following files can be modified:
- ✅ **Home/Index.cshtml** - Modifiable
- ✅ **Booking/Index.cshtml** - Modifiable  
- ✅ **AdminDashboard/Index.cshtml** - Modifiable

❌ All other existing files are **READ-ONLY**. Do not touch them.

---

## 🚌 Rule 2: Transportation Module Scope Only
**Requirement:** Only work on transportation-related functionality.

**Allowed:**
- ✅ Create new files in transportation folder
- ✅ Create new folders under transportation module
- ✅ Implement transportation-specific features

**Forbidden:**
- ❌ Do not modify source code of other modules (e.g., Hotel, Flight, Accommodation)
- ❌ Do not extend or alter non-transportation features

---

## 🔗 Rule 3: Cross-Module Access (Read-Only)
**When accessing functionality from other modules:**

**Allowed:**
- ✅ Read and use (consume) functionality from other modules
- ✅ Import services/interfaces from other modules
- ✅ Call methods from other modules

**Forbidden:**
- ❌ Do not write/modify source code in other modules
- ❌ Do not add methods to other modules' services
- ❌ Do not change other modules' implementations

---

## 🎯 Rule 4: Feature-Driven Development
**Development Scope:** Only implement features that are explicitly requested.

**Example Workflow:**
1. User requests: "Build Route Search Engine for transportation"
2. Goal: Only implement Route Search Engine
3. Do NOT add: Booking confirmation, Payment integration, etc. (unless requested)
4. If user requests additional features later, those become new goals

**Principle:** No scope creep. Stick to what's asked.

---

## 🎨 Rule 5: Design Consistency with Home Page
**Requirement:** All Transportation UI layouts must match the Home page (Home/Index.cshtml) styling and color scheme.

**Color Palette (from travelmate.css):**
- **Primary Blue**: `#4158ff`, `#4057ff`, `#435bff`, `#304bff`, `#1d3cff` (gradients to `#2746d8`)
- **Dark Text**: `#06143b` (main dark color)
- **Light Text**: `#6d7895`, `#071437`
- **Light Background**: `#ffffff`, `#f3f5fb`, `#eef2ff`
- **Border/Divider**: `#cfd5e6`, `#c5cffd`, `#a7b6ff`
- **Accent**: `#5468ff` (gradient component)

**Typography:**
- Font Family: `'Segoe UI', Arial, sans-serif` with `'Brush Script MT', cursive` for stylistic titles
- Button Styles: `.main-btn` (solid blue), `.outline-btn` (white/transparent border)

**Component Styles to Follow:**
- **Hero Section**: Linear gradient overlay `rgba(3,21,54,0.75)` with background image
- **Cards**: White background with subtle shadows `0 18px 45px rgba(21,35,80,0.15)`
- **Buttons**: Rounded corners (12-15px), hover effect with `transform: translateY(-3px)`
- **Search Box**: White background, left-aligned map icon, flex layout with dividers
- **Navigation**: Light gray hover state `#eef2ff` with icon + text

**Do NOT:**
- ❌ Use different color schemes
- ❌ Introduce new primary colors
- ❌ Use different font families (unless for specific stylistic elements like titles)
- ❌ Ignore hover/active states
- ❌ Create disconnected UI from Home page aesthetic

---

## ✅ Development Checklist
Before starting any feature:
- [ ] Feature is for Transportation module only
- [ ] Feature is explicitly requested by user
- [ ] Not modifying any protected/other-module files
- [ ] If accessing other modules, only reading, not writing
- [ ] Creating necessary new files only within transportation scope
- [ ] Layout and colors match Home page design (Rule 5)

---

## 📁 Allowed Project Structure Example
```
TravelPlanningSystem/
├── Services/
│   └── Transportation/
│       ├── ITransportationService.cs (NEW - OK)
│       ├── RouteSearchEngine.cs (NEW - OK)
│       └── ...
├── Models/
│   └── Transportation/ (NEW folder - OK)
│       ├── Route.cs
│       └── ...
└── Views/
	└── Transportation/ (NEW folder - OK)
		└── ...
```

---

## 🚨 Common Violations to Avoid
- ❌ Modifying `Services/Hotels/HotelService.cs`
- ❌ Changing `Models/Flight.cs`
- ❌ Editing files in `Services/Booking/` (unless in Transportation context)
- ❌ Adding features not requested (e.g., loyalty points, insurance options)
- ❌ Modifying views outside the three allowed files without permission

---

## 📝 Questions Before Implementation?
If unclear whether something violates these rules:
1. Reference this document
2. Check: Is it transportation-only?
3. Check: Is it explicitly requested?
4. Check: Will it modify protected files or other modules?
5. If doubt → Ask user for clarification before proceeding

---

**Last Updated:** Upon Transportation Module Initiation  
**Status:** ✅ Active - All rules enforced during development
