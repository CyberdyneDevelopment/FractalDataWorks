using System;
using System.Collections.Generic;
using System.Linq;

namespace Fdw.Services.Messaging;

/// <summary>
/// Turns a UI comment from the request into the rows that keep it, and from the rows into what the API answers (FDW-2143).
/// </summary>
internal static class UiFeedbackMapping
{
    /// <summary>
    /// Makes the row for a new comment from a request that passed every check. The comment is Open, belongs to the author's tenant and carries the author's name
    /// as it is now.
    /// </summary>
    /// <param name="caller">The author.</param>
    /// <param name="request">What the tester sent.</param>
    /// <param name="hasScreenshot">Whether a picture is kept with it.</param>
    /// <returns>The row.</returns>
    public static UiFeedbackRow NewRow(UiFeedbackCaller caller, CreateUiFeedbackRequest request, bool hasScreenshot)
    {
        return new UiFeedbackRow
        {
            Id = Guid.NewGuid(),
            TenantId = caller.TenantId,
            AuthorUserId = caller.UserId,
            AuthorName = caller.Name,
            Screen = request.Screen!,
            Route = request.Route!,
            AreaX = request.Area!.X,
            AreaY = request.Area.Y,
            AreaW = request.Area.W,
            AreaH = request.Area.H,
            ViewportW = request.Viewport!.W,
            ViewportH = request.Viewport.H,
            Theme = request.Theme,
            Anchor = request.AnchorJson,
            Body = request.Body!,
            Urgency = UiFeedbackUrgencies.ByName(request.Urgency!).Name,
            State = "Open",
            CreatedAt = DateTimeOffset.UtcNow,
            HasScreenshot = hasScreenshot
        };
    }

    /// <summary>
    /// Names the tags of a request as their options spell them.
    /// </summary>
    /// <param name="request">What the tester sent, already checked.</param>
    /// <returns>The tags, in the order sent.</returns>
    public static IReadOnlyList<string> TagNames(CreateUiFeedbackRequest request)
        => request.Tags!.Select(tag => UiFeedbackTags.ByName(tag).Name).ToList();

    /// <summary>
    /// Makes what the API answers for a comment.
    /// </summary>
    /// <param name="row">The comment's row.</param>
    /// <param name="tags">The comment's tags.</param>
    /// <param name="callerUserId">The person asking, whose own comments are marked as theirs.</param>
    /// <param name="acknowledgedByCaller">Whether the person asking has acknowledged the comment.</param>
    /// <returns>The payload.</returns>
    public static UiFeedbackPayload ToPayload(UiFeedbackRow row, IEnumerable<string> tags, Guid callerUserId, bool acknowledgedByCaller)
    {
        return new UiFeedbackPayload
        {
            Id = row.Id,
            Screen = row.Screen,
            Route = row.Route,
            Area = new UiFeedbackArea { X = row.AreaX, Y = row.AreaY, W = row.AreaW, H = row.AreaH },
            Viewport = new UiFeedbackSize { W = row.ViewportW, H = row.ViewportH },
            Theme = row.Theme,
            AnchorJson = row.Anchor,
            Body = row.Body,
            Urgency = row.Urgency,
            Tags = tags.OrderBy(tag => tag, StringComparer.Ordinal).ToList(),
            AuthorName = row.AuthorName,
            IsMine = row.AuthorUserId == callerUserId,
            CreatedAt = row.CreatedAt,
            UpdatedAt = row.UpdatedAt,
            State = row.State,
            ResolvedByName = row.ResolvedByName,
            ResolvedAt = row.ResolvedAt,
            HasScreenshot = row.HasScreenshot,
            AcknowledgedByMe = acknowledgedByCaller
        };
    }
}
