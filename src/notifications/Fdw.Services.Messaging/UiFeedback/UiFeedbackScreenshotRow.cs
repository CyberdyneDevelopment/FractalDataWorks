using System;
using Fdw.Data;

namespace Fdw.Services.Messaging;

/// <summary>
/// A row of msg.UiFeedbackScreenshot: the picture of the screen a UI comment was left on (FDW-2143).
/// </summary>
[GenerateMapper]
public sealed class UiFeedbackScreenshotRow
{
    /// <summary>Gets or sets the comment the picture belongs to.</summary>
    public Guid FeedbackId { get; set; }

    /// <summary>Gets or sets the picture's media type.</summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>Gets or sets the picture's bytes.</summary>
    public byte[] Picture { get; set; } = [];
}
