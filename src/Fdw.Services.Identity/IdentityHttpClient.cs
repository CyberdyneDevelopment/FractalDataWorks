using System;
using Microsoft.Extensions.DependencyInjection;

namespace Fdw.Services.Identity;

/// <summary>
/// The named <c>HttpClient</c> every identity mechanism uses to reach its OAuth 2.0 token endpoint.
/// </summary>
/// <remarks>
/// Registered through <c>IHttpClientFactory</c> rather than constructed per service so that socket
/// handlers are pooled and recycled. A service that news up its own <c>HttpClient</c> per identity
/// either exhausts sockets or pins stale DNS, and the identity provider is exactly the dependency
/// that must not become the reason outbound calls stop working.
/// </remarks>
public static class IdentityHttpClient
{
    /// <summary>The name this client is registered and resolved under.</summary>
    public const string Name = "Fdw.Identity.OAuth2";

    /// <summary>
    /// How long a token-endpoint call may take before it is treated as unreachable.
    /// </summary>
    /// <remarks>
    /// Why bounded, and why short: this call sits in front of an outbound request that has its own
    /// deadline. An unbounded wait here turns a slow identity provider into a stalled caller, which is
    /// harder to diagnose than a clean failure.
    /// </remarks>
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Registers the named client, idempotently — every identity option calls this, and a deployment
    /// running several mechanisms must not end up with competing registrations.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    public static void Register(IServiceCollection services)
        => services.AddHttpClient(Name, client => client.Timeout = Timeout);
}
