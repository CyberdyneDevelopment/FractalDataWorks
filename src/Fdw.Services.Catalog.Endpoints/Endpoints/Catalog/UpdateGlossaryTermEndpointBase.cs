using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Quality;
using Microsoft.AspNetCore.Http;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>Endpoint that updates an existing glossary term in the catalog.</summary>
public abstract class UpdateGlossaryTermEndpointBase : Endpoint<UpdateGlossaryTermRequest, GlossaryTermResponse>
{
    private readonly IGlossaryTermConfigurationProvider _provider;

    /// <summary>Initializes a new instance of the <see cref="UpdateGlossaryTermEndpointBase"/> class.</summary>
    /// <param name="provider">The configuration provider for quality and catalog data.</param>
    protected UpdateGlossaryTermEndpointBase(IGlossaryTermConfigurationProvider provider)
    {
        _provider = provider;
    }

    /// <summary>Gets the authorization policy required for write operations.</summary>
    protected virtual string WritePolicy => "datastores:write";

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Patch("/catalog/glossary/{Id}");
#if DEVELOP
        AllowAnonymous();
#else
        Policies(WritePolicy);
#endif
        Summary(s => s.Summary = "Update a glossary term");
    }

    /// <summary>Updates the glossary term identified by the request ID and returns the updated DTO.</summary>
    public override async Task HandleAsync(UpdateGlossaryTermRequest req, CancellationToken ct)
    {
        // Loading the existing row first and merging onto it (rather than building a fresh config
        // from the request alone) is what keeps a caller that only sends the fields it changed from
        // wiping everything else -- and is the only way Steward, Formula, RelatedTerms and
        // LinkedDataSets (none of which this update DTO exposes) survive an update at all; the prior
        // code did not read the existing row and hardcoded Steward to empty on every call.
        var existingResult = await _provider.Get(req.Id, ct).ConfigureAwait(false);
        if (!existingResult.IsSuccess)
        {
            await Send.ResultAsync(TypedResults.Json(new { Error = "Failed to load glossary term", Details = existingResult.CurrentMessage }, statusCode: 500)).ConfigureAwait(false);
            return;
        }

        if (existingResult.Value is null)
        {
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var config = existingResult.Value;
        GlossaryTermMapper.ApplyDto(config, req);

        var result = await _provider.Save(config, "GlossaryTerm", "GlossaryTerm", config.Name, ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            await Send.ResultAsync(TypedResults.Json(new { Error = "Failed to update glossary term", Details = result.CurrentMessage }, statusCode: 500)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(GlossaryTermMapper.ToDto(config), ct).ConfigureAwait(false);
    }
}
