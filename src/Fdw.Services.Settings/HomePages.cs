using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Settings.Configuration;
using Fdw.Services.Settings.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Settings;

/// <summary>
/// The default <see cref="IHomePages"/>: one row per scope and state in the HomePage domain, found by a name made of the scope's kind and
/// id and the state (FDW-2325).
/// </summary>
/// <remarks>
/// The page that applies to a person is asked of the scopes in the order of <see cref="Order"/>, the only place that order is written.
/// A scope with no published page is skipped, and nothing is assumed in its place: when no scope holds one the answer is none.
/// </remarks>
public sealed class HomePages : IHomePages
{
    /// <summary>The scopes a person's page is looked for in, nearest first.</summary>
    public static readonly string[] Order = [HomePageKinds.Person, HomePageKinds.Organization, HomePageKinds.Tenant];

    private readonly IHomePageConfigurationProvider _pages;
    private readonly ILogger<HomePages> _logger;

    /// <summary>Initializes a new instance of the <see cref="HomePages"/> class.</summary>
    /// <param name="pages">Reads and writes the pages.</param>
    /// <param name="logger">The logger.</param>
    public HomePages(IHomePageConfigurationProvider pages, ILogger<HomePages>? logger)
    {
        _pages = pages ?? throw new ArgumentNullException(nameof(pages));
        _logger = logger ?? NullLogger<HomePages>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<HomePageSet>> Get(HomePageScope scope, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scope);

        var published = await Read(scope, HomePageStates.Published, cancellationToken).ConfigureAwait(false);
        if (published.IsFailure)
            return published.ToNewResult<HomePageSet>();

        var draft = await Read(scope, HomePageStates.Draft, cancellationToken).ConfigureAwait(false);
        return draft.IsFailure
            ? draft.ToNewResult<HomePageSet>()
            : GenericResult<HomePageSet>.Success(new HomePageSet(published.Value?.Body, draft.Value?.Body));
    }

    /// <inheritdoc />
    public async Task<IGenericResult> SaveDraft(HomePageScope scope, string body, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scope);

        var checkedBody = HomePageBody.Check(body, _logger);
        if (checkedBody.IsFailure)
            return checkedBody;

        var saved = await Write(scope, HomePageStates.Draft, body, cancellationToken).ConfigureAwait(false);
        if (saved.IsSuccess)
            HomePagesLog.DraftSaved(_logger, scope.Kind, scope.Id.ToString("D", CultureInfo.InvariantCulture));

        return saved;
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Publish(HomePageScope scope, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scope);

        var draft = await Read(scope, HomePageStates.Draft, cancellationToken).ConfigureAwait(false);
        if (draft.IsFailure)
            return draft;

        if (draft.Value is not { } held)
            return GenericResult.Failure(HomePagesLog.NoDraft(_logger, scope.Kind, scope.Id.ToString("D", CultureInfo.InvariantCulture)));

        var published = await Write(scope, HomePageStates.Published, held.Body, cancellationToken).ConfigureAwait(false);
        if (published.IsSuccess)
            HomePagesLog.Published(_logger, scope.Kind, scope.Id.ToString("D", CultureInfo.InvariantCulture));

        return published;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<AppliedHomePage>> Applying(
        Guid userId, Guid tenantId, Guid? organizationId, CancellationToken cancellationToken = default)
    {
        foreach (var kind in Order)
        {
            var scope = ScopeOf(kind, userId, tenantId, organizationId);
            if (scope is null)
                continue;

            var published = await Read(scope, HomePageStates.Published, cancellationToken).ConfigureAwait(false);
            if (published.IsFailure)
                return published.ToNewResult<AppliedHomePage>();

            if (published.Value is { } page)
            {
                HomePagesLog.Applied(_logger, userId.ToString("D", CultureInfo.InvariantCulture), kind);
                return GenericResult<AppliedHomePage>.Success(new AppliedHomePage(kind, scope.Id, page.Body));
            }
        }

        HomePagesLog.Applied(_logger, userId.ToString("D", CultureInfo.InvariantCulture), HomePageKinds.None);
        return GenericResult<AppliedHomePage>.Success(new AppliedHomePage(HomePageKinds.None, null, null));
    }

    // The scope of a kind for this person; none for an organization the person acts in none of.
    private static HomePageScope? ScopeOf(string kind, Guid userId, Guid tenantId, Guid? organizationId) => kind switch
    {
        HomePageKinds.Person => new HomePageScope(HomePageKinds.Person, userId, tenantId),
        HomePageKinds.Organization => organizationId is { } organization ? new HomePageScope(HomePageKinds.Organization, organization, tenantId) : null,
        HomePageKinds.Tenant => new HomePageScope(HomePageKinds.Tenant, tenantId, tenantId),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Order holds only the kinds of scope that hold a home page."),
    };

    private static string NameOf(HomePageScope scope, string state)
        => string.Create(CultureInfo.InvariantCulture, $"{scope.Kind}:{scope.Id:D}:{state}");

    private Task<IGenericResult<IHomePageImplementationConfiguration>> Read(HomePageScope scope, string state, CancellationToken cancellationToken)
        => _pages.Get(NameOf(scope, state), cancellationToken);

    // Writes the body as the page of a scope in a state: a new row for the first save, a new version of the row after that.
    private async Task<IGenericResult> Write(HomePageScope scope, string state, string body, CancellationToken cancellationToken)
    {
        var name = NameOf(scope, state);
        var existing = await _pages.Get(name, cancellationToken).ConfigureAwait(false);
        if (existing.IsFailure)
            return existing;

        var row = existing.Value;
        if (row is null)
        {
            row = new HomePageImplementationConfiguration
            {
                Name = name,
                TenantId = scope.TenantId,
                ScopeKind = scope.Kind,
                ScopeId = scope.Id,
                State = state,
            };
        }

        row.Body = body;
        return await _pages.Save(row, "HomePage", "HomePage", name, cancellationToken).ConfigureAwait(false);
    }
}
