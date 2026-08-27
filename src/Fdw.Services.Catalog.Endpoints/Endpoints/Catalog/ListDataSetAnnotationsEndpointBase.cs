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

/// <summary>Endpoint that lists annotations for a DataSet.</summary>
public abstract class ListDataSetAnnotationsEndpointBase : Endpoint<DataSetAnnotationRequest, List<DataSetAnnotationPayload>>
{
    private readonly QualityConfigurationProvider _provider;

    /// <summary>Initializes a new instance of the <see cref="ListDataSetAnnotationsEndpointBase"/> class.</summary>
    /// <param name="provider">The configuration provider for quality and catalog data.</param>
    protected ListDataSetAnnotationsEndpointBase(QualityConfigurationProvider provider)
    {
        _provider = provider;
    }

    /// <summary>Gets the authorization policy required for read operations.</summary>
    protected virtual string ReadPolicy => "datastores:read";

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Get("/catalog/datasets/{DataSetName}/annotations");
#if DEVELOP
        AllowAnonymous();
#else
        Policies(ReadPolicy);
#endif
        Summary(s => s.Summary = "List DataSet annotations");
    }

    /// <summary>Retrieves all annotations for the specified DataSet.</summary>
    public override async Task HandleAsync(DataSetAnnotationRequest req, CancellationToken ct)
    {
        // Why: a request for annotations on a DataSet that doesn't exist should 404, not return
        // an empty list — the latter would mask typos in the DataSet name. Resolve the provider
        // via DI (FastEndpoints) so we don't break the existing single-arg constructor contract.
        var dataSetProvider = TryResolve<Fdw.Services.Data.DataSetConfigurationProvider>();
        if (dataSetProvider is not null && !string.IsNullOrEmpty(req.DataSetName))
        {
            var existsResult = await dataSetProvider.Get(req.DataSetName, ct).ConfigureAwait(false);
            if (!existsResult.IsSuccess || existsResult.Value is null)
            {
                HttpContext.Response.StatusCode = 404;
                HttpContext.Response.ContentType = "application/json";
                await HttpContext.Response.WriteAsJsonAsync(new
                {
                    errorCode = "NotFound",
                    messages = new[] { $"DataSet '{req.DataSetName}' was not found." }
                }, ct).ConfigureAwait(false);
                return;
            }
        }

        // Why: The provider's Get(all) returns every annotation; we filter client-side by DataSetName
        // because ImplementationConfigurationProviderBase has no per-field filter overload and DataSetName is
        // a domain-specific filter. This is acceptable since annotation counts are small.
        var result = await _provider.GetAllAnnotations(ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            HttpContext.Response.StatusCode = 500;
            await HttpContext.Response.WriteAsJsonAsync(
                new { Error = "Failed to list annotations", Details = result.CurrentMessage }, ct).ConfigureAwait(false);
            return;
        }

        var annotations = result.Value?
            .Where(a => string.Equals(a.DataSetName, req.DataSetName, System.StringComparison.OrdinalIgnoreCase))
            .Select(MapToDto)
            .ToList() ?? [];

        await Send.OkAsync(annotations, ct).ConfigureAwait(false);
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
