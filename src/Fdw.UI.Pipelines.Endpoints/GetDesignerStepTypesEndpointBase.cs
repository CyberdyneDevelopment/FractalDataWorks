using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Pipelines.Abstractions.TypeCollections.PipelineStepTypeOptions;
using Fdw.UI.Pipelines.Clients.Models;

namespace Fdw.UI.Pipelines.Endpoints;

/// <summary>
/// Base endpoint for retrieving all available pipeline step types from the <c>PipelineStepTypes</c> TypeCollection.
/// Route: GET /pipelines/designer/step-types
/// </summary>
public abstract class GetDesignerStepTypesEndpointBase : EndpointWithoutRequest<IReadOnlyList<PipelineStepTypeSummary>>
{
    /// <summary>Gets the authorization policy name for read operations.</summary>
    protected virtual string ReadPolicy => "pipelines:read";

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Get("/pipelines/designer/step-types");
#if DEVELOP
        AllowAnonymous();
#else
        Policies(ReadPolicy);
#endif
        Summary(s =>
        {
            s.Summary = "Get available pipeline step types";
            s.Description = "Returns all step types registered in the PipelineStepTypes TypeCollection (source-generated, no reflection).";
        });
    }

    /// <summary>Returns all registered pipeline step types as summary DTOs.</summary>
    public override Task HandleAsync(CancellationToken ct)
    {
        var all = PipelineStepTypes.All();
        var result = new List<PipelineStepTypeSummary>(all.Count);

        foreach (var t in all)
        {
            result.Add(new PipelineStepTypeSummary
            {
                TypeName = t.Name,
                DisplayName = t.Name,
                RequiresSourceConfig = t.RequiresSourceConfig,
                RequiresTransformConfig = t.RequiresTransformConfig,
                RequiresTargetConfig = t.RequiresTargetConfig,
                RequiresValidationConfig = t.RequiresValidationConfig,
                RequiresNotificationConfig = t.RequiresNotificationConfig,
                RequiresBranchCondition = t.RequiresBranchCondition
            });
        }

        return Send.OkAsync((IReadOnlyList<PipelineStepTypeSummary>)result, ct);
    }
}
