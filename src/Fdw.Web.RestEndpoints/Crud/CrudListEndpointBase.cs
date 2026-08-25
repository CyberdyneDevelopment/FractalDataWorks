using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Settings;
using Fdw.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Fdw.Web.RestEndpoints.Caching;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Fdw.Web.RestEndpoints.Logging;
using Fdw.Web.RestEndpoints.Models;
using FdwErrorResponse = Fdw.Web.RestEndpoints.Models.ErrorResponse;

namespace Fdw.Web.RestEndpoints.Crud;

/// <summary>
/// Abstract base class for list/summary endpoints that return a collection of resources.
/// Provides configurable routing, policy-based authorization, error handling,
/// and default pagination via <c>?skip=0&amp;take=100</c> query string parameters.
/// </summary>
/// <typeparam name="TSummary">The summary DTO type returned in the list.</typeparam>
public abstract class CrudListEndpointBase<TSummary> : EndpointWithoutRequest<List<TSummary>>
    where TSummary : class
{
    /// <summary>
    /// Gets the plural resource name used for routing and policy generation (e.g., "connections", "datastores").
    /// </summary>
    protected abstract string ResourceName { get; }

    /// <summary>Gets the documentation tag this endpoint appears under.</summary>
    /// <remarks>
    /// Derived from the resource, like the route and the policy beside it, rather than stated by
    /// each endpoint. Stated, it was the same string repeated for every endpoint over a resource -
    /// and a wrong route 404s where a wrong tag just drops the endpoint out of its group in the
    /// documentation, with nothing to say so.
    /// </remarks>
    protected virtual string EndpointTag => ResourceName;

    /// <summary>
    /// Gets the read policy for this endpoint. Defaults to "{ResourceName}:read".
    /// Override to customize authorization.
    /// </summary>
    protected virtual string ReadPolicy => $"{ResourceName}:read";

    /// <summary>
    /// Gets the route for this endpoint. Defaults to "/{ResourceName}".
    /// Override to customize the route pattern.
    /// </summary>
    protected virtual string Route => $"/{ResourceName}";

    /// <summary>
    /// Gets the endpoint summary for OpenAPI documentation.
    /// </summary>
    protected virtual string EndpointSummary => $"List {ResourceName}";

    /// <summary>
    /// Gets the endpoint description for OpenAPI documentation.
    /// </summary>
    protected virtual string EndpointDescription => $"Returns a paginated list of {ResourceName}.";

    /// <summary>
    /// Gets whether ETag-based conditional GET is enabled for this endpoint.
    /// When true and an <see cref="IETagProvider"/> is registered, the endpoint checks
    /// the If-None-Match header and returns 304 Not Modified when the ETag matches.
    /// Default is true. Override to disable.
    /// </summary>
    protected virtual bool ETagEnabled => true;

    /// <summary>
    /// Gets the container name used for ETag generation.
    /// Defaults to <see cref="ResourceName"/>. Override if the data container differs from the route resource.
    /// </summary>
    protected virtual string ETagContainerName => ResourceName;

    /// <summary>
    /// Gets the connection name used for ETag generation.
    /// Defaults to "Default". Override to target a specific connection.
    /// </summary>
    protected virtual string ETagConnectionName => "Default";

    /// <summary>
    /// Gets the logger instance. Resolved during HandleAsync.
    /// </summary>
    protected new ILogger Logger { get; private set; } = null!;

    /// <inheritdoc/>
    public override void Configure()
    {
        Get(Route);
#if DEVELOP
        AllowAnonymous();
#else
        Policies(ReadPolicy);
#endif
        Summary(s =>
        {
            s.Summary = EndpointSummary;
            s.Description = EndpointDescription;
        });

        Description(x => x.WithTags(EndpointTag));

    ConfigureEndpoint();
    }

    /// <summary>
    /// Additional endpoint-specific configuration. Override for custom setup
    /// such as caching, throttling, or tags.
    /// </summary>
    protected virtual void ConfigureEndpoint()
    {
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(CancellationToken ct)
    {
        Logger = Resolve<ILoggerFactory>().CreateLogger(GetType());

        try
        {
            if (ETagEnabled)
            {
                var etagProvider = TryResolve<IETagProvider>();
                if (etagProvider is not null)
                {
                    var etag = await etagProvider.GetETag(ETagContainerName, ETagConnectionName, ct)
                        .ConfigureAwait(false);

                    if (etag is not null)
                    {
                        var ifNoneMatch = HttpContext.Request.Headers.IfNoneMatch.ToString();
                        if (string.Equals(etag, ifNoneMatch, StringComparison.Ordinal))
                        {
                            ETagLogger.ETagComputed(Logger, ETagContainerName, etag);
                            HttpContext.Response.StatusCode = 304;
                            return;
                        }

                        HttpContext.Response.Headers.ETag = etag;
                        HttpContext.Response.Headers.CacheControl = "private, must-revalidate";
                    }
                }
            }

            var paging = ParsePaginationFromQuery();
            EndpointLogger.PaginatedListRequest(Logger, GetType().Name, paging.Skip, paging.EffectiveTake);

            var result = await LoadItems(paging, ct).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                await SendErrorResponse(result, ct).ConfigureAwait(false);
                return;
            }

            var allItems = result.Value!;

            var totalCount = allItems.Count;
            var pagedItems = allItems
                .Skip(paging.Skip)
                .Take(paging.EffectiveTake)
                .ToList();

            EndpointLogger.PaginatedListResponse(Logger, GetType().Name, pagedItems.Count, totalCount);

            var response = PaginatedResponse<TSummary>.Create(
                pagedItems,
                paging.Skip,
                paging.EffectiveTake,
                totalCount);

            await SendPaginatedResponse(response, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            var msg = EndpointLogger.EndpointError(Logger, ex, GetType().Name);
            HttpContext.Response.StatusCode = 500;
            HttpContext.Response.ContentType = "application/json";
            await HttpContext.Response.WriteAsJsonAsync(new FdwErrorResponse
            {
                Code = msg.Code ?? "INTERNAL_ERROR",
                Message = msg.Message,
                ReferenceId = HttpContext.TraceIdentifier,
                IsRetryable = false,
                Action = "Contact your administrator"
            }, ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Loads the list of resource summaries with pagination context.
    /// The default implementation delegates to <see cref="LoadItems(CancellationToken)"/>
    /// for backward compatibility. Override to implement server-side pagination
    /// using the paging parameters directly.
    /// </summary>
    /// <param name="paging">The pagination parameters from the query string.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A result containing the list of summaries.</returns>
    protected virtual Task<IGenericResult<List<TSummary>>> LoadItems(PaginatedListRequest paging, CancellationToken ct)
    {
        return LoadItems(ct);
    }

    /// <summary>
    /// Loads the list of resource summaries. Override to implement data retrieval.
    /// </summary>
    /// <remarks>
    /// When only this method is overridden, pagination is applied in-memory by the base class.
    /// For server-side pagination, override <see cref="LoadItems(PaginatedListRequest, CancellationToken)"/> instead.
    /// </remarks>
    protected abstract Task<IGenericResult<List<TSummary>>> LoadItems(CancellationToken ct);

    /// <summary>
    /// Sends the paginated response. Override to customize the response format.
    /// </summary>
    /// <param name="response">The paginated response to send.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous send operation.</returns>
    protected virtual Task SendPaginatedResponse(PaginatedResponse<TSummary> response, CancellationToken ct)
        => HttpContext.Response.WriteAsJsonAsync(response, ct);

    /// <summary>
    /// Sends an error response when LoadItems fails. Override to customize error format.
    /// </summary>
    protected virtual Task SendErrorResponse(IGenericResult result, CancellationToken ct)
    {
        var (statusCode, errorResponse) = ResultHttpStatusMapper.Map(result, HttpContext);
        HttpContext.Response.StatusCode = statusCode;
        return HttpContext.Response.WriteAsJsonAsync(errorResponse, ct);
    }

    /// <summary>
    /// Parses pagination parameters from the query string.
    /// When <see cref="IEffectiveSettingsProvider"/> is registered, the effective
    /// <c>MaxPaginationSize</c> setting (resolved per tenant/role) caps the take value.
    /// </summary>
    private PaginatedListRequest ParsePaginationFromQuery()
    {
        var query = HttpContext.Request.Query;

        int skip = 0;
        int? take = null;

        if (query.TryGetValue("skip", out var skipValue)
            && int.TryParse(skipValue.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedSkip)
            && parsedSkip >= 0)
        {
            skip = parsedSkip;
        }

        if (query.TryGetValue("take", out var takeValue)
            && int.TryParse(takeValue.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedTake)
            && parsedTake > 0)
        {
            take = parsedTake;
        }

        var maxTake = ResolveMaxPaginationSize();
        if (maxTake > 0)
        {
            var effectiveTake = take ?? PaginatedListRequest.DefaultTake;
            if (effectiveTake > maxTake)
            {
                take = maxTake;
            }
        }

        return new PaginatedListRequest { Skip = skip, Take = take };
    }

    /// <summary>
    /// Resolves the effective max pagination size from the layered settings system.
    /// Returns 0 if the settings provider is not registered or the setting is not defined.
    /// </summary>
    private int ResolveMaxPaginationSize()
    {
        var settingsProvider = TryResolve<IEffectiveSettingsProvider>();
        if (settingsProvider is null)
        {
            return 0;
        }

        Guid? tenantId = null;
        string? roleName = null;

        var tenantClaim = HttpContext.User.FindFirst(ClaimDefinitions.tenantId.Name)?.Value;
        if (!string.IsNullOrEmpty(tenantClaim) && Guid.TryParse(tenantClaim, out var parsedTenantId))
        {
            tenantId = parsedTenantId;
        }

        var roleClaim = HttpContext.User.FindFirst(ClaimTypes.Role)?.Value;
        if (!string.IsNullOrEmpty(roleClaim))
        {
            roleName = roleClaim;
        }

        return settingsProvider.GetEffectiveValue<int>(
            SettingDefinitions.MaxPaginationSize,
            tenantId,
            roleName);
    }
}

/// <summary>
/// Abstract base class for paginated list endpoints.
/// Extends CrudListEndpointBase with request-based filtering, pagination,
/// and conditional GET via ETag/If-None-Match headers.
/// </summary>
/// <typeparam name="TListRequest">The list request type with pagination/filter parameters.</typeparam>
/// <typeparam name="TSummary">The summary DTO type returned in the list.</typeparam>
public abstract class CrudListEndpointBase<TListRequest, TSummary> : Endpoint<TListRequest, List<TSummary>>
    where TListRequest : notnull, new()
    where TSummary : class
{
    /// <summary>
    /// Gets the plural resource name used for routing and policy generation.
    /// </summary>
    protected abstract string ResourceName { get; }

    /// <summary>Gets the documentation tag this endpoint appears under.</summary>
    /// <remarks>Derived from the resource, like the route and policy beside it.</remarks>
    protected virtual string EndpointTag => ResourceName;

    /// <summary>
    /// Gets the read policy for this endpoint.
    /// </summary>
    protected virtual string ReadPolicy => $"{ResourceName}:read";

    /// <summary>
    /// Gets the route for this endpoint.
    /// </summary>
    protected virtual string Route => $"/{ResourceName}";

    /// <summary>
    /// Gets the endpoint summary for OpenAPI documentation.
    /// </summary>
    protected virtual string EndpointSummary => $"List {ResourceName}";

    /// <summary>
    /// Gets the endpoint description for OpenAPI documentation.
    /// </summary>
    protected virtual string EndpointDescription => $"Returns a list of {ResourceName} with optional filtering.";

    /// <summary>
    /// Gets whether ETag-based conditional GET is enabled for this endpoint.
    /// When true and an <see cref="IETagProvider"/> is registered, the endpoint checks
    /// the If-None-Match header and returns 304 Not Modified when the ETag matches.
    /// Default is true. Override to disable.
    /// </summary>
    protected virtual bool ETagEnabled => true;

    /// <summary>
    /// Gets the container name used for ETag generation.
    /// Defaults to <see cref="ResourceName"/>. Override if the data container differs from the route resource.
    /// </summary>
    protected virtual string ETagContainerName => ResourceName;

    /// <summary>
    /// Gets the connection name used for ETag generation.
    /// Defaults to "Default". Override to target a specific connection.
    /// </summary>
    protected virtual string ETagConnectionName => "Default";

    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    protected new ILogger Logger { get; private set; } = null!;

    /// <inheritdoc/>
    public override void Configure()
    {
        Get(Route);
#if DEVELOP
        AllowAnonymous();
#else
        Policies(ReadPolicy);
#endif
        Summary(s =>
        {
            s.Summary = EndpointSummary;
            s.Description = EndpointDescription;
        });

        Description(x => x.WithTags(EndpointTag));

    ConfigureEndpoint();
    }

    /// <summary>
    /// Additional endpoint-specific configuration.
    /// </summary>
    protected virtual void ConfigureEndpoint()
    {
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(TListRequest req, CancellationToken ct)
    {
        Logger = Resolve<ILoggerFactory>().CreateLogger(GetType());

        try
        {
            if (ETagEnabled)
            {
                var etagProvider = TryResolve<IETagProvider>();
                if (etagProvider is not null)
                {
                    var etag = await etagProvider.GetETag(ETagContainerName, ETagConnectionName, ct)
                        .ConfigureAwait(false);

                    if (etag is not null)
                    {
                        var ifNoneMatch = HttpContext.Request.Headers.IfNoneMatch.ToString();
                        if (string.Equals(etag, ifNoneMatch, StringComparison.Ordinal))
                        {
                            ETagLogger.ETagComputed(Logger, ETagContainerName, etag);
                            HttpContext.Response.StatusCode = 304;
                            return;
                        }

                        HttpContext.Response.Headers.ETag = etag;
                        HttpContext.Response.Headers.CacheControl = "private, must-revalidate";
                    }
                }
            }

            var result = await LoadItems(req, ct).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                await SendErrorResponse(result, ct).ConfigureAwait(false);
                return;
            }

            var items = result.Value!;

            // Why: consistent with the EndpointWithoutRequest variant (line 160-166) — wrap items
            // in a PaginatedResponse envelope so clients always see {items, skip, take, totalCount, hasMore},
            // never a bare array. LoadItems implementations apply filtering/sorting but return the full
            // matching set; the envelope reports totalCount as items.Count because server-side paging
            // is the override's responsibility when needed.
            var totalCount = items.Count;
            var response = PaginatedResponse<TSummary>.Create(items, 0, totalCount, totalCount);
            await HttpContext.Response.WriteAsJsonAsync(response, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            var msg = EndpointLogger.EndpointError(Logger, ex, GetType().Name);
            HttpContext.Response.StatusCode = 500;
            HttpContext.Response.ContentType = "application/json";
            await HttpContext.Response.WriteAsJsonAsync(new FdwErrorResponse
            {
                Code = msg.Code ?? "INTERNAL_ERROR",
                Message = msg.Message,
                ReferenceId = HttpContext.TraceIdentifier,
                IsRetryable = false,
                Action = "Contact your administrator"
            }, ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Loads the list of resource summaries based on the request parameters.
    /// </summary>
    protected abstract Task<IGenericResult<List<TSummary>>> LoadItems(TListRequest request, CancellationToken ct);

    /// <summary>
    /// Sends an error response when LoadItems fails.
    /// </summary>
    protected virtual Task SendErrorResponse(IGenericResult result, CancellationToken ct)
    {
        var (statusCode, errorResponse) = ResultHttpStatusMapper.Map(result, HttpContext);
        HttpContext.Response.StatusCode = statusCode;
        return HttpContext.Response.WriteAsJsonAsync(errorResponse, ct);
    }
}
