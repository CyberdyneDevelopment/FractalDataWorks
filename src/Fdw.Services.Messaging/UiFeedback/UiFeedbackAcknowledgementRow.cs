using System;
using Fdw.Data;

namespace Fdw.Services.Messaging;

/// <summary>
/// A row of msg.UiFeedbackAcknowledgement: one person's acknowledgement of one UI comment (FDW-2144).
/// </summary>
[GenerateMapper]
public sealed class UiFeedbackAcknowledgementRow
{
    /// <summary>Gets or sets the comment acknowledged.</summary>
    public Guid FeedbackId { get; set; }

    /// <summary>Gets or sets the person who acknowledged it.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets the person's name as it was when they acknowledged.</summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>Gets or sets when they acknowledged it.</summary>
    public DateTimeOffset AcknowledgedAt { get; set; }
}
