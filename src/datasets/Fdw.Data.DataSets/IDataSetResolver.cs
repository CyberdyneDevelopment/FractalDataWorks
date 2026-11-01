using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// Turns a data set name into what it names: a data set, or a variation of one such as a saved view (FDW-2389, FDW-2390).
/// </summary>
/// <remarks>
/// A variation answers the routes its data set answers, so the routes that only read ask this instead of the data set provider. The
/// routes that write keep asking the data set provider, so a write aimed at a variation's name finds no data set and is a 404, never a
/// write to the root. The kinds of variation are the ones the variation domain registers; nothing here names one.
/// </remarks>
public interface IDataSetResolver
{
    /// <summary>Resolves a name, reading the data set as a whole aggregate.</summary>
    /// <param name="name">The name of a data set or of a variation of one.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>What the name names; success with null when nothing has the name; a failure when a data set and a variation both have it, or when a variation's data set is gone.</returns>
    Task<IGenericResult<ResolvedDataSet?>> Resolve(string name, CancellationToken cancellationToken = default);

    /// <summary>Resolves a name, reading the data set to a given depth.</summary>
    /// <param name="name">The name of a data set or of a variation of one.</param>
    /// <param name="levels">How deep to read the data set: a <c>ConfigurationLevels</c> value.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>As the overload without a depth.</returns>
    Task<IGenericResult<ResolvedDataSet?>> Resolve(string name, int levels, CancellationToken cancellationToken = default);
}
