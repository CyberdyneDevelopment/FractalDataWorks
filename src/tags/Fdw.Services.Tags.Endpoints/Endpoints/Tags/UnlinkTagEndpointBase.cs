using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Results;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;

namespace Fdw.Services.Tags.Endpoints;

/// <summary>Takes a tag off a thing of any taggable kind.</summary>
public abstract class UnlinkTagEndpointBase : Endpoint<TagLinkRequest>
{
    private readonly ITagService _tags;

    /// <summary>Initializes a new instance of the <see cref="UnlinkTagEndpointBase"/> class.</summary>
    /// <param name="tags">The Tags service.</param>
    protected UnlinkTagEndpointBase(ITagService tags)
    {
        _tags = tags;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Delete("/tags/{Name}/things/{Kind}/{ThingId}");
        Policies("tags:write");
        Summary(s => s.Summary = "Take a tag off a thing");
        Description(x => x.WithTags("tags"));
    }

    /// <inheritdoc />
    public override async Task HandleAsync(TagLinkRequest req, CancellationToken ct)
    {
        var outcome = await _tags.Unlink(req.Kind, req.ThingId, req.Name, ct).ConfigureAwait(false);
        if (outcome.IsFailure)
        {
            var (statusCode, errorResponse) = ResultHttpStatusMapper.Map(outcome, HttpContext);
            await Send.ResultAsync(TypedResults.Json(errorResponse, statusCode: statusCode)).ConfigureAwait(false);
            return;
        }

        await Send.NoContentAsync(ct).ConfigureAwait(false);
    }
}
