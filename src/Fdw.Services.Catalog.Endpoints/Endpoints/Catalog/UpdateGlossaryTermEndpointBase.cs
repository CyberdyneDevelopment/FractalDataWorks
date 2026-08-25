using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Quality;
using Microsoft.AspNetCore.Http;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>Endpoint that updates an existing glossary term in the catalog.</summary>
public abstract class UpdateGlossaryTermEndpointBase : Endpoint<GlossaryTermResponse, GlossaryTermResponse>
{
    private readonly QualityConfigurationProvider _provider;

    /// <summary>Initializes a new instance of the <see cref="UpdateGlossaryTermEndpointBase"/> class.</summary>
    /// <param name="provider">The configuration provider for quality and catalog data.</param>
    protected UpdateGlossaryTermEndpointBase(QualityConfigurationProvider provider)
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
    public override async Task HandleAsync(GlossaryTermResponse req, CancellationToken ct)
    {
        // Why: MapFromDto preserves dto.Id — when non-empty the provider issues UPDATE.
        var config = GlossaryTermMapper.MapFromDto(req);

        var result = await _provider.SaveGlossaryTerm(config, ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            HttpContext.Response.StatusCode = 500;
            await HttpContext.Response.WriteAsJsonAsync(
                new { Error = "Failed to update glossary term", Details = result.CurrentMessage }, ct).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(req, ct).ConfigureAwait(false);
    }
}
