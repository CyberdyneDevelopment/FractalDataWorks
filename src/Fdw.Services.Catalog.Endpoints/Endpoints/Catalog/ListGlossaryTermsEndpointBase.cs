using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Quality;
using Fdw.Services.Quality.Configuration;
using Microsoft.AspNetCore.Http;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>Endpoint that lists all glossary terms in the catalog.</summary>
public abstract class ListGlossaryTermsEndpointBase : EndpointWithoutRequest<List<GlossaryTermResponse>>
{
    private readonly IGlossaryTermConfigurationProvider _provider;
    private readonly ICategoryConfigurationProvider _categories;

    /// <summary>Initializes a new instance of the <see cref="ListGlossaryTermsEndpointBase"/> class.</summary>
    /// <param name="provider">The configuration provider for quality and catalog data.</param>
    /// <param name="categories">Reads the Glossary category tree the terms are organised in.</param>
    protected ListGlossaryTermsEndpointBase(IGlossaryTermConfigurationProvider provider, ICategoryConfigurationProvider categories)
    {
        _provider = provider;
        _categories = categories;
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
        var result = await _provider.Get(ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            await Send.ResultAsync(TypedResults.Json(new { Error = "Failed to list glossary terms", Details = result.CurrentMessage }, statusCode: 500)).ConfigureAwait(false);
            return;
        }

        var query = HttpContext.Request.Query["query"].FirstOrDefault()
            ?? HttpContext.Request.Query["q"].FirstOrDefault();

        var terms = result.Value ?? [];

        // ?q matches the name, the definition and what an acronym stands for; ?acronym= and ?approved= take true or false (FDW-1682).
        var filtered = GlossaryTermFilter.Apply(
            terms, query, HttpContext.Request.Query["acronym"].FirstOrDefault(), HttpContext.Request.Query["approved"].FirstOrDefault(), Logger);
        if (filtered.IsFailure || filtered.Value is null)
        {
            await Send.ResultAsync(GlossaryCategoryLinks.Failure(filtered, HttpContext)).ConfigureAwait(false);
            return;
        }

        terms = filtered.Value;

        // ?category= names a category of the Glossary tree: the terms under it and under every category below it.
        var category = await GlossaryCategoryLinks.Requested(_categories, HttpContext.Request.Query["category"].FirstOrDefault(), Logger, ct).ConfigureAwait(false);
        if (category.IsFailure)
        {
            await Send.ResultAsync(GlossaryCategoryLinks.Failure(category, HttpContext)).ConfigureAwait(false);
            return;
        }

        if (category.Value.Id is { } categoryId)
        {
            var under = category.Value.Tree.SelfAndDescendants(categoryId);
            terms = terms.Where(t => t.CategoryId is { } cited && under.Contains(cited)).ToList();
        }

        List<GlossaryTermResponse> answers = [];
        foreach (var term in terms)
        {
            var described = GlossaryCategoryLinks.Describe(term, category.Value.Tree, Logger);
            if (described.IsFailure || described.Value is null)
            {
                await Send.ResultAsync(GlossaryCategoryLinks.Failure(described, HttpContext)).ConfigureAwait(false);
                return;
            }

            answers.Add(described.Value);
        }

        await Send.OkAsync(answers, ct).ConfigureAwait(false);
    }
}
