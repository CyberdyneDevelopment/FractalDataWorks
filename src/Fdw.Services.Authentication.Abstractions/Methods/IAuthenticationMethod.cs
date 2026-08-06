using Fdw.Collections;

namespace Fdw.Services.Authentication.Abstractions;

/// <summary>
/// Interface for authentication method types.
/// Defines the contract for authentication methods supported by the framework.
/// </summary>
public interface IAuthenticationMethod : ITypeOption<int>
{
    /// <summary>
    /// Gets whether this authentication method requires user interaction.
    /// </summary>
    bool RequiresUserInteraction { get; }

    /// <summary>
    /// Gets whether this authentication method supports token refresh.
    /// </summary>
    bool SupportsTokenRefresh { get; }

    /// <summary>
    /// Gets whether this authentication method supports multi-tenant scenarios.
    /// </summary>
    bool SupportsMultiTenant { get; }

    /// <summary>
    /// Gets the authentication scheme name used by ASP.NET Core.
    /// </summary>
    string? AuthenticationScheme { get; }

    /// <summary>
    /// Gets the priority of this authentication method (higher values = higher priority).
    /// </summary>
    int Priority { get; }
}