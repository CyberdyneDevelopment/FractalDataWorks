using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Etl;
using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Base endpoint for retrieving all registered ETL pipeline engine types from the
/// <c>EtlPipelineTypes</c> ServiceTypeCollection.
/// Route: GET /pipelines/types
/// </summary>
/// <remarks>
/// Why: sources the engine-type list from the live TypeCollection — never a hardcoded
/// "BatchCopy"/"Streaming" literal — so a new <c>[ServiceTypeOption(typeof(EtlPipelineTypes), "...")]</c>
/// registered by any assembly appears here automatically with no code change.
/// </remarks>
public abstract class GetPipelineTypesEndpointBase : EndpointWithoutRequest<IReadOnlyList<PipelineTypeSummary>>
{
    /// <summary>Gets the authorization policy name for read operations.</summary>
    protected virtual string ReadPolicy => "pipelines:read";

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Get("/pipelines/types");
#if DEVELOP
        AllowAnonymous();
#else
        Policies(ReadPolicy);
#endif
        Summary(s =>
        {
            s.Summary = "Get available pipeline engine types";
            s.Description = "Returns all ETL pipeline engine types registered in the EtlPipelineTypes ServiceTypeCollection (source-generated, no reflection).";
        });
    }

    /// <summary>Returns all registered pipeline engine types as summary DTOs.</summary>
    public override Task HandleAsync(CancellationToken ct)
    {
        var all = EtlPipelineTypes.All();
        var result = new List<PipelineTypeSummary>(all.Count);

        foreach (var kvp in all)
        {
            result.Add(new PipelineTypeSummary
            {
                Name = kvp.Value.Name,
                Category = kvp.Value.Category
            });
        }

        return Send.OkAsync((IReadOnlyList<PipelineTypeSummary>)result, ct);
    }
}
