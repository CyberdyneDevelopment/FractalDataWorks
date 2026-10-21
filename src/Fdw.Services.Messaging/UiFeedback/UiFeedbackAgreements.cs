using System;
using System.Linq;

namespace Fdw.Services.Messaging;

/// <summary>
/// The agreements with some UI comments, read once for a page so each comment can answer how many people agree and whether the person asking does (FDW-2247).
/// </summary>
/// <param name="Rows">The agreement rows, grouped by comment.</param>
internal sealed record UiFeedbackAgreements(ILookup<Guid, UiFeedbackAgreementRow> Rows)
{
    /// <summary>Gets how many people agree with a comment.</summary>
    /// <param name="feedbackId">The comment.</param>
    /// <returns>The number of agreements.</returns>
    public int CountOf(Guid feedbackId) => Rows[feedbackId].Count();

    /// <summary>Gets whether a person agrees with a comment.</summary>
    /// <param name="feedbackId">The comment.</param>
    /// <param name="userId">The person.</param>
    /// <returns>True when they agree.</returns>
    public bool AgreedBy(Guid feedbackId, Guid userId) => Rows[feedbackId].Any(row => row.UserId == userId);
}
