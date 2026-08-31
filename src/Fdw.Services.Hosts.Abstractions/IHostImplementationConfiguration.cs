using System.Collections.Generic;
using Fdw.Configuration;

namespace Fdw.Services.Hosts.Abstractions;

/// <summary>
/// The contract every host implementation's configuration satisfies.
/// </summary>
/// <remarks>
/// These are the settings a host reads while building its request pipeline, before any platform
/// store is reachable. In reference-api today they are split between literals in <c>Program.cs</c>
/// and loose <c>GetSection(...).Get&lt;T&gt;()</c> calls outside any ServiceType phase.
/// </remarks>
public interface IHostImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Gets or sets the route prefix every endpoint is mounted under.</summary>
    string RoutePrefix { get; set; }

    /// <summary>Gets or sets the claim type carrying role membership.</summary>
    string RoleClaimType { get; set; }

    /// <summary>Gets the origins CORS permits.</summary>
    IList<string> AllowedOrigins { get; }

    /// <summary>Gets the proxy networks whose forwarded headers are trusted, in CIDR form.</summary>
    IList<string> TrustedProxyNetworks { get; }

    /// <summary>Gets the routes accepted with no request body.</summary>
    IList<string> BodylessRoutes { get; }

    /// <summary>Gets or sets a value indicating whether response buffering is enabled.</summary>
    bool ResponseBufferingEnabled { get; set; }

    /// <summary>Gets or sets a value indicating whether security headers are written.</summary>
    bool SecurityHeadersEnabled { get; set; }
}
