# Meeting Room Reservation System

A web application for booking office meeting rooms. Users can browse rooms, book them, manage their
reservations and see a usage report that shows which rooms are often booked but rarely used.

| Part | Folder | Tech |
| --- | --- | --- |
| Back end | `MeetingRooms.Api` | ASP.NET Core 8 Web API, EF Core in-memory database |
| Back-end tests | `MeetingRooms.Api.Tests` | xUnit |
| Front end | `meeting-rooms-react` | React 19, Vite, React Router, Bootstrap 5, Bootstrap Icons |

## Running the app

Requirements: .NET 8 SDK and Node.js 20+.

```bash
# 1. Back end (http://localhost:5228, Swagger UI at /swagger)
cd MeetingRooms.Api
dotnet run --launch-profile http

# 2. Front end (http://localhost:5173), in a second terminal
cd meeting-rooms-react
npm install
npm run dev

# Tests
dotnet test MeetingRooms.sln
```

The Vite dev server forwards every `/api` request to the back end, so no URLs are hard-coded in the
React code. The database is seeded on start-up with 6 rooms, 7 users and about four weeks of
meeting history, with times relative to "now" so the data always looks current. One meeting
is in progress when the app starts, so "End early" can be tried straight away.

## Features

- **Rooms**: name, unique code, capacity, floor, equipment and active/inactive status. Filter by
  minimum capacity, equipment and floor. Inactive rooms cannot be booked.
- **Users**: name, unique code and number of active reservations (booked and not finished yet).
- **Reservations**: room, organizer, title, start/end, attendees and status (Booked, Completed,
  Cancelled). Create, edit, cancel and end a meeting early. Filter by room, organizer, status and
  date range.
- **Room usage report**: booked hours vs. hours actually used per room, cancellations and meetings
  ended early. Rooms below a configurable utilization threshold are flagged as underused. The
  report can be exported to PDF using the browser's print dialog ("Save as PDF").

## Booking rules

Every new or edited reservation is validated in the back end. A broken rule rejects the request with
an HTTP error and a JSON body that the front end shows to the user:

```json
{ "code": "CAPACITY_EXCEEDED", "message": "Room 006 holds up to 4 people, but 30 attendees were requested.", "rule": "R5" }
```

| # | Rule | Error code | HTTP |
| --- | --- | --- | --- |
| R1 | Max duration 7 days (exactly 7 is allowed) | `MAX_DURATION_EXCEEDED` | 400 |
| R2 | No overlapping bookings of a room (cancelled ones don't count) | `ROOM_ALREADY_BOOKED` | 409 |
| R3 | No overlapping meetings for one organizer, in any room | `ORGANIZER_DOUBLE_BOOKED` | 409 |
| R4 | Start must be in the future, and end after start | `START_NOT_IN_FUTURE`, `INVALID_TIME_RANGE` | 400 |
| R5 | Attendees must not exceed room capacity | `CAPACITY_EXCEEDED` | 400 |
| R6 | Start at most 90 days ahead | `BEYOND_BOOKING_HORIZON` | 400 |
| R7 | Only active rooms can be booked | `ROOM_INACTIVE` | 400 |

The limits are configured in `MeetingRooms.Api/appsettings.json` and checked when the app starts:

```json
"BookingRules": { "MaxDurationDays": 7, "BookingHorizonDays": 90 },
"UsageReport": { "LowUtilizationPercent": 60, "DefaultPeriodDays": 30 }
```

Other errors use the same shape, e.g. `VALIDATION_ERROR`, `ROOM_NOT_FOUND`, `CANNOT_CANCEL`,
`CANNOT_END_EARLY`.

## API

| Method | Endpoint | Description |
| --- | --- | --- |
| GET | `/api/rooms?minCapacity=&equipment=&floor=&activeOnly=` | Rooms (filtered); `equipment` can be repeated |
| GET | `/api/rooms/floors` | Floors for the filter |
| GET | `/api/users?search=` | Users with their active reservation count |
| GET | `/api/reservations?roomId=&organizerId=&status=&from=&to=` | Reservations (filtered) |
| GET | `/api/reservations/{id}` | One reservation |
| GET | `/api/reservations/rules` | Configured booking limits |
| POST | `/api/reservations` | Create a reservation |
| PUT | `/api/reservations/{id}` | Edit an upcoming reservation |
| POST | `/api/reservations/{id}/cancel` | Cancel an upcoming reservation |
| POST | `/api/reservations/{id}/end` | End a meeting in progress early |
| GET | `/api/reports/room-usage?from=&to=` | Room usage report |

All times are UTC (ISO 8601). The front end converts them to and from the browser's local time.

## Architecture and decisions

### Back end

```
Controllers   thin HTTP layer: bind the request, call a service, return the result
Services      business logic (RoomService, UserService, ReservationService, UsageReportService)
  Rules       one class per booking rule + BookingValidator that runs them
Mapping       entity -> DTO extension methods
Dtos          request/response contracts (entities are never returned directly)
Models        EF Core entities
Data          DbContext + seeder
Errors        ApiException, ApiError and a global exception handler
Options       strongly typed settings bound from appsettings.json
```

- **All calculations happen in the back end**: filtering, active reservation counts, effective
  status, the allowed actions per reservation (`canEdit`, `canCancel`, `canEndEarly`) and the usage
  report. The front end only displays the results.
- **Status is derived, not swept.** A `Booked` reservation whose end time has passed is reported as
  `Completed`, so no background job is needed to update statuses.
- **Ending early** sets `EndedAt` and status `Completed` but keeps the original `EndTime`. The room
  becomes free from `EndedAt`, and the report can compare booked time (`EndTime - StartTime`) with
  used time (`EndedAt - StartTime`).
- **Usage report**: "booked" counts completed meetings (cancelled ones release the room and are
  reported separately). Utilization = used hours / booked hours. Rooms below `LowUtilizationPercent`
  are flagged as underused.
- **Time** comes from an injected `TimeProvider`, so time-based rules are unit-tested with a fake
  clock.
- **Overlap** uses half-open intervals (`start < otherEnd && otherStart < end`), so back-to-back
  meetings (10:00–11:00 and 11:00–12:00) are allowed.
- **Equipment** is a `[Flags]` enum stored in one column and exposed as a list of names
  (`["projector", "whiteboard"]`).

### Front end

- `src/api.js`: the only place that talks to the server. It turns error responses into an
  `ApiError` with `code`, `message` and `rule`.
- `src/useApi.js`: a small hook that loads data, reloads when filters change and ignores late
  responses from older requests.
- `src/pages/*`: one component per page (Rooms, Reservations, Users, Usage).
- `src/dates.js`: converts between UTC from the API and local time in the browser.
- Bootstrap handles layout, the modal and responsiveness, with a custom dark/gold theme in `style.css`.

### Design patterns

- **Strategy / Specification**: each booking rule implements `IBookingRule`. `BookingValidator`
  runs all registered rules in order. To add a rule you write one class and register it in
  `Program.cs`; nothing else changes.
- **Dependency injection** for services, rules, options and the clock.
- **Service layer + DTOs** keep controllers thin and keep entities out of the API contract.
- **Options pattern** with validation on start-up for the configurable limits.
- **Centralized error handling** (`IExceptionHandler`) gives every error the same response shape.
- **Database seeder** for demo data.

## Tests

`MeetingRooms.Api.Tests` has 26 xUnit tests. They run against an in-memory database with a fake
clock and cover every rule and its edge cases (exactly 7 days, exactly 90 days, back-to-back
meetings, a cancelled reservation not blocking the room, a meeting ended early freeing the room,
editing without conflicting with itself), plus cancel, end early, derived status, filters, active
counts and the usage report.

## Possible improvements

- Calendar/timeline view of a room's day or week
- TypeScript on the front end
- Persistent database (SQL Server/PostgreSQL) instead of in-memory
- Integration tests for the HTTP layer (`WebApplicationFactory`)
