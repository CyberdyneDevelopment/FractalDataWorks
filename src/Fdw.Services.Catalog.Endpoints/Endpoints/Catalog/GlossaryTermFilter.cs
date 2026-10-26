using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Quality.Configuration;
using Fdw.Web.RestEndpoints.Pagination;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>
/// Builds the filter a glossary list asks the store for: the search text, whether a term is an acronym, whether an acronym is approved,
/// and the categories a term sits under (FDW-1682, FDW-2336).
/// </summary>
/// <remarks>
/// Every condition is on a field of the term's domain row, so the store narrows the list and the page is read from the rows that remain.
/// The search text matches the name, the definition and what an acronym stands for in the way <c>?match=</c> names. A filter that is not
/// true or false is refused, never read as absent.
/// </remarks>
internal static class GlossaryTermFilter
{
    /// <summary>The most categories one list can be narrowed to: a category and every category below it.</summary>
    /// <remarks>The ceiling of the In operator, which carries each category as a parameter of the statement.</remarks>
    internal const int MaxCategories = 1000;

    private static readonly IReadOnlyList<string> TextProperties =
    [
        nameof(GlossaryTermDomainConfiguration.Name),
        nameof(GlossaryTermDomainConfiguration.Definition),
        nameof(GlossaryTermDomainConfiguration.StandsFor),
    ];

    /// <summary>Builds the filter of a list request.</summary>
    /// <param name="search">The search text, match and identifiers the request asked for.</param>
    /// <param name="acronym">"true" for the acronyms only, "false" for the terms that are not; none for both.</param>
    /// <param name="approved">"true" for the approved acronyms, "false" for the acronyms not approved; none for either.</param>
    /// <param name="categories">The categories the terms must sit under (the one asked for and every category below it); none for any.</param>
    /// <param name="categoryName">The name of the category asked for, for a refusal.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>The filter, nothing when the request narrows nothing, or the refusal of a filter that is not true or false or a category too broad to carry.</returns>
    internal static IGenericResult<IFilterExpression?> Build(
        ListSearch search,
        string? acronym,
        string? approved,
        IReadOnlyCollection<Guid>? categories,
        string? categoryName,
        ILogger logger)
    {
        var onlyAcronyms = Flag("acronym", acronym, logger);
        if (onlyAcronyms.IsFailure)
        {
            return onlyAcronyms.ToNewResult<IFilterExpression?>();
        }

        var onlyApproved = Flag("approved", approved, logger);
        if (onlyApproved.IsFailure)
        {
            return onlyApproved.ToNewResult<IFilterExpression?>();
        }

        if (categories is { Count: > MaxCategories })
        {
            return GenericResult<IFilterExpression?>.Failure(AcronymLog.CategoryTooBroad(logger, categoryName ?? string.Empty, categories.Count, MaxCategories));
        }

        var nodes = new List<IFilterNode>();
        if (search.Filter(TextProperties, nameof(GlossaryTermDomainConfiguration.Id))?.Root is { } text)
        {
            nodes.Add(text);
        }

        if (onlyAcronyms.Value is { } wantAcronym)
        {
            nodes.Add(Condition(nameof(GlossaryTermDomainConfiguration.StandsFor), wantAcronym ? "IsNotNull" : "IsNull", null));
        }

        if (onlyApproved.Value is { } wantApproved)
        {
            nodes.Add(Condition(nameof(GlossaryTermDomainConfiguration.StandsFor), "IsNotNull", null));
            nodes.Add(Condition(nameof(GlossaryTermDomainConfiguration.IsApproved), "Equal", wantApproved));
        }

        if (categories is not null)
        {
            nodes.Add(Condition(nameof(GlossaryTermDomainConfiguration.CategoryId), "In", categories.Cast<object?>().ToList()));
        }

        IFilterExpression? filter = nodes.Count switch
        {
            0 => null,
            1 => new FilterExpression { Root = nodes[0] },
            _ => new FilterExpression { Root = new FilterGroup { Operator = LogicalOperator.And, Nodes = nodes } },
        };
        return GenericResult<IFilterExpression?>.Success(filter);
    }

    private static FilterCondition Condition(string property, string operatorName, object? value) =>
        new() { PropertyName = property, Operator = FilterOperators.ByName(operatorName), Value = value };

    private static IGenericResult<bool?> Flag(string filter, string? value, ILogger logger)
    {
        if (string.IsNullOrEmpty(value))
        {
            return GenericResult<bool?>.Success(null);
        }

        return bool.TryParse(value, out var parsed)
            ? GenericResult<bool?>.Success(parsed)
            : GenericResult<bool?>.Failure(AcronymLog.FilterNotBoolean(logger, filter, value));
    }
}
