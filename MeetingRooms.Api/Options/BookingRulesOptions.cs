using System.ComponentModel.DataAnnotations;

namespace MeetingRooms.Api.Options;

// Limits used by the booking rules, bound from the "BookingRules" section of appsettings.json
public class BookingRulesOptions
{
    public const string SectionName = "BookingRules";

    // R1: a single reservation cannot last longer than this (exactly this long is allowed)
    [Range(1, 365)]
    public int MaxDurationDays { get; set; } = 7;

    // R6: a reservation cannot start more than this many days in the future
    [Range(1, 3650)]
    public int BookingHorizonDays { get; set; } = 90;
}
