using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.Transformations.Configuration;
using Fdw.Services.Calculations;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Quality;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>
/// Endpoint that answers where a data set, field, container, calculation, pipeline or term is used, and what a pipeline, calculation,
/// term or data set uses (FDW-1683).
/// </summary>
public abstract class GetCatalogUsesEndpointBase : Endpoint<CatalogUsesRequest, CatalogUsesResponse>
{
    private readonly CatalogUses _uses;

    /// <summary>Initializes a new instance of the <see cref="GetCatalogUsesEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dataSets">Supplies the data sets and where they come from.</param>
    /// <param name="pipelines">Supplies the pipelines.</param>
    /// <param name="calculations">Supplies the calculations.</param>
    /// <param name="transformations">Supplies the transformations, so a tool's transformation id has a name.</param>
    /// <param name="terms">Supplies the glossary terms.</param>
    protected GetCatalogUsesEndpointBase(
        ILogger logger,
        IDataSetConfigurationProvider dataSets,
        IPipelineConfigurationProvider pipelines,
        ICalculationConfigurationProvider calculations,
        ITransformationConfigurationProvider transformations,
        IGlossaryTermConfigurationProvider terms)
    {
        _uses = new CatalogUses(dataSets, pipelines, calculations, transformations, terms, logger);
    }

    /// <summary>Gets the authorization policy required for read operations.</summary>
    protected virtual string ReadPolicy => "datastores:read";

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Get("/catalog/uses");
#if DEVELOP
        AllowAnonymous();
#else
        Policies(ReadPolicy);
#endif
        Summary(s =>
        {
            s.Summary = "Where a catalog thing is used";
            s.Description = "kind and name state the thing (a kind of the catalog search); direction is usedBy for what uses it (the pipelines that read or " +
                "write a data set or container, the pipelines that call a pipeline or run a transformation, the terms that explain it, the data sets built from it) or uses for what " +
                "it uses (the data sets and containers a pipeline reads and writes and the transformations it runs, the data sets a calculation reads and writes, the data sets a term explains, " +
                "where a data set comes from). parents narrows a container or field to the names it sits under, outermost first. The answer lists each " +
                "other thing with its kind, name and the relation (reads, writes, calls, uses, describes, derivedFrom, storedIn); a thing nothing uses " +
                "answers an empty list. A thing the caller may not see is absent.";
        });
    }

    /// <summary>Answers one where-used question.</summary>
    public override async Task HandleAsync(CatalogUsesRequest req, CancellationToken ct)
    {
        var refusal = Refusal(req);
        if (refusal is not null)
        {
            AddError(refusal);
            await Send.ErrorsAsync(400, ct).ConfigureAwait(false);
            return;
        }

        var kind = CatalogKinds.All[CatalogKinds.IndexOf(req.Kind!)];
        var direction = CatalogUsesDirections.Of(req.Direction)!;
        var found = await _uses.Run(kind, req.Name!.Trim(), req.Parents.Where(parent => !string.IsNullOrWhiteSpace(parent)).ToList(), direction, ct).ConfigureAwait(false);
        if (found.IsFailure)
        {
            await Send.ResultAsync(TypedResults.Json(new { Error = "Failed to read where the thing is used", Details = found.CurrentMessage }, statusCode: 500)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(found.Value!, ct).ConfigureAwait(false);
    }

    // What the request got wrong, in words the caller can act on; null when it is complete.
    private static string? Refusal(CatalogUsesRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Kind) || CatalogKinds.IndexOf(req.Kind) < 0)
            return $"kind must be one of {string.Join(", ", CatalogKinds.All)}";
        if (string.IsNullOrWhiteSpace(req.Name))
            return "name is required";
        return CatalogUsesDirections.Of(req.Direction) is null
            ? $"direction must be one of {string.Join(", ", CatalogUsesDirections.All)}"
            : null;
    }
}
