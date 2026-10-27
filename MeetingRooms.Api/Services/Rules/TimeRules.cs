using MeetingRooms.Api.Errors;
using MeetingRooms.Api.Options;
using Microsoft.Extensions.Options;

namespace MeetingRooms.Api.Services.Rules;

// R4: the end must come after the start
public class ValidTimeRangeRule : IBookingRule
{
    public Task<RuleViolation?> CheckAsync(BookingCandidate candidate, CancellationToken cancellationToken = default)
    {
        RuleViolation? violation = candidate.EndTime <= candidate.StartTime
            ? new("R4", ErrorCodes.InvalidTimeRange, "End time must be after start time.")
            : null;

        return Task.FromResult(violation);
    }
}

// R4: the start must be in the future
public class StartInFutureRule : IBookingRule
{
    private readonly TimeProvider _clock;

    public StartInFutureRule(TimeProvider clock)
    {
        _clock = clock;
    }

    public Task<RuleViolation?> CheckAsync(BookingCandidate candidate, CancellationToken cancellationToken = default)
    {
        var now = _clock.GetUtcNow().UtcDateTime;

        RuleViolation? violation = candidate.StartTime <= now
            ? new("R4", ErrorCodes.StartNotInFuture, "Start time must be in the future.")
            : null;

        return Task.FromResult(violation);
    }
}

// R6: a reservation cannot start more than BookingHorizonDays in the future
public class BookingHorizonRule : IBookingRule
{
    private readonly TimeProvider _clock;
    private readonly BookingRulesOptions _options;

    public BookingHorizonRule(TimeProvider clock, IOptions<BookingRulesOptions> options)
    {
        _clock = clock;
        _options = options.Value;
    }

    public Task<RuleViolation?> CheckAsync(BookingCandidate candidate, CancellationToken cancellationToken = default)
    {
        var latestStart = _clock.GetUtcNow().UtcDateTime.AddDays(_options.BookingHorizonDays);

        RuleViolation? violation = candidate.StartTime > latestStart
            ? new("R6", ErrorCodes.BeyondBookingHorizon,
                $"A reservation cannot start more than {_options.BookingHorizonDays} days in the future.")
            : null;

        return Task.FromResult(violation);
    }
}

// R1: a reservation cannot last longer than MaxDurationDays (exactly that long is allowed)
public class MaxDurationRule : IBookingRule
{
    private readonly BookingRulesOptions _options;

    public MaxDurationRule(IOptions<BookingRulesOptions> options)
    {
        _options = options.Value;
    }

    public Task<RuleViolation?> CheckAsync(BookingCandidate candidate, CancellationToken cancellationToken = default)
    {
        var duration = candidate.EndTime - candidate.StartTime;

        RuleViolation? violation = duration > TimeSpan.FromDays(_options.MaxDurationDays)
            ? new("R1", ErrorCodes.MaxDurationExceeded,
                $"A reservation cannot last longer than {_options.MaxDurationDays} days.")
            : null;

        return Task.FromResult(violation);
    }
}
