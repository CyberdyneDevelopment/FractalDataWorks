using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Fdw.Services.Messaging.Endpoints.Models;

/// <summary>
/// A UI comment as the API answers it. It never carries the picture; that is read from <c>/ui-feedback/{id}/screenshot</c>.
/// </summary>
public class UiFeedbackResponse
{
    /// <summary>Gets or sets the comment's identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the address of the screen.</summary>
    public string Screen { get; set; } = string.Empty;

    /// <summary>Gets or sets the route pattern of the screen.</summary>
    public string Route { get; set; } = string.Empty;

    /// <summary>Gets or sets the area marked.</summary>
    public UiFeedbackArea Area { get; set; } = new();

    /// <summary>Gets or sets the size of the window.</summary>
    public UiFeedbackSize Viewport { get; set; } = new();

    /// <summary>Gets or sets the theme, or <see langword="null"/> when none was stated.</summary>
    public string? Theme { get; set; }

    /// <summary>Gets or sets the console's description of each component the comment is about, exactly as it was sent, in the order it was sent.</summary>
    public IReadOnlyList<JsonElement> Anchors { get; set; } = [];

    /// <summary>Gets or sets the note.</summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>Gets or sets the urgency.</summary>
    public string Urgency { get; set; } = string.Empty;

    /// <summary>Gets or sets the tags.</summary>
    public IReadOnlyList<string> Tags { get; set; } = [];

    /// <summary>Gets or sets the author's name.</summary>
    public string AuthorName { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the person asking left the comment.</summary>
    public bool IsMine { get; set; }

    /// <summary>Gets or sets when the comment was left.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets when the note was last changed.</summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>Gets or sets the state: Open or Resolved.</summary>
    public string State { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the person who resolved the comment.</summary>
    public string? ResolvedByName { get; set; }

    /// <summary>Gets or sets when the comment was resolved.</summary>
    public DateTimeOffset? ResolvedAt { get; set; }

    /// <summary>Gets or sets whether a picture of the screen is kept.</summary>
    public bool HasScreenshot { get; set; }

    /// <summary>Gets or sets whether the person asking has acknowledged the comment.</summary>
    public bool AcknowledgedByMe { get; set; }

    /// <summary>Gets or sets how many people agree with the comment; the person who left it is not counted.</summary>
    public int AgreeCount { get; set; }

    /// <summary>Gets or sets whether the person asking agrees with the comment.</summary>
    public bool AgreedByMe { get; set; }

    /// <summary>Gets or sets how many comments the note has.</summary>
    public int CommentCount { get; set; }

    /// <summary>
    /// Makes the answer for a comment.
    /// </summary>
    /// <param name="payload">The comment.</param>
    /// <returns>The response.</returns>
    public static UiFeedbackResponse Of(UiFeedbackPayload payload)
    {
        return new UiFeedbackResponse
        {
            Id = payload.Id,
            Screen = payload.Screen,
            Route = payload.Route,
            Area = payload.Area,
            Viewport = payload.Viewport,
            Theme = payload.Theme,
            Anchors = [.. payload.AnchorsJson.Select(AnchorOf)],
            Body = payload.Body,
            Urgency = payload.Urgency,
            Tags = payload.Tags,
            AuthorName = payload.AuthorName,
            IsMine = payload.IsMine,
            CreatedAt = payload.CreatedAt,
            UpdatedAt = payload.UpdatedAt,
            State = payload.State,
            ResolvedByName = payload.ResolvedByName,
            ResolvedAt = payload.ResolvedAt,
            HasScreenshot = payload.HasScreenshot,
            AcknowledgedByMe = payload.AcknowledgedByMe,
            AgreeCount = payload.AgreeCount,
            AgreedByMe = payload.AgreedByMe,
            CommentCount = payload.CommentCount
        };
    }

    private static JsonElement AnchorOf(string anchorJson)
    {
        using var document = JsonDocument.Parse(anchorJson);
        return document.RootElement.Clone();
    }
}
