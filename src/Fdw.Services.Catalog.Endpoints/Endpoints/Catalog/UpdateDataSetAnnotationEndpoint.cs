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

/// <summary>Endpoint that updates an existing DataSet annotation.</summary>
public abstract class UpdateDataSetAnnotationEndpoint : Endpoint<DataSetAnnotationPayload, DataSetAnnotationPayload>
{
    private readonly QualityConfigurationProvider _provider;

    /// <summary>Initializes a new instance of the <see cref="UpdateDataSetAnnotationEndpoint"/> class.</summary>
    /// <param name="provider">The configuration provider for quality and catalog data.</param>
    protected UpdateDataSetAnnotationEndpoint(QualityConfigurationProvider provider)
    {
        _provider = provider;
    }

    /// <summary>Gets the authorization policy required for write operations.</summary>
    protected virtual string WritePolicy => "datastores:write";

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Patch("/catalog/datasets/{DataSetName}/annotation");
#if DEVELOP
        AllowAnonymous();
#else
        Policies(WritePolicy);
#endif
        Summary(s => s.Summary = "Update DataSet annotation");
    }

    /// <summary>Updates the DataSet annotation matching the request and returns the updated DTO.</summary>
    public override async Task HandleAsync(DataSetAnnotationPayload req, CancellationToken ct)
    {
        // Why: Load existing to preserve the Id so Save() issues an UPDATE rather than INSERT.
        var getResult = await _provider.GetAnnotation(req.DataSetName, ct).ConfigureAwait(false);

        if (!getResult.IsSuccess)
        {
            HttpContext.Response.StatusCode = 500;
            await HttpContext.Response.WriteAsJsonAsync(
                new { Error = "Failed to load existing annotation", Details = getResult.CurrentMessage }, ct).ConfigureAwait(false);
            return;
        }

        var config = QualityConfigurationProvider.MapAnnotationFromDto(
            req.DataSetName, req.Owner, req.Steward, req.Classification, req.Tags);

        // Why: Carry forward the existing Id so DefaultConfigurationProvider.Save routes to UPDATE.
        if (getResult.Value is not null)
            config.Id = getResult.Value.Id;

        var result = await _provider.SaveAnnotation(config, ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            HttpContext.Response.StatusCode = 500;
            await HttpContext.Response.WriteAsJsonAsync(
                new { Error = "Failed to update annotation", Details = result.CurrentMessage }, ct).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(req, ct).ConfigureAwait(false);
    }
}
