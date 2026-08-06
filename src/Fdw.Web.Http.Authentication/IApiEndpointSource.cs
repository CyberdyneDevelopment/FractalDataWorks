namespace Fdw.Web.Http.Authentication;

/// <summary>
/// A source of API client endpoints beyond the host's own configuration.
/// </summary>
/// <remarks>
/// <para>
/// Why this interface exists rather than <see cref="ApiEndpointRegistration"/> reading the configuration
/// gateway directly: an endpoint is configuration, and configuration in this framework is read through
/// the gateway — but this package holds the bearer handler and carries three project references. Reaching
/// the gateway from here would drag the Connections, Data and Commands stacks into every host that only
/// wanted to attach a token. The dependency is inverted instead: this package states what it needs, and a
/// package that already knows about connections supplies it.
/// </para>
/// <para>
/// A host that registers no implementation resolves endpoints from its own configuration alone, which is
/// what an app with no configuration store (or one still bootstrapping) requires.
/// </para>
/// </remarks>
public interface IApiEndpointSource
{
    /// <summary>
    /// Resolves the endpoint declared for <paramref name="clientName"/>, or null when this source
    /// declares none for it.
    /// </summary>
    /// <param name="clientName">The API client's name.</param>
    /// <returns>The declared base URL, or null when this source has no endpoint for that client.</returns>
    /// <remarks>
    /// Returning null means "this source does not declare one", which is a real answer and lets the caller
    /// consult the next declared source. It does not mean "use a default" — when no source declares an
    /// endpoint, <see cref="ApiEndpointRegistration.ResolveEndpoint"/> throws rather than inventing one.
    /// </remarks>
    string? Resolve(string clientName);
}
