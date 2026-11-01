using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Authentication.Abstractions;

namespace Fdw.Services.Authentication.Validation;

/// <summary>
/// What the Session kind needs to read a server-side session's cookie and guard the writes it
/// authenticates (FDW-903).
/// </summary>
/// <remarks>
/// The implementation row for a <c>Session</c> authentication service,
/// <c>auth.SessionAuthenticationServiceConfiguration</c>. See
/// <see cref="ISessionAuthenticationConfiguration"/> for what each value means.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "AuthenticationService")]
public partial class SessionAuthenticationConfiguration : ISessionAuthenticationConfiguration
{
    /// <inheritdoc/>
    public string CookieName { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string SameSite { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string CsrfProtection { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string? AllowedOrigins { get; set; }

    /// <inheritdoc/>
    public string? CsrfHeaderName { get; set; }

    /// <inheritdoc/>
    public bool Enabled { get; set; }

    /// <inheritdoc/>
    public string? Authority { get; set; }

    /// <inheritdoc/>
    public string? Description { get; set; }

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Implementation { get; set; } = string.Empty;

    /// <summary>Initializes a new instance of the <see cref="SessionAuthenticationConfiguration"/> class.</summary>
    public SessionAuthenticationConfiguration()
    {
    }

    /// <inheritdoc />
    public Guid Id { get; set; }

    /// <inheritdoc />
    public string Name { get; set; } = string.Empty;
}
