using System;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace Fdw.Hosting.UiHost.Endpoints.CookieSignInEndpointOptions;

/// <summary>
/// Base for the cookie sign-in endpoint options.
/// </summary>
public abstract class CookieSignInEndpointBase : EndpointTypeOptionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CookieSignInEndpointBase"/> class.
    /// </summary>
    /// <param name="name">The option name.</param>
    /// <param name="endpointType">The endpoint type this option declares.</param>
    /// <param name="description">The description.</param>
    protected CookieSignInEndpointBase(string name, Type endpointType, string description)
        : base(name, endpointType, description, "CookieSignInEndpoint")
    {
    }
}

/// <summary>
/// Base for a cookie sign-in endpoint option that names its endpoint by type.
/// </summary>
/// <typeparam name="TEndpoint">The endpoint type.</typeparam>
public abstract class CookieSignInEndpointBase<TEndpoint> : CookieSignInEndpointBase
    where TEndpoint : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CookieSignInEndpointBase{TEndpoint}"/> class.
    /// </summary>
    protected CookieSignInEndpointBase()
        : base(DeriveName(typeof(TEndpoint)), typeof(TEndpoint), $"The {DeriveName(typeof(TEndpoint))} endpoint.")
    {
    }
}
