using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Quality;
using Fdw.Services.Quality.Configuration;
using Microsoft.AspNetCore.Http;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>Endpoint that retrieves a single glossary term by its identifier.</summary>
public abstract class GetGlossaryTermEndpointBase : Endpoint<GlossaryTermIdRequest, GlossaryTermResponse>
{
    private readonly IGlossaryTermConfigurationProvider _provider;

    /// <summary>Initializes a new instance of the <see cref="GetGlossaryTermEndpointBase"/> class.</summary>
    /// <param name="provider">The configuration provider for quality and catalog data.</param>
    protected GetGlossaryTermEndpointBase(IGlossaryTermConfigurationProvider provider)
    {
        _provider = provider;
    }

    /// <summary>Gets the authorization policy required for read operations.</summary>
    protected virtual string ReadPolicy => "datastores:read";

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Get("/catalog/glossary/{Id}");
#if DEVELOP
        AllowAnonymous();
#else
        Policies(ReadPolicy);
#endif
        Summary(s => s.Summary = "Get glossary term by ID");
    }

    /// <summary>Retrieves a glossary term by its identifier, returning 404 if not found.</summary>
    public override async Task HandleAsync(GlossaryTermIdRequest req, CancellationToken ct)
    {
        var result = await _provider.Get(req.Id, ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            HttpContext.Response.StatusCode = 500;
            await HttpContext.Response.WriteAsJsonAsync(
                new { Error = "Failed to get glossary term", Details = result.CurrentMessage }, ct).ConfigureAwait(false);
            return;
        }

        if (result.Value is null)
        {
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(MapToDto(result.Value), ct).ConfigureAwait(false);
    }

    /// <summary>Maps a GlossaryTermImplementationConfiguration to its corresponding DTO.</summary>
    protected virtual GlossaryTermResponse MapToDto(GlossaryTermImplementationConfiguration config)
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
