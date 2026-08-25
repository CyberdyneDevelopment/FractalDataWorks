using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Quality;
using Microsoft.AspNetCore.Http;
using Fdw.Services.Catalog.Clients.Models;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>Endpoint that creates a new DataSet annotation.</summary>
public abstract class CreateDataSetAnnotationEndpointBase : Endpoint<DataSetAnnotationPayload, DataSetAnnotationPayload>
{
    private readonly QualityConfigurationProvider _provider;

    /// <summary>Initializes a new instance of the <see cref="CreateDataSetAnnotationEndpointBase"/> class.</summary>
    /// <param name="provider">The configuration provider for quality and catalog data.</param>
    protected CreateDataSetAnnotationEndpointBase(QualityConfigurationProvider provider)
    {
        _provider = provider;
    }

    /// <summary>Gets the authorization policy required for write operations.</summary>
    protected virtual string WritePolicy => "datastores:write";

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Post("/catalog/datasets/{DataSetName}/annotations");
#if DEVELOP
        AllowAnonymous();
#else
        Policies(WritePolicy);
#endif
        Summary(s => s.Summary = "Create DataSet annotation");
    }

    /// <summary>Creates a new DataSet annotation and returns the created resource.</summary>
    public override async Task HandleAsync(DataSetAnnotationPayload req, CancellationToken ct)
    {
        var config = QualityConfigurationProvider.MapAnnotationFromDto(
            req.DataSetName, req.Owner, req.Steward, req.Classification, req.Tags);

        var result = await _provider.SaveAnnotation(config, ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            HttpContext.Response.StatusCode = 500;
            await HttpContext.Response.WriteAsJsonAsync(
                new { Error = "Failed to create annotation", Details = result.CurrentMessage }, ct).ConfigureAwait(false);
            return;
        }

        await Send.CreatedAtAsync<GetDataSetAnnotationEndpointBase>(
            new { DataSetName = req.DataSetName }, req, cancellation: ct).ConfigureAwait(false);
    }
}
