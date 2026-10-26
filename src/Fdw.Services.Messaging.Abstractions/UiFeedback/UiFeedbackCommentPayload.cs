using System;

namespace Fdw.Services.Messaging;

/// <summary>
/// A comment on a UI note, as the API answers it (FDW-2248).
/// </summary>
public class UiFeedbackCommentPayload
{
    /// <summary>Gets or sets the comment's identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the note commented on.</summary>
    public Guid FeedbackId { get; set; }

    /// <summary>Gets or sets the person who wrote the comment.</summary>
    public Guid AuthorUserId { get; set; }

    /// <summary>Gets or sets the author's name as it was when they wrote it.</summary>
    public string AuthorName { get; set; } = string.Empty;

    /// <summary>Gets or sets the text of the comment.</summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>Gets or sets when the comment was written.</summary>
    public DateTimeOffset CreatedAt { get; set; }
}
