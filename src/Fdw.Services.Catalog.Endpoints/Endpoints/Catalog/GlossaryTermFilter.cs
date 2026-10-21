using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Quality.Configuration;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>
/// Narrows the glossary list by the search text, by whether a term is an acronym and by whether an acronym is approved (FDW-1682).
/// </summary>
/// <remarks>
/// The search text matches the name, the definition and what an acronym stands for, ignoring case. A filter that is not true or false is
/// refused, never read as absent.
/// </remarks>
internal static class GlossaryTermFilter
{
    /// <summary>Applies the filters of a list request.</summary>
    /// <param name="terms">Every term.</param>
    /// <param name="query">The search text; none for every term.</param>
    /// <param name="acronym">"true" for the acronyms only, "false" for the terms that are not; none for both.</param>
    /// <param name="approved">"true" for the approved acronyms, "false" for the acronyms not approved; none for either.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>The terms that pass, or the refusal of a filter that is not true or false.</returns>
    internal static IGenericResult<List<IGlossaryTermImplementationConfiguration>> Apply(
        IEnumerable<IGlossaryTermImplementationConfiguration> terms, string? query, string? acronym, string? approved, ILogger logger)
    {
        var onlyAcronyms = Flag("acronym", acronym, logger);
        if (onlyAcronyms.IsFailure)
        {
            return onlyAcronyms.ToNewResult<List<IGlossaryTermImplementationConfiguration>>();
        }

        var onlyApproved = Flag("approved", approved, logger);
        if (onlyApproved.IsFailure)
        {
            return onlyApproved.ToNewResult<List<IGlossaryTermImplementationConfiguration>>();
        }

        var passed = terms
            .Where(term => string.IsNullOrWhiteSpace(query) || Matches(term, query))
            .Where(term => onlyAcronyms.Value is not { } wantAcronym || (term.StandsFor is not null) == wantAcronym)
            .Where(term => onlyApproved.Value is not { } wantApproved || (term.StandsFor is not null && term.IsApproved == wantApproved))
            .ToList();
        return GenericResult<List<IGlossaryTermImplementationConfiguration>>.Success(passed);
    }

    private static bool Matches(IGlossaryTermImplementationConfiguration term, string query) =>
        term.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
        || term.Definition.Contains(query, StringComparison.OrdinalIgnoreCase)
        || (term.StandsFor is not null && term.StandsFor.Contains(query, StringComparison.OrdinalIgnoreCase));

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
