using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Quality;
using Microsoft.AspNetCore.Http;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>Endpoint that creates a new glossary term in the catalog.</summary>
public abstract class CreateGlossaryTermEndpointBase : Endpoint<GlossaryTermResponse, GlossaryTermResponse>
{
    private readonly IGlossaryTermConfigurationProvider _provider;
    private readonly ICategoryConfigurationProvider _categories;

    /// <summary>Initializes a new instance of the <see cref="CreateGlossaryTermEndpointBase"/> class.</summary>
    /// <param name="provider">The configuration provider for quality and catalog data.</param>
    /// <param name="categories">Reads the Glossary category tree the terms are organised in.</param>
    protected CreateGlossaryTermEndpointBase(IGlossaryTermConfigurationProvider provider, ICategoryConfigurationProvider categories)
    {
        _provider = provider;
        _categories = categories;
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
        // The acronym facts and the name are judged before anything is written (FDW-1682).
        var checkedTerm = await AcronymFacts.Check(_provider, req.Name, req.StandsFor, req.IsApproved, null, Logger, ct).ConfigureAwait(false);
        if (checkedTerm.IsFailure)
        {
            await Send.ResultAsync(GlossaryCategoryLinks.Failure(checkedTerm, HttpContext)).ConfigureAwait(false);
            return;
        }

        // The category the request names, before anything is written.
        var category = await GlossaryCategoryLinks.Requested(_categories, req.Category, Logger, ct).ConfigureAwait(false);
        if (category.IsFailure)
        {
            await Send.ResultAsync(GlossaryCategoryLinks.Failure(category, HttpContext)).ConfigureAwait(false);
            return;
        }

        var config = GlossaryTermMapper.MapFromDto(req, category.Value.Id);

        var result = await _provider.Save(config, "GlossaryTerm", "GlossaryTerm", config.Name, ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            await Send.ResultAsync(TypedResults.Json(new { Error = "Failed to create glossary term", Details = result.CurrentMessage }, statusCode: 500)).ConfigureAwait(false);
            return;
        }

        req.Id = config.Id;
        var described = GlossaryCategoryLinks.Describe(config, category.Value.Tree, Logger);
        if (described.IsFailure || described.Value is null)
        {
            await Send.ResultAsync(GlossaryCategoryLinks.Failure(described, HttpContext)).ConfigureAwait(false);
            return;
        }

        req.Category = described.Value.Category;
        req.CategoryPath = described.Value.CategoryPath;

        await Send.CreatedAtAsync<GetGlossaryTermEndpointBase>(new { Id = req.Id }, req, cancellation: ct).ConfigureAwait(false);
    }
}
