using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Quality;
using Microsoft.AspNetCore.Http;
using Fdw.Services.Catalog.Clients.Models;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Tags;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>Endpoint that creates a new DataSet annotation.</summary>
public abstract class CreateDataSetAnnotationEndpointBase : Endpoint<DataSetAnnotationPayload, DataSetAnnotationPayload>
{
    private readonly IDataSetAnnotationConfigurationProvider _provider;
    private readonly IDataSetConfigurationProvider _dataSets;
    private readonly ITagService _tags;
    private readonly IAuthenticationContextAccessor _authentication;

    /// <summary>Initializes a new instance of the <see cref="CreateDataSetAnnotationEndpointBase"/> class.</summary>
    /// <param name="provider">The configuration provider for quality and catalog data.</param>
    /// <param name="dataSets">Finds the data set the annotation names, which carries the tags.</param>
    /// <param name="tags">Puts the request's tags on the data set.</param>
    /// <param name="authentication">The caller's tenant, in which a tag the tenant does not have yet is made.</param>
    protected CreateDataSetAnnotationEndpointBase(
        IDataSetAnnotationConfigurationProvider provider,
        IDataSetConfigurationProvider dataSets,
        ITagService tags,
        IAuthenticationContextAccessor authentication)
    {
        _provider = provider;
        _dataSets = dataSets;
        _tags = tags;
        _authentication = authentication;
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
        var config = DataSetAnnotationRequestMapper.FromPayload(
            req.DataSetName, req.Owner, req.Steward, req.Classification);

        var result = await _provider.Create(config, "DataSetAnnotation", "DataSetAnnotation", config.Name, ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(result, HttpContext)).ConfigureAwait(false);
            return;
        }

        var tagged = await DataSetAnnotationTags.Replace(
            _tags, _dataSets, _authentication, req.DataSetName, req.Tags, Logger, ct).ConfigureAwait(false);
        if (tagged.IsFailure)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(tagged, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.CreatedAtAsync<GetDataSetAnnotationEndpointBase>(
            new { DataSetName = req.DataSetName }, req, cancellation: ct).ConfigureAwait(false);
    }
}
