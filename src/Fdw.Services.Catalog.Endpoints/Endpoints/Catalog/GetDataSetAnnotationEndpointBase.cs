using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Quality;
using Fdw.Services.Quality.Configuration;
using Microsoft.AspNetCore.Http;
using Fdw.Services.Catalog.Clients.Models;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>Endpoint that retrieves a DataSet annotation by DataSet name.</summary>
public abstract class GetDataSetAnnotationEndpointBase : Endpoint<DataSetAnnotationRequest, DataSetAnnotationPayload>
{
    private readonly QualityConfigurationProvider _provider;

    /// <summary>Initializes a new instance of the <see cref="GetDataSetAnnotationEndpointBase"/> class.</summary>
    /// <param name="provider">The configuration provider for quality and catalog data.</param>
    protected GetDataSetAnnotationEndpointBase(QualityConfigurationProvider provider)
    {
        _provider = provider;
    }

    /// <summary>Gets the authorization policy required for read operations.</summary>
    protected virtual string ReadPolicy => "datastores:read";

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Get("/catalog/datasets/{DataSetName}/annotation");
#if DEVELOP
        AllowAnonymous();
#else
        Policies(ReadPolicy);
#endif
        Summary(s => s.Summary = "Get DataSet annotation");
    }

    /// <summary>Retrieves the annotation for the specified DataSet, returning 404 if not found.</summary>
    public override async Task HandleAsync(DataSetAnnotationRequest req, CancellationToken ct)
    {
        var result = await _provider.GetAnnotation(req.DataSetName, ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            HttpContext.Response.StatusCode = 500;
            await HttpContext.Response.WriteAsJsonAsync(
                new { Error = "Failed to get annotation", Details = result.CurrentMessage }, ct).ConfigureAwait(false);
            return;
        }

        if (result.Value is null)
        {
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(MapToDto(result.Value), ct).ConfigureAwait(false);
    }

    /// <summary>Maps a DataSetAnnotationConfiguration to its corresponding DTO.</summary>
    protected virtual DataSetAnnotationPayload MapToDto(DataSetAnnotationConfiguration config)
    {
        return new DataSetAnnotationPayload
        {
            DataSetName = config.DataSetName,
            Owner = config.BusinessOwner,
            Steward = config.TechnicalOwner,
            Classification = config.DataClassification,
            Tags = config.Tags.Select(t => t.Tag).ToList()
        };
    }
}
