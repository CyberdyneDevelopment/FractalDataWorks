using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Authentication.Sessions;

/// <summary>The single column putting a session to sleep touches on <c>auth.Session</c>.</summary>
[ExcludeFromCodeCoverage]
public sealed class SessionSleepUpdate
{
    /// <summary>Gets or sets when the session was put to sleep.</summary>
    public DateTimeOffset AsleepAt { get; set; }
}
