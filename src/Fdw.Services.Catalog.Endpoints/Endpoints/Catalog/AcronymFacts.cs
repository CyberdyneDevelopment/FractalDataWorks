using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Quality;
using Fdw.Services.Quality.Configuration;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>
/// The rules for a glossary term that is an acronym (FDW-1682): it states what it stands for and whether it is approved, together, and
/// its name is unique among the terms ignoring case.
/// </summary>
/// <remarks>
/// Nothing is defaulted: an acronym that does not say whether it is approved is refused, and a term that says it is approved without
/// being an acronym is refused, so the approved state always means something.
/// </remarks>
internal static class AcronymFacts
{
    /// <summary>The most characters of "stands for" the column keeps.</summary>
    internal const int MaxStandsForLength = 200;

    /// <summary>Finds why the facts of a term are not a valid acronym, or none when they are.</summary>
    /// <param name="term">The term's name, for the refusal.</param>
    /// <param name="standsFor">What it stands for, or null when it is not an acronym.</param>
    /// <param name="isApproved">Whether it is approved, or null when not stated.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>The refusal, or null when the facts are valid.</returns>
    internal static IGenericMessage? Refusal(string term, string? standsFor, bool? isApproved, ILogger logger)
    {
        if (standsFor is null)
        {
            return isApproved is null ? null : AcronymLog.ApprovalWithoutStandsFor(logger, term);
        }

        if (string.IsNullOrWhiteSpace(standsFor))
        {
            return AcronymLog.StandsForEmpty(logger, term);
        }

        if (standsFor.Length > MaxStandsForLength)
        {
            return AcronymLog.StandsForTooLong(logger, term, standsFor.Length, MaxStandsForLength);
        }

        return isApproved is null ? AcronymLog.ApprovalNotStated(logger, term) : null;
    }

    /// <summary>Checks the facts and the name of a term about to be saved.</summary>
    /// <param name="provider">Reads every term, to find a name that is taken.</param>
    /// <param name="term">The term's name.</param>
    /// <param name="standsFor">What it stands for, or null when it is not an acronym.</param>
    /// <param name="isApproved">Whether it is approved, or null when not stated.</param>
    /// <param name="except">The term that is being saved, which does not clash with itself; none for a new term.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>Success, or the refusal (400 for the facts, 409 for a name that is taken) or the failure of the read.</returns>
    internal static async Task<IGenericResult> Check(
        IGlossaryTermConfigurationProvider provider,
        string term,
        string? standsFor,
        bool? isApproved,
        Guid? except,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (Refusal(term, standsFor, isApproved, logger) is { } refusal)
        {
            return GenericResult.Failure(refusal);
        }

        var terms = await provider.Get(cancellationToken).ConfigureAwait(false);
        if (terms.IsFailure || terms.Value is null)
        {
            return terms;
        }

        return NameTaken(terms.Value, term, except) ? GenericResult.Failure(AcronymLog.NameTaken(logger, term)) : GenericResult.Success();
    }

    /// <summary>Finds whether another term already has the name, ignoring case.</summary>
    /// <param name="terms">Every term.</param>
    /// <param name="name">The name to check.</param>
    /// <param name="except">The term that is being saved, which does not clash with itself; none for a new term.</param>
    /// <returns><see langword="true"/> when a different term has the name.</returns>
    internal static bool NameTaken(IEnumerable<IGlossaryTermImplementationConfiguration> terms, string name, Guid? except) =>
        terms.Any(term => term.Id != except && string.Equals(term.Name, name, StringComparison.OrdinalIgnoreCase));
}
