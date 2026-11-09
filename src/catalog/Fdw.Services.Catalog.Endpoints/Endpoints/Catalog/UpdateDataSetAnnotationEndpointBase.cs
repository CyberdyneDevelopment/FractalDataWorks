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
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Tags;
using Fdw.Web.RestEndpoints.ErrorMapping;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>Endpoint that updates an existing DataSet annotation.</summary>
public abstract class UpdateDataSetAnnotationEndpointBase : Endpoint<DataSetAnnotationPayload, DataSetAnnotationPayload>
{
    private readonly IDataSetAnnotationConfigurationProvider _provider;
    private readonly IDataSetConfigurationProvider _dataSets;
    private readonly ITagService _tags;
    private readonly IAuthenticationContextAccessor _authentication;

    /// <summary>Initializes a new instance of the <see cref="UpdateDataSetAnnotationEndpointBase"/> class.</summary>
    /// <param name="provider">The configuration provider for quality and catalog data.</param>
    /// <param name="dataSets">Finds the data set the annotation names, which carries the tags.</param>
    /// <param name="tags">Makes the data set's tags equal the request's.</param>
    /// <param name="authentication">The caller's tenant, in which a tag the tenant does not have yet is made.</param>
    protected UpdateDataSetAnnotationEndpointBase(
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
        var getResult = await _provider.Get(req.DataSetName, ct).ConfigureAwait(false);

        if (!getResult.IsSuccess)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(getResult, HttpContext)).ConfigureAwait(false);
            return;
        }

        var config = DataSetAnnotationRequestMapper.FromPayload(
            req.DataSetName, req.Owner, req.Steward, req.Classification);

        if (getResult.Value is { } stored)
        {
            config.Id = stored.Id;
            // Why: an update states the note's content, not whether someone resolved it. A new row built from the request alone would
            // save every updated note as unresolved.
            config.IsResolved = stored.IsResolved;
            config.ResolvedDate = stored.ResolvedDate;
            config.ResolvedBy = stored.ResolvedBy;
        }

        var result = await _provider.Save(config, "DataSetAnnotation", "DataSetAnnotation", config.Name, ct).ConfigureAwait(false);

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

        await Send.OkAsync(req, ct).ConfigureAwait(false);
    }
}
