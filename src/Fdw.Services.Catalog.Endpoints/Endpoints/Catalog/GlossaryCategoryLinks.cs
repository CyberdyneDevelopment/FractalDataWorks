using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Quality.Configuration;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>
/// How a glossary term and the Glossary category tree meet (FDW-1466): a request names a category by its stable name, an answer carries
/// the name and the path, and a failure is answered as every endpoint answers one.
/// </summary>
internal static class GlossaryCategoryLinks
{
    /// <summary>Reads the Glossary tree and finds the category a request names, before anything is written.</summary>
    internal static Task<IGenericResult<(CategoryTree Tree, Guid? Id)>> Requested(
        ICategoryConfigurationProvider categories, string? name, ILogger logger, CancellationToken ct) =>
        CategoryLinks.Requested(categories, GlossaryCategoryConfiguration.ImplementationName, name, logger, ct);

    /// <summary>Writes a term as the API answers it: its category by name and path.</summary>
    internal static IGenericResult<GlossaryTermResponse> Describe(
        IGlossaryTermImplementationConfiguration term, CategoryTree tree, ILogger logger)
    {
        var label = CategoryLinks.Label(tree, GlossaryCategoryConfiguration.ImplementationName, term.CategoryId, logger);
        if (label.IsFailure || label.Value is null)
        {
            return label.ToNewResult<GlossaryTermResponse>();
        }

        var response = GlossaryTermMapper.ToDto(term);
        response.Category = label.Value.Name;
        response.CategoryPath = label.Value.Path;
        return GenericResult<GlossaryTermResponse>.Success(response);
    }

    /// <summary>Writes a term, as the glossary terms domain table holds it, the way the API answers it: its category by name and path.</summary>
    internal static IGenericResult<GlossaryTermResponse> Describe(
        GlossaryTermDomainConfiguration term, CategoryTree tree, ILogger logger)
    {
        var label = CategoryLinks.Label(tree, GlossaryCategoryConfiguration.ImplementationName, term.CategoryId, logger);
        if (label.IsFailure || label.Value is null)
        {
            return label.ToNewResult<GlossaryTermResponse>();
        }

        var response = GlossaryTermMapper.ToDto(term);
        response.Category = label.Value.Name;
        response.CategoryPath = label.Value.Path;
        return GenericResult<GlossaryTermResponse>.Success(response);
    }

    /// <summary>The answer to a failed read or refused request.</summary>
    internal static IResult Failure(IGenericResult failure, HttpContext context) => ResultHttpStatusMapper.Answer(failure, context);
}
