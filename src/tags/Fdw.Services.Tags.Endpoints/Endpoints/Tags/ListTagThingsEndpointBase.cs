using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Results;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;

namespace Fdw.Services.Tags.Endpoints;

/// <summary>Lists the Ids of the things of a kind that carry a tag.</summary>
public abstract class ListTagThingsEndpointBase : Endpoint<TagThingsRequest, List<Guid>>
{
    private readonly ITagService _tags;

    /// <summary>Initializes a new instance of the <see cref="ListTagThingsEndpointBase"/> class.</summary>
    /// <param name="tags">The Tags service.</param>
    protected ListTagThingsEndpointBase(ITagService tags)
    {
        _tags = tags;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/tags/{Name}/things/{Kind}");
        Policies("tags:read");
        Summary(s => s.Summary = "List the things that carry a tag");
        Description(x => x.WithTags("tags"));
    }

    /// <inheritdoc />
    public override async Task HandleAsync(TagThingsRequest req, CancellationToken ct)
    {
        var outcome = await _tags.ThingsOf(req.Kind, [req.Name], ct).ConfigureAwait(false);
        if (outcome.IsFailure || outcome.Value is not { } found)
        {
            var (statusCode, errorResponse) = ResultHttpStatusMapper.Map(outcome, HttpContext);
            await Send.ResultAsync(TypedResults.Json(errorResponse, statusCode: statusCode)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync([.. found], ct).ConfigureAwait(false);
    }
}
