using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// Finds the things of one kind (the fields of data sets, the fields of containers, the glossary terms) that are closest to a text, asking
/// the database for them and reading only the rows of the page.
/// </summary>
/// <remarks>
/// These kinds can number in the hundreds of thousands, so reading them whole to rank them is the cost this removes. A search reads the
/// closest ones directly, in order, and counts the rest. The names a thing sits under are read afterwards, for the things a page keeps.
/// </remarks>
public interface IRankedSearch
{
    /// <summary>Gets the kind of thing this finds, such as <c>DataSetField</c>.</summary>
    string Kind { get; }

    /// <summary>Gets the most things one search window may hold.</summary>
    /// <remarks>The ceiling of the In operator, which carries each key as a parameter of the statement.</remarks>
    int MaxWindow { get; }

    /// <summary>Finds the closest things to a text.</summary>
    /// <param name="text">What to find; empty lists the first things by name.</param>
    /// <param name="window">How many of the closest things to return, at least 1 and at most <see cref="MaxWindow"/>.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The closest things and how many match in all, or the failure of a read or a refusal of the window.</returns>
    Task<IGenericResult<SearchPage>> Search(string text, int window, CancellationToken cancellationToken = default);

    /// <summary>Reads the names the given things sit under.</summary>
    /// <param name="hits">The things of a page, at most <see cref="MaxWindow"/>.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// The things in the same order, each with its parents, or the failure of a read. A thing whose parent the caller cannot read is left
    /// out, as it is absent from every other answer.
    /// </returns>
    Task<IGenericResult<IReadOnlyList<SearchHit>>> Place(IReadOnlyList<SearchHit> hits, CancellationToken cancellationToken = default);
}
