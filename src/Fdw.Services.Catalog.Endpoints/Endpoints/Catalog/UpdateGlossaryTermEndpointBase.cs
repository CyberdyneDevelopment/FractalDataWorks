using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Quality;
using Fdw.Services.Quality.Configuration;
using Microsoft.AspNetCore.Http;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>Endpoint that updates an existing glossary term in the catalog.</summary>
public abstract class UpdateGlossaryTermEndpointBase : Endpoint<UpdateGlossaryTermRequest, GlossaryTermResponse>
{
    private readonly IGlossaryTermConfigurationProvider _provider;
    private readonly ICategoryConfigurationProvider _categories;

    /// <summary>Initializes a new instance of the <see cref="UpdateGlossaryTermEndpointBase"/> class.</summary>
    /// <param name="provider">The configuration provider for quality and catalog data.</param>
    /// <param name="categories">Reads the Glossary category tree the terms are organised in.</param>
    protected UpdateGlossaryTermEndpointBase(IGlossaryTermConfigurationProvider provider, ICategoryConfigurationProvider categories)
    {
        _provider = provider;
        _categories = categories;
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

        // A category left out leaves the stored one; an empty name takes the term out of its category; a name must be a category of the
        // Glossary type.
        var category = await GlossaryCategoryLinks.Requested(_categories, req.Category, Logger, ct).ConfigureAwait(false);
        if (category.IsFailure)
        {
            await Send.ResultAsync(GlossaryCategoryLinks.Failure(category, HttpContext)).ConfigureAwait(false);
            return;
        }

        // The category the term will sit under must be in the tree before anything is written: a term that keeps a category that has gone
        // would otherwise be saved and then answered as a fault.
        var config = existingResult.Value;
        var target = CategoryLinks.Label(
            category.Value.Tree, GlossaryCategoryConfiguration.ImplementationName, req.Category is null ? config.CategoryId : category.Value.Id, Logger);
        if (target.IsFailure)
        {
            await Send.ResultAsync(GlossaryCategoryLinks.Failure(target, HttpContext)).ConfigureAwait(false);
            return;
        }

        var merged = await CheckMerged(req, config, ct).ConfigureAwait(false);
        if (merged.IsFailure)
        {
            await Send.ResultAsync(GlossaryCategoryLinks.Failure(merged, HttpContext)).ConfigureAwait(false);
            return;
        }

        GlossaryTermMapper.ApplyDto(config, req, category.Value.Id);

        var result = await _provider.Save(config, "GlossaryTerm", "GlossaryTerm", config.Name, ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            await Send.ResultAsync(TypedResults.Json(new { Error = "Failed to update glossary term", Details = result.CurrentMessage }, statusCode: 500)).ConfigureAwait(false);
            return;
        }

        var described = GlossaryCategoryLinks.Describe(config, category.Value.Tree, Logger);
        if (described.IsFailure || described.Value is null)
        {
            await Send.ResultAsync(GlossaryCategoryLinks.Failure(described, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(described.Value, ct).ConfigureAwait(false);
    }

    // The facts and the name the term will have once the request is merged onto it (FDW-1682): a field left out keeps what is stored.
    private Task<IGenericResult> CheckMerged(UpdateGlossaryTermRequest req, IGlossaryTermImplementationConfiguration config, CancellationToken ct) =>
        AcronymFacts.Check(
            _provider,
            req.Name is { } name ? name : config.Name,
            req.StandsFor is { } standsFor ? standsFor : config.StandsFor,
            req.IsApproved is { } approved ? approved : config.IsApproved,
            config.Id,
            Logger,
            ct);
}
