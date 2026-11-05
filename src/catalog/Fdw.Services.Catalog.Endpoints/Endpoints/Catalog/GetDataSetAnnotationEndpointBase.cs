using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Quality;
using Fdw.Services.Quality.Configuration;
using Microsoft.AspNetCore.Http;
using Fdw.Services.Catalog.Clients.Models;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Tags;
using Fdw.Web.RestEndpoints.ErrorMapping;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>Endpoint that retrieves a DataSet annotation by DataSet name.</summary>
public abstract class GetDataSetAnnotationEndpointBase : Endpoint<DataSetAnnotationRequest, DataSetAnnotationPayload>
{
    private readonly IDataSetAnnotationConfigurationProvider _provider;
    private readonly IDataSetConfigurationProvider _dataSets;
    private readonly ITagService _tags;

    /// <summary>Initializes a new instance of the <see cref="GetDataSetAnnotationEndpointBase"/> class.</summary>
    /// <param name="provider">The configuration provider for quality and catalog data.</param>
    /// <param name="dataSets">Finds the data set the annotation names, whose tags the annotation shows.</param>
    /// <param name="tags">Reads the data set's tags.</param>
    protected GetDataSetAnnotationEndpointBase(
        IDataSetAnnotationConfigurationProvider provider, IDataSetConfigurationProvider dataSets, ITagService tags)
    {
        _provider = provider;
        _dataSets = dataSets;
        _tags = tags;
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
        var result = await _provider.Get(req.DataSetName, ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            await Send.ResultAsync(TypedResults.Json(new { Error = "Failed to get annotation", Details = result.CurrentMessage }, statusCode: 500)).ConfigureAwait(false);
            return;
        }

        if (result.Value is null)
        {
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var names = await DataSetAnnotationTags.Read(_tags, _dataSets, req.DataSetName, Logger, ct).ConfigureAwait(false);
        if (names.IsFailure || names.Value is null)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(names, HttpContext)).ConfigureAwait(false);
            return;
        }

        var dto = MapToDto(result.Value);
        dto.Tags = names.Value;
        await Send.OkAsync(dto, ct).ConfigureAwait(false);
    }

    /// <summary>Maps a DataSetAnnotationImplementationConfiguration to its corresponding DTO.</summary>
    protected virtual DataSetAnnotationPayload MapToDto(IDataSetAnnotationImplementationConfiguration config)
    {
        return DataSetAnnotationPayloadMapper.Map(config);
    }
}
