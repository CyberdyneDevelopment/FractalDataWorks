using System;
using System.Collections.Generic;

namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// The date rule of a retention policy (FDW-1575): the period counts from the date the administrator designated. When the document has
/// that date, the deadline follows. When it does not, the uploader must say it; the upload date is only a suggestion to the uploader and
/// never stands in for it.
/// </summary>
/// <remarks>
/// An application's upload screen asks <see cref="DeadlineFor"/> with the dates it has, and asks the uploader for
/// <see cref="RetentionDeadline.MissingStartField"/> whenever it comes back.
/// </remarks>
public static class RetentionClock
{
    /// <summary>Works out when a document is kept until.</summary>
    /// <param name="policy">The retention policy of the document's type.</param>
    /// <param name="dates">The dates the document has, by field name.</param>
    /// <returns>The deadline, or the field the uploader must state.</returns>
    public static RetentionDeadline DeadlineFor(RetentionPolicy policy, IReadOnlyDictionary<string, DateTimeOffset> dates)
    {
        if (policy is null)
        {
            throw new ArgumentNullException(nameof(policy));
        }

        if (dates is null)
        {
            throw new ArgumentNullException(nameof(dates));
        }

        foreach (var date in dates)
        {
            if (string.Equals(date.Key, policy.StartField, StringComparison.OrdinalIgnoreCase))
            {
                return new RetentionDeadline(date.Value.AddYears(policy.Years).AddMonths(policy.Months).AddDays(policy.Days), null);
            }
        }

        return new RetentionDeadline(null, policy.StartField);
    }
}
