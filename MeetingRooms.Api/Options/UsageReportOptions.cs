using System.ComponentModel.DataAnnotations;

namespace MeetingRooms.Api.Options;

// Settings for the room usage report, bound from the "UsageReport" section of appsettings.json
public class UsageReportOptions
{
    public const string SectionName = "UsageReport";

    // Rooms that use less than this share of their booked time are flagged as underused
    [Range(1, 100)]
    public int LowUtilizationPercent { get; set; } = 60;

    // Period covered by the report when no dates are given
    [Range(1, 365)]
    public int DefaultPeriodDays { get; set; } = 30;
}
