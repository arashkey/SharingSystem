using SharingSystem.Application.Common;
using SharingSystem.Domain.Enums;

namespace SharingSystem.Application.Shares;

/// <summary>
/// Converts an <see cref="ExpirationRequest"/> into an absolute UTC date and enforces the configured maximum.
/// </summary>
public static class ExpirationCalculator
{
    public const int MaxValue = 999;

    public static DateTime? Calculate(ExpirationRequest request, DateTime utcNow, int maxExpirationHours)
    {
        if (request.NeverExpires)
        {
            if (maxExpirationHours > 0)
            {
                throw AppException.Validation(
                    $"A share must expire. The maximum expiration is {maxExpirationHours} hours.");
            }

            return null;
        }

        if (request.Value is < 1 or > MaxValue)
        {
            throw AppException.Validation($"The expiration must be between 1 and {MaxValue}.");
        }

        var expiresAt = request.Unit switch
        {
            ExpirationUnit.Minutes => utcNow.AddMinutes(request.Value),
            ExpirationUnit.Hours => utcNow.AddHours(request.Value),
            ExpirationUnit.Days => utcNow.AddDays(request.Value),
            ExpirationUnit.Weeks => utcNow.AddDays(request.Value * (double)DaysPerWeek),
            ExpirationUnit.Months => utcNow.AddMonths(request.Value),
            ExpirationUnit.Years => utcNow.AddYears(request.Value),
            _ => throw AppException.Validation("The expiration unit is invalid.")
        };

        if (maxExpirationHours > 0 && expiresAt > utcNow.AddHours(maxExpirationHours))
        {
            throw AppException.Validation($"The expiration cannot be longer than {maxExpirationHours} hours.");
        }

        return expiresAt;
    }

    private const int DaysPerWeek = 7;
}
