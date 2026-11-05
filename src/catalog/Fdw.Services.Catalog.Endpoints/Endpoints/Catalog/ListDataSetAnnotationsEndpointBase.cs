using Fdw.Data.DataSets.Abstractions;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Data;
using Fdw.Services.Quality;
using Fdw.Services.Quality.Configuration;
using Microsoft.AspNetCore.Http;
using Fdw.Services.Catalog.Clients.Models;
using Fdw.Services.Tags;
using Fdw.Web.RestEndpoints.ErrorMapping;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>Endpoint that lists annotations for a DataSet.</summary>
public abstract class ListDataSetAnnotationsEndpointBase : Endpoint<DataSetAnnotationRequest, List<DataSetAnnotationPayload>>
{
    private readonly IDataSetAnnotationConfigurationProvider _provider;
    private readonly IDataSetConfigurationProvider _dataSets;
    private readonly ITagService _tags;

    /// <summary>Initializes a new instance of the <see cref="ListDataSetAnnotationsEndpointBase"/> class.</summary>
    /// <param name="provider">The configuration provider for quality and catalog data.</param>
    /// <param name="dataSets">Used to confirm the named DataSet exists and to find its tags.</param>
    /// <param name="tags">Reads the data set's tags.</param>
    protected ListDataSetAnnotationsEndpointBase(
        IDataSetAnnotationConfigurationProvider provider,
        IDataSetConfigurationProvider dataSets,
        ITagService tags)
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
        // Whether the data set exists is what the read says, not whether a provider was injected.
        var exists = await _dataSets.Get(req.DataSetName, ct).ConfigureAwait(false);
        if (!exists.IsSuccess)
        {
            await Send.ResultAsync(TypedResults.Json(new { Error = "Failed to read the data set", Details = exists.CurrentMessage }, statusCode: 500)).ConfigureAwait(false);
            return;
        }

        if (exists.Value is null)
        {
            await Send.ResultAsync(TypedResults.Json(new
            {
                errorCode = "NotFound",
                messages = new[] { $"DataSet '{req.DataSetName}' was not found." }
            }, statusCode: 404)).ConfigureAwait(false);
            return;
        }

        var result = await _provider.Get(ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            await Send.ResultAsync(TypedResults.Json(new { Error = "Failed to list annotations", Details = result.CurrentMessage }, statusCode: 500)).ConfigureAwait(false);
            return;
        }

        var names = await DataSetAnnotationTags.Read(_tags, _dataSets, req.DataSetName, Logger, ct).ConfigureAwait(false);
        if (names.IsFailure || names.Value is null)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(names, HttpContext)).ConfigureAwait(false);
            return;
        }

        var annotations = result.Value?
            .Where(a => string.Equals(a.DataSetName, req.DataSetName, System.StringComparison.OrdinalIgnoreCase))
            .Select(MapToDto)
            .ToList() ?? [];
        foreach (var annotation in annotations)
        {
            annotation.Tags = names.Value;
        }

        await Send.OkAsync(annotations, ct).ConfigureAwait(false);
    }

    /// <summary>Maps a DataSetAnnotationImplementationConfiguration to its corresponding DTO.</summary>
    protected virtual DataSetAnnotationPayload MapToDto(IDataSetAnnotationImplementationConfiguration config)
    {
        return DataSetAnnotationPayloadMapper.Map(config);
    }
}
