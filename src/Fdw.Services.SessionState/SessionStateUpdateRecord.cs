using System;

namespace Fdw.Services.SessionState;

/// <summary>
/// Partial record used for in-place session state updates.
/// Only the mutable fields (StateValue and UpdatedAt) are included;
/// the gateway maps these to the SET clause of the UPDATE statement.
/// </summary>
public sealed class SessionStateUpdateRecord
{
    /// <summary>Gets or sets the new serialized state value.</summary>
    public string StateValue { get; set; } = string.Empty;

    /// <summary>Gets or sets the timestamp of the update.</summary>
    public DateTimeOffset UpdatedAt { get; set; }
}
