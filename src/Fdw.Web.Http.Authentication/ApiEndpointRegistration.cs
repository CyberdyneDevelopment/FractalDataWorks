using System;
using Microsoft.Extensions.Configuration;
using Fdw.Web.Http.Authentication.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Web.Http.Authentication;

/// <summary>
/// Registers the named <see cref="System.Net.Http.HttpClient"/> that backs an API client, resolving its
/// base address from the endpoint the host declared for it.
/// </summary>
/// <remarks>
/// <para>
/// Why this is a standalone helper rather than a method on <c>ApiClientTypeBase</c>: endpoint resolution
/// was previously inherited, so only options deriving from that base could reach it. The three options
/// that register a named API client from a DIFFERENT base — the session-state client
/// (<c>SessionStateServiceTypeBase</c>) and the health-monitor client (<c>HealthMonitorTypeBase</c>) —
/// each re-implemented the rule by hand and drifted: one reads a wholly different configuration section,
/// and a third client silently omitted the bearer handler and issued unauthenticated requests. Endpoint
/// registration is a property of registering an API client, not of a place in the type hierarchy, so it
/// lives where every such option can call it: beside the bearer handler they all already attach.
/// </para>
/// </remarks>
public static class ApiEndpointRegistration
{
    /// <summary>
    /// Registers the named HTTP client for <paramref name="clientName"/> with the base address the host
    /// declared for it, and attaches the bearer-token handler.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">The host's configuration.</param>
    /// <param name="clientName">The client name — both the named-HttpClient key and the configuration key.</param>
    /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <remarks>
    /// When this client is RESOLVED and no source declares an endpoint for it, an error naming the client
    /// and every satisfying key is logged and the client is left with no BaseAddress. See the remarks below.
    /// </remarks>
    /// <remarks>
    /// <para>
    /// Registration is unconditional, and the endpoint is resolved INSIDE the configure delegate — which
    /// the HTTP client factory runs on each <c>CreateClient(name)</c>, not here. That is what makes
    /// "required" mean the only thing it can honestly mean: <b>a client is required when something
    /// resolves it</b>.
    /// </para>
    /// <para>
    /// Why that distinction matters: a host registers every client type in every package it references,
    /// because module initializers auto-register each <c>[ServiceTypeOption]</c> at assembly load — a
    /// package reference IS registration. Reference.Api therefore registers ~35 client types while calling
    /// two of them. Validating at registration would force it to declare endpoints for 33 clients it never
    /// resolves, and the only way to satisfy that is an invented URL — the fallback this codebase forbids.
    /// Validating at resolution asks nothing of the clients nobody uses, and fails immediately and by name
    /// for the ones something actually asked for.
    /// </para>
    /// <para>
    /// Hosts that genuinely depend on a client surface the error at boot rather than at first request: the
    /// three-phase lifecycle's Initialize pass eagerly resolves what it depends on, so the log is emitted
    /// there. No "is this client required" flag or per-host allow-list is needed — resolution already
    /// carries that information.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddApiHttpClient(
        this IServiceCollection services,
        IConfiguration configuration,
        string clientName)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        if (configuration is null) throw new ArgumentNullException(nameof(configuration));
        if (string.IsNullOrEmpty(clientName)) throw new ArgumentException("A client name is required.", nameof(clientName));

        services.AddHttpClient(clientName, (sp, client) =>
        {
            var declared = ResolveEndpoint(configuration, clientName, sp);

            if (string.IsNullOrEmpty(declared))
            {
                ApiEndpointLog.EndpointNotDeclared(
                    sp.GetService<ILoggerFactory>() is ILoggerFactory factory
                        ? factory.CreateLogger(typeof(ApiEndpointRegistration))
                        : NullLogger.Instance,
                    clientName);
                return;
            }

            client.BaseAddress = new Uri(declared);
        }).AddBearerTokenHandler();

        return services;
    }

    /// <summary>
    /// Resolves the endpoint the host declared for <paramref name="clientName"/>.
    /// </summary>
    /// <param name="configuration">The host's configuration.</param>
    /// <param name="clientName">The client name.</param>
    /// <param name="services">
    /// The scope resolving this client, used to consult <see cref="IApiEndpointSource"/> when the host
    /// registers one. Null when no scope is available, in which case only host configuration is read.
    /// </param>
    /// <returns>The declared base URL, or null when no source declares one for this client.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <remarks>
    /// <para>
    /// This is a declared-override hierarchy — most specific declared value wins — NOT a fallback default:
    /// both keys hold values the operator wrote, and neither is invented here.
    /// </para>
    /// <para>
    /// Returning null is a real answer — "nothing declares one" — not a stand-in for a value. The caller
    /// reports it through <c>ApiEndpointLog.EndpointNotDeclared</c>, which names the client and every key
    /// that would satisfy it. No endpoint is ever invented.
    /// </para>
    /// </remarks>
    public static string? ResolveEndpoint(IConfiguration configuration, string clientName, IServiceProvider? services = null)
    {
        if (configuration is null) throw new ArgumentNullException(nameof(configuration));
        if (string.IsNullOrEmpty(clientName)) throw new ArgumentException("A client name is required.", nameof(clientName));

        // 1. This host's own override for this one client — the most specific thing an operator can write.
        var declared = configuration[$"ApiClients:{clientName}:BaseUrl"];

        // 2. The configuration store, when the host has one. An API client's endpoint is an HTTP
        //    connection, so this is the same record the connections admin screen edits.
        if (string.IsNullOrEmpty(declared) && services is not null)
        {
            declared = services.GetService(typeof(IApiEndpointSource)) is IApiEndpointSource source
                ? source.Resolve(clientName)
                : null;
        }

        // 3. This host's endpoint for all its clients.
        if (string.IsNullOrEmpty(declared))
        {
            declared = configuration["ApiClients:BaseUrl"];
        }

        return declared;
    }
}
