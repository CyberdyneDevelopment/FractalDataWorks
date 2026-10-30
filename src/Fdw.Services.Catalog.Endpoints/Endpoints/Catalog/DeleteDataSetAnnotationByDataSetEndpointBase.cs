using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Quality;
using Fdw.Web.RestEndpoints.ErrorMapping;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>Endpoint that deletes a DataSet's annotation, found by the name of the DataSet.</summary>
/// <remarks>
/// Why by the DataSet's name: the annotation reads (get, list) and the update are all addressed by the DataSet's name and answer no
/// annotation id, so a client holds nothing to call the delete-by-id route with.
/// </remarks>
public abstract class DeleteDataSetAnnotationByDataSetEndpointBase : Endpoint<DataSetAnnotationRequest>
{
    private readonly IDataSetAnnotationConfigurationProvider _provider;

    /// <summary>Initializes a new instance of the <see cref="DeleteDataSetAnnotationByDataSetEndpointBase"/> class.</summary>
    /// <param name="provider">The configuration provider for quality and catalog data.</param>
    protected DeleteDataSetAnnotationByDataSetEndpointBase(IDataSetAnnotationConfigurationProvider provider)
    {
        _provider = provider;
    }

    /// <summary>Gets the authorization policy required for write operations.</summary>
    protected virtual string WritePolicy => "datastores:write";

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Delete("/catalog/datasets/{DataSetName}/annotation");
#if DEVELOP
        AllowAnonymous();
#else
        Policies(WritePolicy);
#endif
        Summary(s =>
        {
            s.Summary = "Delete the annotation of a DataSet";
            s.Description = "Deletes the annotation of the named DataSet. 204 when deleted; 404 when the DataSet has no annotation.";
        });
    }

    /// <summary>Deletes the DataSet's annotation and answers 204 No Content, or the failure the provider states (a not-found failure, so 404, when the DataSet has none).</summary>
    public override async Task HandleAsync(DataSetAnnotationRequest req, CancellationToken ct)
    {
        var result = await _provider.Delete(req.DataSetName, ct).ConfigureAwait(false);

        if (result.IsFailure)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(result, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.NoContentAsync(ct).ConfigureAwait(false);
    }
}
