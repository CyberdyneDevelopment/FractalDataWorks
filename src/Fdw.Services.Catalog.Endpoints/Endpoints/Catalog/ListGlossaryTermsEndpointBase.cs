using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Quality;
using Fdw.Services.Quality.Configuration;
using Fdw.Services.Workspaces;
using Fdw.Services.Workspaces.Logging;
using Fdw.Web.RestEndpoints.Crud;
using Fdw.Web.RestEndpoints.Models;
using Fdw.Web.RestEndpoints.Pagination;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>
/// Endpoint that lists glossary terms one page at a time, in name order (FDW-2336).
/// </summary>
/// <remarks>
/// A page is one read of the glossary terms domain table: what the list filters on and shows lives on the domain row, so no term's
/// implementation, related terms or linked data sets are read to list it, and the cost of a page does not grow with the glossary.
/// <para>
/// <c>?q=</c> finds text in the name, the definition and what an acronym stands for, in the way <c>?match=</c> names;
/// <c>?acronym=</c> and <c>?approved=</c> take true or false; <c>?category=</c> names a category of the Glossary tree and keeps the terms
/// under it and under every category below it; <c>?total=true</c> adds how many terms the filter keeps in all.
/// </para>
/// <para>
/// Each term carries <c>versionCount</c>: how many versions of it the caller may read, the term itself counting as one. The versions of every term on
/// the page are read in one query, however many terms the page holds (FDW-2548).
/// </para>
/// </remarks>
public abstract class ListGlossaryTermsEndpointBase : CrudPageEndpointBase<GlossaryTermDomainConfiguration, GlossaryTermListItem>
{
    private readonly IGlossaryTermConfigurationProvider _provider;
    private readonly ICategoryConfigurationProvider _categories;
    private readonly IGlossaryTermVersionConfigurationProvider _versions;
    private readonly IPlaceAncestry _ancestry;
    private readonly IScopeAuthority _authority;
    private readonly IAuthenticationContextAccessor _authContext;
    private CategoryTree? _tree;
    private IReadOnlyDictionary<Guid, int>? _versionCounts;

    /// <summary>Initializes a new instance of the <see cref="ListGlossaryTermsEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="provider">Reads the page of terms.</param>
    /// <param name="categories">Reads the Glossary category tree the terms are organised in.</param>
    /// <param name="versions">Reads the versions of terms held at organizations and workspaces.</param>
    /// <param name="ancestry">Says which places are at or above a place.</param>
    /// <param name="authority">Says whether the caller holds a right at a place.</param>
    /// <param name="authContext">Says who is signed in and the place they act in.</param>
    protected ListGlossaryTermsEndpointBase(
        ILogger logger,
        IGlossaryTermConfigurationProvider provider,
        ICategoryConfigurationProvider categories,
        IGlossaryTermVersionConfigurationProvider versions,
        IPlaceAncestry ancestry,
        IScopeAuthority authority,
        IAuthenticationContextAccessor authContext)
        : base(logger)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _categories = categories ?? throw new ArgumentNullException(nameof(categories));
        _versions = versions ?? throw new ArgumentNullException(nameof(versions));
        _ancestry = ancestry ?? throw new ArgumentNullException(nameof(ancestry));
        _authority = authority ?? throw new ArgumentNullException(nameof(authority));
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
    }

    /// <inheritdoc />
    protected override string ResourceName => "glossary";

    /// <inheritdoc />
    protected override string Route => "/catalog/glossary";

    /// <inheritdoc />
    protected override string EndpointTag => "Catalog";

    /// <inheritdoc />
    protected override string EndpointSummary => "List glossary terms";

    /// <inheritdoc />
    protected override string EndpointDescription =>
        "Streams one page of glossary terms in name order, sized by ?skip= and ?take=. ?q= finds text in the name, the definition and what " +
        "an acronym stands for; ?acronym= and ?approved= take true or false; ?category= keeps the terms under a Glossary category and the " +
        "categories below it; ?total=true adds how many terms match in all. hasMore says whether another page follows.";

    /// <inheritdoc />
    protected override string ReadPolicy => "datastores:read";

    /// <inheritdoc />
    protected override bool Searchable => true;

    /// <summary>Gets the query string of the request this endpoint serves.</summary>
    /// <remarks>Overridden by a test, which states a request without a running host.</remarks>
    protected virtual IQueryCollection RequestQuery => HttpContext.Request.Query;

    /// <inheritdoc />
    protected override async Task<IGenericResult<ConfigurationPage<GlossaryTermDomainConfiguration>>> LoadPage(
        PaginatedListRequest paging, CancellationToken ct)
    {
        var query = RequestQuery;
        var categoryName = query["category"].FirstOrDefault();

        // ?category= names a category of the Glossary tree: the terms under it and under every category below it.
        var category = await GlossaryCategoryLinks.Requested(_categories, categoryName, Logger, ct).ConfigureAwait(false);
        if (category.IsFailure)
        {
            return category.ToNewResult<ConfigurationPage<GlossaryTermDomainConfiguration>>();
        }

        _tree = category.Value.Tree;
        var under = category.Value.Id is { } categoryId ? _tree.SelfAndDescendants(categoryId) : null;

        var filter = GlossaryTermFilter.Build(Search, query["acronym"].FirstOrDefault(), query["approved"].FirstOrDefault(), under, categoryName, Logger);
        if (filter.IsFailure)
        {
            return filter.ToNewResult<ConfigurationPage<GlossaryTermDomainConfiguration>>();
        }

        var page = await _provider.Get(
            new DomainQuery(filter.Value, ByName(), paging.Skip, paging.Take, Search.IncludeTotal), ct).ConfigureAwait(false);
        if (page.IsFailure || page.Value is not { } read)
        {
            return page;
        }

        // The page is at most one page of terms, so it is read through here to count the versions of all of them in one query.
        var terms = new List<GlossaryTermDomainConfiguration>();
        await foreach (var term in read.Items.WithCancellation(ct).ConfigureAwait(false))
        {
            terms.Add(term);
        }

        if (read.Outcome.IsFailure)
        {
            return read.Outcome.ToNewResult<ConfigurationPage<GlossaryTermDomainConfiguration>>();
        }

        if (CallerPlaces.Of(_authContext, null) is not { } origin)
        {
            return GenericResult<ConfigurationPage<GlossaryTermDomainConfiguration>>.Failure(PlaceAncestryLog.CallerHasNoPlace(Logger));
        }

        var counts = await PlaceVersions.Counts(
            GlossaryTermVersionSource.Of(_versions),
            _ancestry,
            _authority,
            terms.Select(term => term.Id).ToList(),
            origin,
            ReadPolicy,
            Logger,
            ct).ConfigureAwait(false);
        if (counts.IsFailure || counts.Value is not { } counted)
        {
            return counts.ToNewResult<ConfigurationPage<GlossaryTermDomainConfiguration>>();
        }

        _versionCounts = counted;
        return GenericResult<ConfigurationPage<GlossaryTermDomainConfiguration>>.Success(
            new ConfigurationPage<GlossaryTermDomainConfiguration>(read.Skip, read.Take, read.HasMore, _ => terms.ToAsyncEnumerable(), read.Total));
    }

    /// <inheritdoc />
    protected override Task<IGenericResult<GlossaryTermListItem>> ToSummary(GlossaryTermDomainConfiguration item, CancellationToken ct)
    {
        var described = GlossaryCategoryLinks.Describe(
            item, _tree ?? throw new InvalidOperationException("The Glossary category tree is read before the page is loaded"), Logger);
        if (described.IsFailure || described.Value is not { } term)
        {
            return Task.FromResult(described.ToNewResult<GlossaryTermListItem>());
        }

        var counts = _versionCounts ?? throw new InvalidOperationException("The versions of the page's terms are counted before the page is loaded");
        return Task.FromResult(counts.TryGetValue(item.Id, out var count)
            ? GenericResult<GlossaryTermListItem>.Success(GlossaryTermListItem.From(term, count))
            : GenericResult<GlossaryTermListItem>.Failure(GlossaryTermVersionLog.TermNotFound(Logger, item.Id.ToString("D"))));
    }

    private static OrderingExpression ByName() => new()
    {
        OrderedFields = [new OrderedField { PropertyName = nameof(GlossaryTermDomainConfiguration.Name), Direction = SortDirections.ByName("Ascending") }],
    };
}
