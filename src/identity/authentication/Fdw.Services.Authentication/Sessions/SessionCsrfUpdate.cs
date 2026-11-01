using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Authentication.Sessions;

/// <summary>The single column issuing a CSRF token touches on <c>auth.Session</c>.</summary>
[ExcludeFromCodeCoverage]
public sealed class SessionCsrfUpdate
{
    /// <summary>Gets or sets the SHA-256 of the new CSRF token.</summary>
    public byte[] CsrfTokenHash { get; set; } = [];
}
