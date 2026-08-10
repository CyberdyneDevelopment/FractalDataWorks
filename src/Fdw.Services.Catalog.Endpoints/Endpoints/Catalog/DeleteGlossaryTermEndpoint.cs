using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Quality;
using Microsoft.AspNetCore.Http;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>Endpoint that deletes an existing glossary term from the catalog.</summary>
public abstract class DeleteGlossaryTermEndpoint : Endpoint<GlossaryTermIdRequest>
{
    private readonly QualityConfigurationProvider _provider;

    /// <summary>Initializes a new instance of the <see cref="DeleteGlossaryTermEndpoint"/> class.</summary>
    /// <param name="provider">The configuration provider for quality and catalog data.</param>
    protected DeleteGlossaryTermEndpoint(QualityConfigurationProvider provider)
    {
        _provider = provider;
    }

    /// <summary>Gets the authorization policy required for write operations.</summary>
    protected virtual string WritePolicy => "datastores:write";

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Delete("/catalog/glossary/{Id}");
#if DEVELOP
        AllowAnonymous();
#else
        Policies(WritePolicy);
#endif
        Summary(s => s.Summary = "Delete a glossary term");
    }

    /// <summary>Deletes the glossary term identified by the request ID and returns 204 No Content on success.</summary>
    public override async Task HandleAsync(GlossaryTermIdRequest req, CancellationToken ct)
    {
        var result = await _provider.DeleteGlossaryTerm(req.Id, ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            HttpContext.Response.StatusCode = 500;
            await HttpContext.Response.WriteAsJsonAsync(
                new { Error = "Failed to delete glossary term", Details = result.CurrentMessage }, ct).ConfigureAwait(false);
            return;
        }

        await Send.NoContentAsync(ct).ConfigureAwait(false);
    }
}
