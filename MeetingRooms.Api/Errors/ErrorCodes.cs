namespace MeetingRooms.Api.Errors;

public static class ErrorCodes
{
    // Booking rules
    public const string MaxDurationExceeded = "MAX_DURATION_EXCEEDED";          // R1
    public const string RoomAlreadyBooked = "ROOM_ALREADY_BOOKED";              // R2
    public const string OrganizerDoubleBooked = "ORGANIZER_DOUBLE_BOOKED";      // R3
    public const string InvalidTimeRange = "INVALID_TIME_RANGE";                // R4
    public const string StartNotInFuture = "START_NOT_IN_FUTURE";               // R4
    public const string CapacityExceeded = "CAPACITY_EXCEEDED";                 // R5
    public const string BeyondBookingHorizon = "BEYOND_BOOKING_HORIZON";        // R6
    public const string RoomInactive = "ROOM_INACTIVE";                         // R7

    // Other errors
    public const string ValidationError = "VALIDATION_ERROR";
    public const string RoomNotFound = "ROOM_NOT_FOUND";
    public const string UserNotFound = "USER_NOT_FOUND";
    public const string ReservationNotFound = "RESERVATION_NOT_FOUND";
    public const string ReservationNotEditable = "RESERVATION_NOT_EDITABLE";
    public const string CannotCancel = "CANNOT_CANCEL";
    public const string CannotEndEarly = "CANNOT_END_EARLY";
    public const string InvalidDateRange = "INVALID_DATE_RANGE";
}
