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

/// <summary>Endpoint that resolves (marks as reviewed/approved) a DataSet annotation.</summary>
public abstract class ResolveDataSetAnnotationEndpointBase : Endpoint<DataSetAnnotationIdRequest, DataSetAnnotationPayload>
{
    private readonly QualityConfigurationProvider _provider;

    /// <summary>Initializes a new instance of the <see cref="ResolveDataSetAnnotationEndpointBase"/> class.</summary>
    /// <param name="provider">The configuration provider for quality and catalog data.</param>
    protected ResolveDataSetAnnotationEndpointBase(QualityConfigurationProvider provider)
    {
        _provider = provider;
    }

    /// <summary>Gets the authorization policy required for write operations.</summary>
    protected virtual string WritePolicy => "datastores:write";

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Post("/catalog/annotations/{AnnotationId}/resolve");
#if DEVELOP
        AllowAnonymous();
#else
        Policies(WritePolicy);
#endif
        Summary(s => s.Summary = "Resolve DataSet annotation");
    }

    /// <summary>
    /// Marks the annotation as resolved by re-saving it through the provider.
    /// The DB DEFAULT (sysdatetimeoffset()) on ModifiedAt is refreshed on UPDATE by the provider.
    /// </summary>
    public override async Task HandleAsync(DataSetAnnotationIdRequest req, CancellationToken ct)
    {
        var getResult = await _provider.GetAnnotation(req.AnnotationId, ct).ConfigureAwait(false);

        if (!getResult.IsSuccess)
        {
            HttpContext.Response.StatusCode = 500;
            await HttpContext.Response.WriteAsJsonAsync(
                new { Error = "Failed to query annotation", Details = getResult.CurrentMessage }, ct).ConfigureAwait(false);
            return;
        }

        if (getResult.Value is null)
        {
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var saveResult = await _provider.SaveAnnotation(getResult.Value, ct).ConfigureAwait(false);

        if (!saveResult.IsSuccess)
        {
            HttpContext.Response.StatusCode = 500;
            await HttpContext.Response.WriteAsJsonAsync(
                new { Error = "Failed to resolve annotation", Details = saveResult.CurrentMessage }, ct).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(MapToDto(getResult.Value), ct).ConfigureAwait(false);
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
