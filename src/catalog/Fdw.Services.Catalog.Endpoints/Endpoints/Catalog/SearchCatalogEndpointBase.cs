using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.Transformations.Configuration;
using Fdw.Services.Calculations;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Quality;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Fdw.Web.RestEndpoints.Pagination;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Fdw.Results;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>
/// Endpoint that searches the catalog by name or description across data sets and their fields, data stores with their paths,
/// containers and fields, calculations, transformations, pipelines and glossary terms (FDW-1681).
/// </summary>
public abstract class SearchCatalogEndpointBase : Endpoint<CatalogSearchRequest, CatalogSearchResponse>
{
    private readonly IDataSetConfigurationProvider _dataSets;
    private readonly IDataStoreProvider _dataStores;
    private readonly IPipelineConfigurationProvider _pipelines;
    private readonly ICalculationConfigurationProvider _calculations;
    private readonly ITransformationConfigurationProvider _transformations;
    private readonly IGlossaryTermConfigurationProvider _terms;
    private readonly IReadOnlyList<IRankedSearch> _fieldSearches;

    /// <summary>Initializes a new instance of the <see cref="SearchCatalogEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dataSets">Supplies the data sets and their fields.</param>
    /// <param name="dataStores">Supplies the data stores with their paths, containers and fields.</param>
    /// <param name="pipelines">Supplies the pipelines.</param>
    /// <param name="calculations">Supplies the calculations.</param>
    /// <param name="transformations">Supplies the transformations.</param>
    /// <param name="terms">Supplies the glossary terms.</param>
    /// <param name="fieldSearches">The searches of the fields of data sets and of containers, which are found at the store.</param>
    protected SearchCatalogEndpointBase(
        ILogger logger,
        IDataSetConfigurationProvider dataSets,
        IDataStoreProvider dataStores,
        IPipelineConfigurationProvider pipelines,
        ICalculationConfigurationProvider calculations,
        ITransformationConfigurationProvider transformations,
        IGlossaryTermConfigurationProvider terms,
        IEnumerable<IRankedSearch> fieldSearches)
    {
        Logger = logger;
        _dataSets = dataSets;
        _dataStores = dataStores;
        _pipelines = pipelines;
        _calculations = calculations;
        _transformations = transformations;
        _terms = terms;
        _fieldSearches = [.. fieldSearches];
    }

    /// <summary>Gets the authorization policy required for read operations.</summary>
    protected virtual string ReadPolicy => "datastores:read";

    /// <summary>Gets the logger instance for this endpoint.</summary>
    protected new ILogger Logger { get; }

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Get("/catalog/search");
#if DEVELOP
        AllowAnonymous();
#else
        Policies(ReadPolicy);
#endif
        Summary(s =>
        {
            s.Summary = "Search the catalog";
            s.Description = "Finds data sets, data set fields, data stores, paths, containers, container fields, calculations, transformations, " +
                "pipelines and glossary terms whose name or description contains q (ignoring case), the closest names first. entityTypes " +
                "narrows the kinds (repeat it per kind); left out, every kind is searched, and q left out lists everything. skip and take page " +
                "the matches (take is the DefaultPaginationSize setting when left out and at most MaxPaginationSize" +
                ", and skip plus take at most the KeyChunkSize the configuration gateway is registered with, 1000 unless the host " +
                "changes it, because each source reads skip plus take of its closest matches and places them with one In filter). Each entry carries its kind, name, description and where it sits. A thing the caller may not see is absent. total counts " +
                "every match before paging.";
        });
    }

    /// <summary>Executes a catalog search using the provided criteria and returns the page of matching entries.</summary>
    public override async Task HandleAsync(CatalogSearchRequest req, CancellationToken ct)
    {
        var (kinds, unknown) = CatalogKindSet.Of(req.EntityTypes);
        if (kinds is null)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(CatalogAnswerLog.SearchCatalogUnknownKind(Logger, unknown ?? string.Empty, string.Join(", ", CatalogKinds.All))), HttpContext)).ConfigureAwait(false);
            return;
        }

        // The one reader every list endpoint pages by: ?skip and ?take, sized by the paging settings, a failure when one is missing.
        var paging = PaginationQuery.Parse(HttpContext, Logger);
        if (paging.IsFailure)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(paging, HttpContext)).ConfigureAwait(false);
            return;
        }

    if (paging.Value is not { } page)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(CatalogAnswerLog.SearchCatalogPagingReturnedNothing(Logger)), HttpContext)).ConfigureAwait(false);
            return;
        }

        var skip = page.Skip;
        var take = page.Take;

        // Why: the page is chosen from the closest matches each source returns, and a source reads no more than the keys one statement
        // carries, which is the host's configuration (FDW-7106).
        var gateways = Resolve<IConfigurationGatewayProvider>();
        if ((long)skip + take > gateways.KeyChunkSize)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(CatalogAnswerLog.SearchCatalogWindowTooLarge(Logger, gateways.KeyChunkSize)), HttpContext)).ConfigureAwait(false);
            return;
        }

        var found = await new CatalogSearch(
            _dataSets, _dataStores, _pipelines, _calculations, _transformations,
            [.. _fieldSearches, new GlossaryTermSearch(_terms, gateways, Logger)], Logger).Run(kinds, (req.Query ?? string.Empty).Trim(), skip, take, ct).ConfigureAwait(false);
        if (found.IsFailure)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(found, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(found.Value!, ct).ConfigureAwait(false);
    }
}
