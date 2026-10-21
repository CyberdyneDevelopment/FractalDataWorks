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

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>
/// Endpoint that searches the catalog by name or description across data sets and their fields, data stores with their paths,
/// containers and fields, calculations, transformations, pipelines and glossary terms (FDW-1681).
/// </summary>
public abstract class SearchCatalogEndpointBase : Endpoint<CatalogSearchRequest, CatalogSearchResponse>
{
    private readonly CatalogSearch _search;

    /// <summary>Initializes a new instance of the <see cref="SearchCatalogEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dataSets">Supplies the data sets and their fields.</param>
    /// <param name="dataStores">Supplies the data stores with their paths, containers and fields.</param>
    /// <param name="pipelines">Supplies the pipelines.</param>
    /// <param name="calculations">Supplies the calculations.</param>
    /// <param name="transformations">Supplies the transformations.</param>
    /// <param name="terms">Supplies the glossary terms.</param>
    protected SearchCatalogEndpointBase(
        ILogger logger,
        IDataSetConfigurationProvider dataSets,
        IDataStoreProvider dataStores,
        IPipelineConfigurationProvider pipelines,
        ICalculationConfigurationProvider calculations,
        ITransformationConfigurationProvider transformations,
        IGlossaryTermConfigurationProvider terms)
    {
        Logger = logger;
        _search = new CatalogSearch(dataSets, dataStores, pipelines, calculations, transformations, terms, logger);
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
                "the matches (take is " + CatalogSearchLimits.DefaultTake + " when left out and at most " + CatalogSearchLimits.MaxTake +
                "). Each entry carries its kind, name, description and where it sits. A thing the caller may not see is absent. total counts " +
                "every match before paging.";
        });
    }

    /// <summary>Executes a catalog search using the provided criteria and returns the page of matching entries.</summary>
    public override async Task HandleAsync(CatalogSearchRequest req, CancellationToken ct)
    {
        var (kinds, unknown) = CatalogKindSet.Of(req.EntityTypes);
        if (kinds is null)
        {
            AddError(r => r.EntityTypes, $"'{unknown}' is not a catalog kind; the kinds are {string.Join(", ", CatalogKinds.All)}");
            await Send.ErrorsAsync(400, ct).ConfigureAwait(false);
            return;
        }

        var skip = req.Skip ?? 0;
        var take = req.Take ?? CatalogSearchLimits.DefaultTake;
        if (skip < 0 || take < 1 || take > CatalogSearchLimits.MaxTake)
        {
            AddError(r => r.Take, $"skip must be 0 or more and take between 1 and {CatalogSearchLimits.MaxTake}");
            await Send.ErrorsAsync(400, ct).ConfigureAwait(false);
            return;
        }

        var found = await _search.Run(kinds, (req.Query ?? string.Empty).Trim(), skip, take, ct).ConfigureAwait(false);
        if (found.IsFailure)
        {
            await Send.ResultAsync(TypedResults.Json(new { Error = "Failed to search the catalog", Details = found.CurrentMessage }, statusCode: 500)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(found.Value!, ct).ConfigureAwait(false);
    }
}
