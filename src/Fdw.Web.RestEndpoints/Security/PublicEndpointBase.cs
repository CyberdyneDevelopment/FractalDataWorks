using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;

namespace Fdw.Web.RestEndpoints.Security;

/// <summary>
/// Abstract base class for public endpoints that allow anonymous access with standard rate limiting.
/// No authentication is required. Rate limiting defaults to <see cref="RateLimitPolicyNames.Standard"/> (100 req/min).
/// </summary>
/// <typeparam name="TResponse">The response DTO type.</typeparam>
public abstract class PublicEndpointBase<TResponse> : EndpointWithoutRequest<TResponse>
    where TResponse : class
{
    /// <summary>
    /// Gets the route for this endpoint (e.g., "/public/data").
    /// </summary>
    protected abstract string Route { get; }

    /// <summary>
    /// Gets the rate limit policy name. Defaults to <see cref="RateLimitPolicyNames.Standard"/>.
    /// Override to change or set to <see langword="null"/> to disable rate limiting.
    /// </summary>
    protected virtual string? RateLimitPolicy => RateLimitPolicyNames.Standard;

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
        AllowAnonymous();

        if (!string.IsNullOrEmpty(RateLimitPolicy))
        {
            Options(x => x.RequireRateLimiting(RateLimitPolicy));
        }

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
/// Abstract base class for public endpoints with a request body that allow anonymous access with standard rate limiting.
/// No authentication is required. Rate limiting defaults to <see cref="RateLimitPolicyNames.Standard"/> (100 req/min).
/// </summary>
/// <typeparam name="TRequest">The request DTO type.</typeparam>
/// <typeparam name="TResponse">The response DTO type.</typeparam>
public abstract class PublicEndpointBase<TRequest, TResponse> : Endpoint<TRequest, TResponse>
    where TRequest : notnull, new()
    where TResponse : class
{
    /// <summary>
    /// Gets the route for this endpoint (e.g., "/public/search").
    /// </summary>
    protected abstract string Route { get; }

    /// <summary>
    /// Gets the rate limit policy name. Defaults to <see cref="RateLimitPolicyNames.Standard"/>.
    /// Override to change or set to <see langword="null"/> to disable rate limiting.
    /// </summary>
    protected virtual string? RateLimitPolicy => RateLimitPolicyNames.Standard;

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
        AllowAnonymous();

        if (!string.IsNullOrEmpty(RateLimitPolicy))
        {
            Options(x => x.RequireRateLimiting(RateLimitPolicy));
        }

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
