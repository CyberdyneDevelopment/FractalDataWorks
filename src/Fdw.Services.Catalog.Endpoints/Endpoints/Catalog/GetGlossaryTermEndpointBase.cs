using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Data;
using Fdw.Services.Quality;
using Fdw.Services.Quality.Configuration;
using Microsoft.AspNetCore.Http;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>Endpoint that retrieves a single glossary term by its identifier.</summary>
public abstract class GetGlossaryTermEndpointBase : Endpoint<GlossaryTermIdRequest, GlossaryTermResponse>
{
    private readonly IGlossaryTermConfigurationProvider _provider;
    private readonly ICategoryConfigurationProvider _categories;

    /// <summary>Initializes a new instance of the <see cref="GetGlossaryTermEndpointBase"/> class.</summary>
    /// <param name="provider">The configuration provider for quality and catalog data.</param>
    /// <param name="categories">Reads the Glossary category tree the terms are organised in.</param>
    protected GetGlossaryTermEndpointBase(IGlossaryTermConfigurationProvider provider, ICategoryConfigurationProvider categories)
    {
        _provider = provider;
        _categories = categories;
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
            await Send.ResultAsync(TypedResults.Json(new { Error = "Failed to get glossary term", Details = result.CurrentMessage }, statusCode: 500)).ConfigureAwait(false);
            return;
        }

        if (result.Value is null)
        {
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var tree = await CategoryTrees.Load(_categories, GlossaryCategoryConfiguration.ImplementationName, Logger, ct).ConfigureAwait(false);
        if (tree.IsFailure || tree.Value is null)
        {
            await Send.ResultAsync(GlossaryCategoryLinks.Failure(tree, HttpContext)).ConfigureAwait(false);
            return;
        }

        var described = GlossaryCategoryLinks.Describe(result.Value, tree.Value, Logger);
        if (described.IsFailure || described.Value is null)
        {
            await Send.ResultAsync(GlossaryCategoryLinks.Failure(described, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(described.Value, ct).ConfigureAwait(false);
    }
}
