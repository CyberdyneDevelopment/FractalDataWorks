using System;
using System.Threading;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Http;
using Fdw.Web.Http.Authentication;
using Fdw.Web.Http.Authentication.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Web.Clients.Abstractions.Registration;

/// <summary>
/// Registers the named <see cref="System.Net.Http.HttpClient"/> that backs an API client, resolving its
/// base address from the HTTP connection declared under the same name.
/// </summary>
/// <remarks>
/// <para>
/// Why this is a standalone helper rather than a method on <c>ApiClientTypeBase</c>: three options
/// register a named API client from a DIFFERENT base — the session-state and health-monitor clients —
/// and each re-implemented the rule by hand and drifted, one of them omitting the bearer handler and
/// issuing unauthenticated requests. Registering an endpoint is a property of registering an API
/// client, not of a place in the type hierarchy.
/// </para>
/// <para>
/// Why it lives here rather than beside the bearer handler: an endpoint IS a connection, so resolving
/// one means reading the connection domain. <c>Fdw.Web.Http.Authentication</c> deliberately knows
/// nothing about connections, and the previous answer to that was an <c>IApiEndpointSource</c>
/// interface it declared for someone else to implement. Nobody did — it had zero implementations, so
/// every client registered here was handed out with no BaseAddress and every request failed with an
/// invalid URI. An interface whose only implementation is the one you are about to write is not an
/// abstraction, it is an indirection; this package already references both halves, so it does the work.
/// </para>
/// </remarks>
public static class ApiEndpointRegistration
{
    /// <summary>
    /// Registers the named HTTP client for <paramref name="clientName"/> with the base address declared
    /// for it, and attaches the bearer-token handler.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="clientName">The API client's name, which is also the connection's name.</param>
    /// <returns>The service collection.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="clientName"/> is empty.</exception>
    public static IServiceCollection AddApiHttpClient(
        this IServiceCollection services,
        string clientName)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        if (string.IsNullOrEmpty(clientName)) throw new ArgumentException("A client name is required.", nameof(clientName));

        services.AddHttpClient(clientName, (sp, client) =>
        {
            var declared = ResolveEndpoint(clientName, sp);

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
    /// Resolves the base address declared for <paramref name="clientName"/> by the HTTP connection of
    /// that name, or null when none is declared.
    /// </summary>
    /// <param name="clientName">The API client's name.</param>
    /// <param name="services">The scope resolving this client.</param>
    /// <returns>The declared base URL, or null when nothing declares one.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="clientName"/> is empty.</exception>
    /// <remarks>
    /// Null stays a real answer. A host registers a client for every package it references and calls a
    /// few — reference-api registers around thirty-five and uses two — so requiring an endpoint for each
    /// would mean declaring URLs nobody resolves, and the only way to satisfy that is to invent them.
    /// Null means "nothing declares one" and the caller reports it through EndpointNotDeclared; no
    /// endpoint is ever invented.
    /// </remarks>
    public static string? ResolveEndpoint(string clientName, IServiceProvider? services = null)
    {
        if (string.IsNullOrEmpty(clientName)) throw new ArgumentException("A client name is required.", nameof(clientName));

        if (services?.GetService(typeof(IConnectionConfigurationProvider)) is not IConnectionConfigurationProvider connections)
            return null;

        // Why sync-over-async: this runs inside AddHttpClient's configure delegate, which the factory
        // invokes per CreateClient and which has no asynchronous form. Keeping the throw contained
        // matters here — an exception from this delegate surfaces during MapFastEndpoints and takes
        // endpoint mapping down for the whole host rather than failing one client.
#pragma warning disable VSTHRD002
        var result = connections.Get(clientName, CancellationToken.None).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002

        // A connection of another kind under this name declares something else, not an endpoint.
        return result.IsSuccess && result.Value is HttpConnectionConfigurationBase http
                && !string.IsNullOrWhiteSpace(http.BaseUrl)
            ? http.BaseUrl
            : null;
    }
}
