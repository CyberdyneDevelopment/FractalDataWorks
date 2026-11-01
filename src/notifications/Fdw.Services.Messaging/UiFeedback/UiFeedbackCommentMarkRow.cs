using System;
using Fdw.Data;

namespace Fdw.Services.Messaging;

/// <summary>
/// The one column of msg.UiFeedbackComment that counting needs, so a page of notes counts its comments without reading their text (FDW-2248).
/// </summary>
[GenerateMapper]
public sealed class UiFeedbackCommentMarkRow
{
    /// <summary>Gets or sets the note commented on.</summary>
    public Guid FeedbackId { get; set; }
}
