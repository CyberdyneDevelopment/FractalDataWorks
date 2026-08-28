using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Quality;
using Fdw.Services.Quality.Configuration;
using Microsoft.AspNetCore.Http;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>Endpoint that lists all glossary terms in the catalog.</summary>
public abstract class ListGlossaryTermsEndpointBase : EndpointWithoutRequest<List<GlossaryTermResponse>>
{
    private readonly QualityConfigurationProvider _provider;

    /// <summary>Initializes a new instance of the <see cref="ListGlossaryTermsEndpointBase"/> class.</summary>
    /// <param name="provider">The configuration provider for quality and catalog data.</param>
    protected ListGlossaryTermsEndpointBase(QualityConfigurationProvider provider)
    {
        _provider = provider;
    }

    /// <summary>Gets the authorization policy required for read operations.</summary>
    protected virtual string ReadPolicy => "datastores:read";

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Get("/catalog/glossary");
#if DEVELOP
        AllowAnonymous();
#else
        Policies(ReadPolicy);
#endif
        Summary(s => s.Summary = "List glossary terms");
    }

    /// <summary>Retrieves all glossary terms, optionally filtered by a search query, and returns them as a list of DTOs.</summary>
    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await _provider.GetAllGlossaryTerms(ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            HttpContext.Response.StatusCode = 500;
            await HttpContext.Response.WriteAsJsonAsync(
                new { Error = "Failed to list glossary terms", Details = result.CurrentMessage }, ct).ConfigureAwait(false);
            return;
        }

        var query = HttpContext.Request.Query["query"].FirstOrDefault()
            ?? HttpContext.Request.Query["q"].FirstOrDefault();

        var terms = result.Value ?? [];

        if (!string.IsNullOrWhiteSpace(query))
        {
            terms = terms
                .Where(t =>
                    t.Name.Contains(query, System.StringComparison.OrdinalIgnoreCase) ||
                    t.Definition.Contains(query, System.StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        await Send.OkAsync(terms.Select(MapToDto).ToList(), ct).ConfigureAwait(false);
    }

    /// <summary>Maps a GlossaryTermConfiguration to its corresponding DTO.</summary>
    protected virtual GlossaryTermResponse MapToDto(GlossaryTermConfiguration config)
    {
        return new GlossaryTermResponse
        {
            Id = config.Id,
            Name = config.Name,
            Definition = config.Definition,
            Category = config.Category,
            Owner = config.Owner
        };
    }
}
