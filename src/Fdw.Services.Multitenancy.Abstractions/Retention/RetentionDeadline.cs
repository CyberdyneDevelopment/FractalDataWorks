using System;

namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// The answer of <see cref="RetentionClock"/>: the date a document is kept until, or the date it needs the uploader to state (FDW-1575).
/// </summary>
/// <param name="KeepUntil">The date the document is kept until, or null when the document lacks the date the period counts from.</param>
/// <param name="MissingStartField">The field the uploader must state before the period can count, or null when the deadline is known.</param>
public sealed record RetentionDeadline(DateTimeOffset? KeepUntil, string? MissingStartField)
{
    /// <summary>Gets a value indicating whether the document has a deadline.</summary>
    public bool IsKnown => KeepUntil is not null;
}
