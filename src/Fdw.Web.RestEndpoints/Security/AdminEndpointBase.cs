using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;

namespace Fdw.Web.RestEndpoints.Security;

/// <summary>
/// Abstract base class for administrative endpoints that require the admin authorization policy.
/// Rate limiting defaults to <see cref="RateLimitPolicyNames.Admin"/> (10000 req/min).
/// In DEVELOP mode, anonymous access is allowed for local development.
/// </summary>
/// <typeparam name="TResponse">The response DTO type.</typeparam>
public abstract class AdminEndpointBase<TResponse> : EndpointWithoutRequest<TResponse>
    where TResponse : class
{
    /// <summary>
    /// Gets the route for this endpoint (e.g., "/admin/data").
    /// </summary>
    protected abstract string Route { get; }

    /// <summary>
    /// Gets the admin authorization policy. Defaults to "configurations:write".
    /// Override to customize the policy name.
    /// </summary>
    protected virtual string AdminPolicy => "configurations:write";

    /// <summary>
    /// Gets the rate limit policy name. Defaults to <see cref="RateLimitPolicyNames.Admin"/>.
    /// Override to change or set to <see langword="null"/> to disable rate limiting.
    /// </summary>
    protected virtual string? RateLimitPolicy => RateLimitPolicyNames.Admin;

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
        Policies(AdminPolicy);
        // Why: PermissionClaimsPreProcessor checks the baked perm claims in the JWT before
        // executing the handler. Because the base class owns the Policies(...) call, it also
        // owns registering the pre-processor so every admin endpoint gets it automatically.
        Definition.PreProcessors(Order.Before, new PermissionClaimsPreProcessor());
#endif

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
/// Abstract base class for administrative endpoints with a request body.
/// Rate limiting defaults to <see cref="RateLimitPolicyNames.Admin"/> (10000 req/min).
/// In DEVELOP mode, anonymous access is allowed for local development.
/// </summary>
/// <typeparam name="TRequest">The request DTO type.</typeparam>
/// <typeparam name="TResponse">The response DTO type.</typeparam>
public abstract class AdminEndpointBase<TRequest, TResponse> : Endpoint<TRequest, TResponse>
    where TRequest : notnull, new()
    where TResponse : class
{
    /// <summary>
    /// Gets the route for this endpoint.
    /// </summary>
    protected abstract string Route { get; }

    /// <summary>
    /// Gets the admin authorization policy. Defaults to "configurations:write".
    /// Override to customize the policy name.
    /// </summary>
    protected virtual string AdminPolicy => "configurations:write";

    /// <summary>
    /// Gets the rate limit policy name. Defaults to <see cref="RateLimitPolicyNames.Admin"/>.
    /// Override to change or set to <see langword="null"/> to disable rate limiting.
    /// </summary>
    protected virtual string? RateLimitPolicy => RateLimitPolicyNames.Admin;

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
        Policies(AdminPolicy);
        // Why: see AdminEndpointBase<TResponse>.Configure() — same rationale.
        Definition.PreProcessors(Order.Before, new PermissionClaimsPreProcessor());
#endif

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
