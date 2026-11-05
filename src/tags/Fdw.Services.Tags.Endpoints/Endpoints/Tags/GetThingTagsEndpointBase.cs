using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Results;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;

namespace Fdw.Services.Tags.Endpoints;

/// <summary>Lists the names of the tags a thing carries.</summary>
public abstract class GetThingTagsEndpointBase : Endpoint<ThingTagsRequest, List<string>>
{
    private readonly ITagService _tags;

    /// <summary>Initializes a new instance of the <see cref="GetThingTagsEndpointBase"/> class.</summary>
    /// <param name="tags">The Tags service.</param>
    protected GetThingTagsEndpointBase(ITagService tags)
    {
        _tags = tags;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/things/{Kind}/{ThingId}/tags");
        Policies("tags:read");
        Summary(s => s.Summary = "List the tags of a thing");
        Description(x => x.WithTags("tags"));
    }

    /// <inheritdoc />
    public override async Task HandleAsync(ThingTagsRequest req, CancellationToken ct)
    {
        var outcome = await _tags.TagsOf(req.Kind, req.ThingId, ct).ConfigureAwait(false);
        if (outcome.IsFailure || outcome.Value is not { } found)
        {
            var (statusCode, errorResponse) = ResultHttpStatusMapper.Map(outcome, HttpContext);
            await Send.ResultAsync(TypedResults.Json(errorResponse, statusCode: statusCode)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync([.. found], ct).ConfigureAwait(false);
    }
}
