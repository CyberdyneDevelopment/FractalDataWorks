using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Quality;
using Microsoft.AspNetCore.Http;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>Endpoint that creates a new glossary term in the catalog.</summary>
public abstract class CreateGlossaryTermEndpointBase : Endpoint<GlossaryTermResponse, GlossaryTermResponse>
{
    private readonly QualityConfigurationProvider _provider;

    /// <summary>Initializes a new instance of the <see cref="CreateGlossaryTermEndpointBase"/> class.</summary>
    /// <param name="provider">The configuration provider for quality and catalog data.</param>
    protected CreateGlossaryTermEndpointBase(QualityConfigurationProvider provider)
    {
        _provider = provider;
    }

    /// <summary>Gets the authorization policy required for write operations.</summary>
    protected virtual string WritePolicy => "datastores:write";

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Post("/catalog/glossary");
#if DEVELOP
        AllowAnonymous();
#else
        Policies(WritePolicy);
#endif
        Summary(s => s.Summary = "Create a glossary term");
    }

    /// <summary>Creates a new glossary term and returns the created resource with a 201 status.</summary>
    public override async Task HandleAsync(GlossaryTermResponse req, CancellationToken ct)
    {
        var config = GlossaryTermMapper.MapFromDto(req);

        var result = await _provider.SaveGlossaryTerm(config, ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            HttpContext.Response.StatusCode = 500;
            await HttpContext.Response.WriteAsJsonAsync(
                new { Error = "Failed to create glossary term", Details = result.CurrentMessage }, ct).ConfigureAwait(false);
            return;
        }

        // Why: Reflect the minted Id back to the DTO so the caller receives the canonical identifier.
        req.Id = result.Value?.Id ?? req.Id;

        await Send.CreatedAtAsync<GetGlossaryTermEndpointBase>(new { Id = req.Id }, req, cancellation: ct).ConfigureAwait(false);
    }
}
