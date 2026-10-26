using System;
using System.Collections.Generic;

namespace Fdw.Services.Messaging;

/// <summary>
/// A UI comment as the API answers it (FDW-2143): a post-it a tester pinned to an area of a screen. It never carries the picture; that is read on its own.
/// </summary>
public sealed class UiFeedbackPayload
{
    /// <summary>Gets or sets the comment's identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the address of the screen the comment was left on.</summary>
    public string Screen { get; set; } = string.Empty;

    /// <summary>Gets or sets the route pattern of that screen, such as <c>/datasets/$name</c>.</summary>
    public string Route { get; set; } = string.Empty;

    /// <summary>Gets or sets the area the tester marked.</summary>
    public UiFeedbackArea Area { get; set; } = new();

    /// <summary>Gets or sets the size of the window the tester was looking at.</summary>
    public UiFeedbackSize Viewport { get; set; } = new();

    /// <summary>Gets or sets the theme the screen was in, or <see langword="null"/> when none was stated.</summary>
    public string? Theme { get; set; }

    /// <summary>Gets or sets the console's description of each component the comment is about, as the JSON text it was sent, in the order it was sent; nothing here reads them.</summary>
    public IReadOnlyList<string> AnchorsJson { get; set; } = [];

    /// <summary>Gets or sets the tester's note.</summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>Gets or sets the urgency, an option of the UiFeedbackUrgencies type collection.</summary>
    public string Urgency { get; set; } = string.Empty;

    /// <summary>Gets or sets the tags, options of the UiFeedbackTags type collection.</summary>
    public IReadOnlyList<string> Tags { get; set; } = [];

    /// <summary>Gets or sets the name of the person who left the comment, as it was when they left it.</summary>
    public string AuthorName { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the person asking left the comment.</summary>
    public bool IsMine { get; set; }

    /// <summary>Gets or sets when the comment was left.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets when the note was last changed, or <see langword="null"/> when it never was.</summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>Gets or sets where the comment stands: Open or Resolved.</summary>
    public string State { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the person who resolved the comment.</summary>
    public string? ResolvedByName { get; set; }

    /// <summary>Gets or sets when the comment was resolved.</summary>
    public DateTimeOffset? ResolvedAt { get; set; }

    /// <summary>Gets or sets whether a picture of the screen was kept with the comment.</summary>
    public bool HasScreenshot { get; set; }

    /// <summary>Gets or sets whether the person asking has acknowledged the comment.</summary>
    public bool AcknowledgedByMe { get; set; }

    /// <summary>Gets or sets how many people agree with the comment; the person who left it is not counted (FDW-2247).</summary>
    public int AgreeCount { get; set; }

    /// <summary>Gets or sets whether the person asking agrees with the comment (FDW-2247).</summary>
    public bool AgreedByMe { get; set; }

    /// <summary>Gets or sets how many comments the note has (FDW-2248).</summary>
    public int CommentCount { get; set; }
}
