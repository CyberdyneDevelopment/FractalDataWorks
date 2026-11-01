using System;

namespace Fdw.Services.Messaging;

/// <summary>
/// The columns a change of a UI comment's note writes (FDW-2143).
/// </summary>
internal sealed class UiFeedbackBodyUpdate
{
    /// <summary>Gets or sets the new note.</summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>Gets or sets when the note was changed.</summary>
    public DateTimeOffset UpdatedAt { get; set; }
}
