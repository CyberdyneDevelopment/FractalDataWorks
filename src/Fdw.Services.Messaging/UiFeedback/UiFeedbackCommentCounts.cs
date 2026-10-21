using System;
using System.Linq;

namespace Fdw.Services.Messaging;

/// <summary>
/// How many comments some UI notes have, read once for a page (FDW-2248).
/// </summary>
/// <param name="Rows">One mark for each comment, grouped by note.</param>
internal sealed record UiFeedbackCommentCounts(ILookup<Guid, UiFeedbackCommentMarkRow> Rows)
{
    /// <summary>Gets a value with no comments on any note.</summary>
    public static UiFeedbackCommentCounts None { get; } = new(Enumerable.Empty<UiFeedbackCommentMarkRow>().ToLookup(row => row.FeedbackId));

    /// <summary>Gets how many comments a note has.</summary>
    /// <param name="feedbackId">The note.</param>
    /// <returns>The number of comments.</returns>
    public int CountOf(Guid feedbackId) => Rows[feedbackId].Count();
}
