using MeetingRooms.Api.Errors;

namespace MeetingRooms.Api.Services.Rules;

public interface IBookingValidator
{
    // Throws an ApiException describing the first broken rule
    Task ValidateAsync(BookingCandidate candidate, CancellationToken cancellationToken = default);
}

// Runs every registered booking rule in registration order and stops at the first violation
public class BookingValidator : IBookingValidator
{
    private readonly IEnumerable<IBookingRule> _rules;

    public BookingValidator(IEnumerable<IBookingRule> rules)
    {
        _rules = rules;
    }

    public async Task ValidateAsync(BookingCandidate candidate, CancellationToken cancellationToken = default)
    {
        foreach (var rule in _rules)
        {
            var violation = await rule.CheckAsync(candidate, cancellationToken);
            if (violation is not null)
            {
                throw new ApiException(violation.StatusCode, violation.Code, violation.Message, violation.Rule);
            }
        }
    }
}
