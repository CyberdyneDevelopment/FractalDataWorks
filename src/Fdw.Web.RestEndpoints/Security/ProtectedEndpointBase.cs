using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Fdw.Web.RestEndpoints.Security;

/// <summary>
/// Abstract base class for RBAC-protected endpoints that require authentication and a specific permission policy.
/// No rate limiting is applied (RBAC-protected endpoints are typically server-to-server or internal).
/// In DEVELOP mode, anonymous access is allowed for local development.
/// </summary>
/// <typeparam name="TResponse">The response DTO type.</typeparam>
public abstract class ProtectedEndpointBase<TResponse> : EndpointWithoutRequest<TResponse>
    where TResponse : class
{
    /// <summary>
    /// Gets the route for this endpoint (e.g., "/protected").
    /// </summary>
    protected abstract string Route { get; }

    /// <summary>
    /// Gets the resource name used for policy generation (e.g., "connections", "datastores").
    /// The <see cref="ReadPolicy"/> is derived from this value.
    /// </summary>
    protected abstract string ResourceName { get; }

    /// <summary>
    /// Gets the RBAC read policy for this endpoint. Defaults to "{ResourceName}:read".
    /// Override to customize the policy name.
    /// </summary>
    protected virtual string ReadPolicy => $"{ResourceName}:read";

    /// <summary>
    /// Gets the endpoint summary for OpenAPI documentation.
    /// </summary>
    protected virtual string EndpointSummary => string.Empty;

    /// <summary>
    /// Gets the endpoint description for OpenAPI documentation.
    /// </summary>
    protected virtual string EndpointDescription => string.Empty;

    /// <summary>
    /// Gets the endpoint tag for OpenAPI documentation grouping.
    /// </summary>
    protected virtual string? EndpointTag => null;

    /// <inheritdoc/>
    public override void Configure()
    {
        Get(Route);
#if DEVELOP
        AllowAnonymous();
#else
        Policies(ReadPolicy);
#endif

        if (!string.IsNullOrEmpty(EndpointSummary) || !string.IsNullOrEmpty(EndpointDescription))
        {
            Summary(s =>
            {
                s.Summary = EndpointSummary;
                s.Description = EndpointDescription;
            });
        }

        if (!string.IsNullOrEmpty(EndpointTag))
        {
            Description(x => x.WithTags(EndpointTag));
        }

        ConfigureEndpoint();
    }

    /// <summary>
    /// Additional endpoint-specific configuration. Override for custom setup
    /// such as caching, response codes, or example requests.
    /// </summary>
    protected virtual void ConfigureEndpoint()
    {
    }
}

/// <summary>
/// Abstract base class for RBAC-protected endpoints with a request body.
/// No rate limiting is applied. In DEVELOP mode, anonymous access is allowed for local development.
/// </summary>
/// <typeparam name="TRequest">The request DTO type.</typeparam>
/// <typeparam name="TResponse">The response DTO type.</typeparam>
public abstract class ProtectedEndpointBase<TRequest, TResponse> : Endpoint<TRequest, TResponse>
    where TRequest : notnull, new()
    where TResponse : class
{
    /// <summary>
    /// Gets the route for this endpoint.
    /// </summary>
    protected abstract string Route { get; }

    /// <summary>
    /// Gets the resource name used for policy generation (e.g., "connections", "datastores").
    /// The <see cref="ReadPolicy"/> is derived from this value.
    /// </summary>
    protected abstract string ResourceName { get; }

    /// <summary>
    /// Gets the RBAC read policy for this endpoint. Defaults to "{ResourceName}:read".
    /// Override to customize the policy name.
    /// </summary>
    protected virtual string ReadPolicy => $"{ResourceName}:read";

    /// <summary>
    /// Gets the endpoint summary for OpenAPI documentation.
    /// </summary>
    protected virtual string EndpointSummary => string.Empty;

    /// <summary>
    /// Gets the endpoint description for OpenAPI documentation.
    /// </summary>
    protected virtual string EndpointDescription => string.Empty;

    /// <summary>
    /// Gets the endpoint tag for OpenAPI documentation grouping.
    /// </summary>
    protected virtual string? EndpointTag => null;

    /// <inheritdoc/>
    public override void Configure()
    {
        Get(Route);
#if DEVELOP
        AllowAnonymous();
#else
        Policies(ReadPolicy);
#endif

        if (!string.IsNullOrEmpty(EndpointSummary) || !string.IsNullOrEmpty(EndpointDescription))
        {
            Summary(s =>
            {
                s.Summary = EndpointSummary;
                s.Description = EndpointDescription;
            });
        }

        if (!string.IsNullOrEmpty(EndpointTag))
        {
            Description(x => x.WithTags(EndpointTag));
        }

        ConfigureEndpoint();
    }

    /// <summary>
    /// Additional endpoint-specific configuration. Override for custom setup
    /// such as caching, response codes, or example requests.
    /// </summary>
    protected virtual void ConfigureEndpoint()
    {
    }
}
