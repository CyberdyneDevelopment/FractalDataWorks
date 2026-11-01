using System;
using Fdw.Data;

namespace Fdw.Services.Messaging;

/// <summary>
/// A row of msg.UiFeedbackTag: one tag of a UI comment (FDW-2143).
/// </summary>
[GenerateMapper]
public sealed class UiFeedbackTagRow
{
    /// <summary>Gets or sets the comment the tag is on.</summary>
    public Guid FeedbackId { get; set; }

    /// <summary>Gets or sets the tag's name, an option of the UiFeedbackTags type collection.</summary>
    public string Tag { get; set; } = string.Empty;
}
