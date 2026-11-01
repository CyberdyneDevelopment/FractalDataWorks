using System;

namespace Fdw.Services.Messaging;

/// <summary>
/// The columns a resolve or a reopen of a UI comment writes (FDW-2143). Every property is written, so a reopen states null for who and when it was resolved
/// and clears them.
/// </summary>
internal sealed class UiFeedbackStateUpdate
{
    /// <summary>Gets or sets the new state: Open or Resolved.</summary>
    public string State { get; set; } = string.Empty;

    /// <summary>Gets or sets who resolved the comment; null when it is reopened.</summary>
    public Guid? ResolvedByUserId { get; set; }

    /// <summary>Gets or sets the name of who resolved the comment; null when it is reopened.</summary>
    public string? ResolvedByName { get; set; }

    /// <summary>Gets or sets when the comment was resolved; null when it is reopened.</summary>
    public DateTimeOffset? ResolvedAt { get; set; }
}
