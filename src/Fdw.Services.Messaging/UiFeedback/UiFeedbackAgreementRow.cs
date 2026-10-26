using System;
using Fdw.Data;

namespace Fdw.Services.Messaging;

/// <summary>
/// A row of msg.UiFeedbackAgreement: one person's agreement with one UI comment (FDW-2247).
/// </summary>
[GenerateMapper]
public sealed class UiFeedbackAgreementRow
{
    /// <summary>Gets or sets the comment agreed with.</summary>
    public Guid FeedbackId { get; set; }

    /// <summary>Gets or sets the person who agrees.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets the person's name as it was when they agreed.</summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>Gets or sets when they agreed.</summary>
    public DateTimeOffset AgreedAt { get; set; }
}
