using System;
using Fdw.Data;

namespace Fdw.Services.Messaging;

/// <summary>
/// A row of msg.UiFeedback: one UI comment, without its tags or picture (FDW-2143).
/// </summary>
[GenerateMapper]
public sealed class UiFeedbackRow
{
    /// <summary>Gets or sets the comment's identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the tenant the comment belongs to: the author's active tenant when it was left.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Gets or sets the person who left the comment.</summary>
    public Guid AuthorUserId { get; set; }

    /// <summary>Gets or sets the author's name as it was when they left the comment.</summary>
    public string AuthorName { get; set; } = string.Empty;

    /// <summary>Gets or sets the address of the screen.</summary>
    public string Screen { get; set; } = string.Empty;

    /// <summary>Gets or sets the route pattern of the screen.</summary>
    public string Route { get; set; } = string.Empty;

    /// <summary>Gets or sets the distance of the marked area from the window's left edge.</summary>
    public double AreaX { get; set; }

    /// <summary>Gets or sets the distance of the marked area from the window's top edge.</summary>
    public double AreaY { get; set; }

    /// <summary>Gets or sets the width of the marked area.</summary>
    public double AreaW { get; set; }

    /// <summary>Gets or sets the height of the marked area.</summary>
    public double AreaH { get; set; }

    /// <summary>Gets or sets the width of the window.</summary>
    public int ViewportW { get; set; }

    /// <summary>Gets or sets the height of the window.</summary>
    public int ViewportH { get; set; }

    /// <summary>Gets or sets the theme the screen was in, or <see langword="null"/> when none was stated.</summary>
    public string? Theme { get; set; }

    /// <summary>Gets or sets the console's description of the nearest element, as the JSON text it sent; nothing here reads it.</summary>
    public string? Anchor { get; set; }

    /// <summary>Gets or sets the note.</summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>Gets or sets the urgency's name.</summary>
    public string Urgency { get; set; } = string.Empty;

    /// <summary>Gets or sets the state: Open or Resolved.</summary>
    public string State { get; set; } = string.Empty;

    /// <summary>Gets or sets the person who resolved the comment.</summary>
    public Guid? ResolvedByUserId { get; set; }

    /// <summary>Gets or sets the name of the person who resolved the comment.</summary>
    public string? ResolvedByName { get; set; }

    /// <summary>Gets or sets when the comment was resolved.</summary>
    public DateTimeOffset? ResolvedAt { get; set; }

    /// <summary>Gets or sets when the comment was left.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets when the note was last changed.</summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>Gets or sets whether a picture of the screen is kept in msg.UiFeedbackScreenshot.</summary>
    public bool HasScreenshot { get; set; }
}
