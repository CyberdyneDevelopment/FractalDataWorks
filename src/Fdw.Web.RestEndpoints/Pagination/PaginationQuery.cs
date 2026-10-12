using System;
using System.Globalization;
using System.Security.Claims;
using Fdw.Claims.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Settings;
using Fdw.Web.RestEndpoints.Logging;
using Fdw.Web.RestEndpoints.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fdw.Web.RestEndpoints.Pagination;

/// <summary>
/// Reads <c>?skip=</c> and <c>?take=</c> from a list request and resolves the page size from settings.
/// </summary>
/// <remarks>
/// Why one reader for every list endpoint: the parsing lived privately in one CRUD base, so every
/// other list endpoint answered in the paged envelope while returning the whole table with
/// <c>skip 0, take = count</c>. The envelope promised paging nothing applied. One reader means a
/// list endpoint pages by calling it rather than by re-implementing it.
/// <para>
/// Why the page sizes come from settings and nothing else: they were the constants 100 and 1000,
/// used whenever <see cref="SettingDefinitions.MaxPaginationSize"/> was absent, and no list endpoint
/// ever passed a settings provider, so the setting was never read and nothing could change either
/// number. Both sizes are now <see cref="SettingDefinitions.DefaultPaginationSize"/> and
/// <see cref="SettingDefinitions.MaxPaginationSize"/>, resolved for the caller's tenant and role, and a
/// list request fails when either is missing.
/// </para>
/// <para>
/// Why the settings provider is read from the request's services: every list endpoint calls this
/// reader with its <see cref="HttpContext"/>, so resolving the provider here reaches all of them at
/// once. Passed in by each endpoint instead, it was an optional argument none of them supplied.
/// </para>
/// <para>
/// Why a caller an API key authenticated is sized from other settings (FDW-1209): a key is a service running unattended,
/// and a page cut short is wrong data to it, where the limits exist to protect an interactive caller.
/// <see cref="SettingDefinitions.ApiKeyDefaultPaginationSize"/> and <see cref="SettingDefinitions.ApiKeyMaxPaginationSize"/>
/// are resolved like the two above, and 0 means no limit. A key is told apart by the scheme that authenticated it
/// (<see cref="ApiKeyAuthenticationSchemes.Authenticated"/>), not by a role, which a person signed in holds too.
/// </para>
/// </remarks>
public static class PaginationQuery
{
    /// <summary>
    /// Reads the page a list request asks for and resolves its size from settings.
    /// </summary>
    /// <param name="context">The current request.</param>
    /// <param name="logger">The calling endpoint's logger.</param>
    /// <returns>
    /// The page to cut; a failure when <c>skip</c> or <c>take</c> is not a valid number, or when a
    /// paging setting is missing.
    /// </returns>
    public static IGenericResult<PaginatedListRequest> Parse(HttpContext context, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(logger);

        if (ReadNumber(context.Request.Query, "skip", 0, logger, out var skip) is { } skipInvalid)
            return GenericResult<PaginatedListRequest>.Failure(skipInvalid);

        if (ReadNumber(context.Request.Query, "take", 1, logger, out var take) is { } takeInvalid)
            return GenericResult<PaginatedListRequest>.Failure(takeInvalid);

        return Resolve(context, logger, skip.GetValueOrDefault(), take);
    }

    /// <summary>
    /// Resolves the page for a skip and an optional requested take, sizing it from settings.
    /// </summary>
    /// <param name="context">The current request; its services supply the settings provider and its user the tenant and role.</param>
    /// <param name="logger">The calling endpoint's logger.</param>
    /// <param name="skip">How many items to skip; already validated as zero or more.</param>
    /// <param name="requestedTake">The take the caller asked for, or <see langword="null"/> when it asked for none.</param>
    /// <returns>The page to cut, its take capped at <see cref="SettingDefinitions.MaxPaginationSize"/>; a failure when a paging setting is missing.</returns>
    /// <remarks>
    /// For endpoints whose request declares its own paging fields, so their page sizes come from the
    /// same settings as every other list.
    /// </remarks>
    public static IGenericResult<PaginatedListRequest> Resolve(HttpContext context, ILogger logger, int skip, int? requestedTake)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(logger);

        if (context.RequestServices.GetService<IEffectiveSettingsProvider>() is not { } settings)
            return GenericResult<PaginatedListRequest>.Failure(
                EndpointLogger.PaginationSettingsUnavailable(logger, nameof(IEffectiveSettingsProvider)));

        var (tenantId, roleName) = Caller(context);

        return ApiKeyAuthenticationSchemes.Authenticated(context.User)
            ? ResolveForApiKey(settings, logger, skip, requestedTake, tenantId, roleName)
            : ResolveForUser(settings, logger, skip, requestedTake, tenantId, roleName);
    }

    /// <summary>
    /// Resolves the size of the batches a read of every row is cut into, which is not a limit on what the caller gets.
    /// </summary>
    /// <param name="context">The current request.</param>
    /// <param name="logger">The calling endpoint's logger.</param>
    /// <returns>The batch size: the default page the interactive settings give the caller's tenant and role, cut at their largest page; a failure when either setting is missing.</returns>
    /// <remarks>
    /// For an endpoint that reads page after page to answer a <see cref="PaginatedListRequest.IsUnlimited"/> request, or to
    /// stream an export. Always the interactive pair, whoever asks: the batch is how much is held in memory at once, which is
    /// not what an API key's limit settings decide, and there is one number to tune and one way for it to be missing.
    /// </remarks>
    public static IGenericResult<int> ResolveBatchSize(HttpContext context, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(logger);

        if (context.RequestServices.GetService<IEffectiveSettingsProvider>() is not { } settings)
            return GenericResult<int>.Failure(
                EndpointLogger.PaginationSettingsUnavailable(logger, nameof(IEffectiveSettingsProvider)));

        var (tenantId, roleName) = Caller(context);
        var batch = ResolveForUser(settings, logger, 0, null, tenantId, roleName);
        return batch.IsFailure
            ? batch.ToNewResult<int>()
            : GenericResult<int>.Success(batch.Value!.Take);
    }

    // A caller an API key authenticated: no limit unless the ApiKey settings set one. 0 is how an operator says so, and a
    // setting that is absent, unreadable or negative is refused, never read as 0.
    private static IGenericResult<PaginatedListRequest> ResolveForApiKey(
        IEffectiveSettingsProvider settings, ILogger logger, int skip, int? requestedTake, Guid? tenantId, string? roleName)
    {
        var maxTake = ApiKeyLimit(settings, logger, SettingDefinitions.ApiKeyMaxPaginationSize, tenantId, roleName);
        if (maxTake.IsFailure)
            return maxTake.ToNewResult<PaginatedListRequest>();

        var defaultTake = ApiKeyLimit(settings, logger, SettingDefinitions.ApiKeyDefaultPaginationSize, tenantId, roleName);
        if (defaultTake.IsFailure)
            return defaultTake.ToNewResult<PaginatedListRequest>();

        EndpointLogger.ApiKeyPaginationLimits(logger, defaultTake.Value, maxTake.Value);

        if (requestedTake is null && defaultTake.Value == 0 && maxTake.Value == 0)
        {
            EndpointLogger.ApiKeyPaginationUnlimited(logger, skip);
            return GenericResult<PaginatedListRequest>.Success(
                new PaginatedListRequest { Skip = skip, Take = PaginatedListRequest.NoLimitTake, IsUnlimited = true });
        }

        var wanted = requestedTake is { } asked ? asked : WithoutAsking(defaultTake.Value);
        if (maxTake.Value > 0 && wanted > maxTake.Value)
        {
            EndpointLogger.PaginationTakeCapped(logger, wanted, maxTake.Value);
            wanted = maxTake.Value;
        }

        return GenericResult<PaginatedListRequest>.Success(new PaginatedListRequest { Skip = skip, Take = wanted });
    }

    // The take of a request that named none: the default page, or every row when the default is 0 (a largest page, if one is
    // set, still cuts it).
    private static int WithoutAsking(int defaultTake)
        => defaultTake == 0 ? PaginatedListRequest.NoLimitTake : defaultTake;

    private static IGenericResult<int> ApiKeyLimit(
        IEffectiveSettingsProvider settings, ILogger logger, string settingName, Guid? tenantId, string? roleName)
        => settings.TryGetEffectiveValue<int>(settingName, out var limit, tenantId, roleName) && limit >= 0
            ? GenericResult<int>.Success(limit)
            : GenericResult<int>.Failure(EndpointLogger.ApiKeyPaginationSettingInvalid(logger, settingName));

    private static IGenericResult<PaginatedListRequest> ResolveForUser(
        IEffectiveSettingsProvider settings, ILogger logger, int skip, int? requestedTake, Guid? tenantId, string? roleName)
    {
        // Both are resolved on every request, not only when needed, so a missing setting fails the
        // first list call rather than only the ones that happen to omit ?take=.
        var maxTake = settings.GetEffectiveValue<int>(SettingDefinitions.MaxPaginationSize, tenantId, roleName);
        if (maxTake <= 0)
            return GenericResult<PaginatedListRequest>.Failure(
                EndpointLogger.PaginationSettingMissing(logger, SettingDefinitions.MaxPaginationSize));

        var defaultTake = settings.GetEffectiveValue<int>(SettingDefinitions.DefaultPaginationSize, tenantId, roleName);
        if (defaultTake <= 0)
            return GenericResult<PaginatedListRequest>.Failure(
                EndpointLogger.PaginationSettingMissing(logger, SettingDefinitions.DefaultPaginationSize));

        var take = requestedTake is { } asked ? asked : defaultTake;
        if (take > maxTake)
        {
            EndpointLogger.PaginationTakeCapped(logger, take, maxTake);
            take = maxTake;
        }

        return GenericResult<PaginatedListRequest>.Success(new PaginatedListRequest { Skip = skip, Take = take });
    }

    /// <summary>
    /// Resolves the most rows a data query may read to filter or sort on a calculated field (FDW-1045), which the caller's
    /// role, tenant or the server can set.
    /// </summary>
    /// <param name="context">The HTTP context of the request.</param>
    /// <param name="logger">The logger a failure is written to.</param>
    /// <returns>The cap, or the failure when the setting provider is absent or the setting resolves to nothing positive.</returns>
    /// <remarks>
    /// Resolved on every data query, like the pagination limits, so a missing setting fails the first query and not only
    /// the ones that happen to name a calculated field.
    /// </remarks>
    public static IGenericResult<int> ResolveCalculatedRowCap(HttpContext context, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(logger);

        if (context.RequestServices.GetService<IEffectiveSettingsProvider>() is not { } settings)
            return GenericResult<int>.Failure(
                EndpointLogger.PaginationSettingsUnavailable(logger, nameof(IEffectiveSettingsProvider)));

        var (tenantId, roleName) = Caller(context);
        var cap = settings.GetEffectiveValue<int>(SettingDefinitions.MaxCalculatedFilterRows, tenantId, roleName);
        return cap <= 0
            ? GenericResult<int>.Failure(EndpointLogger.PaginationSettingMissing(logger, SettingDefinitions.MaxCalculatedFilterRows))
            : GenericResult<int>.Success(cap);
    }

    /// <summary>
    /// Resolves a page for endpoints whose request pages by number (<c>Page</c>/<c>PageSize</c>) rather
    /// than by skip and take, sizing it from the same settings as every other list.
    /// </summary>
    /// <param name="context">The current request.</param>
    /// <param name="logger">The calling endpoint's logger.</param>
    /// <param name="page">The 1-based page number the caller asked for.</param>
    /// <param name="requestedPageSize">The page size the caller asked for, or <see langword="null"/> when it asked for none.</param>
    /// <returns>
    /// The page to cut, with <see cref="PaginatedListRequest.Skip"/> set to the items before it; a failure
    /// when the page or page size is not a positive number, or when a paging setting is missing.
    /// </returns>
    /// <remarks>
    /// Why a page below 1 or a page size below 1 is refused rather than corrected: those requests
    /// used to be quietly clamped to page 1 of 25, so a caller's mistake came back as a different
    /// page than it asked for with nothing to say so.
    /// </remarks>
    public static IGenericResult<PaginatedListRequest> ResolvePage(HttpContext context, ILogger logger, int page, int? requestedPageSize)
    {
        ArgumentNullException.ThrowIfNull(logger);

        if (page < 1)
            return GenericResult<PaginatedListRequest>.Failure(EndpointLogger.PaginationValueInvalid(
                logger, "page", page.ToString(CultureInfo.InvariantCulture), "a whole number of 1 or more"));

        if (requestedPageSize < 1)
            return GenericResult<PaginatedListRequest>.Failure(EndpointLogger.PaginationValueInvalid(
                logger, "pageSize", requestedPageSize.Value.ToString(CultureInfo.InvariantCulture), "a whole number of 1 or more"));

        var sized = Resolve(context, logger, 0, requestedPageSize);
        if (sized.IsFailure)
            return sized;

        // Why page 1 only: with no page size to multiply by, the whole list is the one page, and a later page would be a
        // multiple of the largest take -- an offset past every row, answered as an empty page the caller reads as the end.
        if (sized.Value!.IsUnlimited)
            return page == 1
                ? sized
                : GenericResult<PaginatedListRequest>.Failure(EndpointLogger.PaginationValueInvalid(
                    logger, "page", page.ToString(CultureInfo.InvariantCulture),
                    "1, because no page size applies to this caller and the whole list is one page"));

        return GenericResult<PaginatedListRequest>.Success(new PaginatedListRequest
        {
            Skip = (page - 1) * sized.Value!.Take,
            Take = sized.Value.Take,
        });
    }

    /// <summary>
    /// Reads one paging query parameter.
    /// </summary>
    /// <returns><see langword="null"/> when the parameter is absent or valid; otherwise the message refusing it.</returns>
    private static IGenericMessage? ReadNumber(IQueryCollection query, string name, int minimum, ILogger logger, out int? value)
    {
        value = null;
        if (!query.TryGetValue(name, out var raw))
            return null;

        if (int.TryParse(raw.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            && parsed >= minimum)
        {
            value = parsed;
            return null;
        }

        return EndpointLogger.PaginationValueInvalid(
            logger, name, raw.ToString(),
            string.Create(CultureInfo.InvariantCulture, $"a whole number of {minimum} or more"));
    }

    private static (Guid? TenantId, string? RoleName) Caller(HttpContext context)
    {
        Guid? tenantId = null;
        var tenantClaim = context.User.FindFirst(ClaimDefinitions.tenantId.Name)?.Value;
        if (!string.IsNullOrEmpty(tenantClaim) && Guid.TryParse(tenantClaim, out var parsedTenantId))
            tenantId = parsedTenantId;

        var roleClaim = context.User.FindFirst(ClaimTypes.Role)?.Value;
        return (tenantId, string.IsNullOrEmpty(roleClaim) ? null : roleClaim);
    }
}
