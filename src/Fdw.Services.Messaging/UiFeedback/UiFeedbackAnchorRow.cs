using System;
using Fdw.Data;

namespace Fdw.Services.Messaging;

/// <summary>
/// A row of msg.UiFeedbackAnchor: one component a UI comment is about, in the order the reviewer chose it (FDW-2343).
/// </summary>
[GenerateMapper]
public sealed class UiFeedbackAnchorRow
{
    /// <summary>Gets or sets the comment the component belongs to.</summary>
    public Guid FeedbackId { get; set; }

    /// <summary>Gets or sets the position among the comment's components, from 0.</summary>
    public int Ordinal { get; set; }

    /// <summary>Gets or sets the console's description of the component, as the JSON text it sent.</summary>
    public string Anchor { get; set; } = string.Empty;
}
