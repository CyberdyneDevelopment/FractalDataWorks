using System.Collections.Generic;
using System.Linq;
using Fdw.Messages;
using Fdw.Results;

namespace Fdw.Services.Data;

/// <summary>
/// Carries the warnings a schema discovery returned with its success onto the result of the work done with what it found
/// (FDW-6309). A discovery that succeeds with a warning (a foreign key whose target it did not read, key metadata the login cannot
/// see) says so once; a layer that builds its own result from the containers must not drop it.
/// </summary>
internal static class DiscoveryWarnings
{
    /// <summary>
    /// Builds a success carrying <paramref name="value"/> and <paramref name="warnings"/>.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">What the work produced.</param>
    /// <param name="warnings">The messages the discovery returned with its success.</param>
    /// <returns>A plain success when there is no warning; otherwise a success with warnings carrying every one of them.</returns>
    internal static IGenericResult<T> Carry<T>(T value, IReadOnlyList<IGenericMessage> warnings)
        => warnings.Count == 0
            ? GenericResult<T>.Success(value)
            : GenericResult<T>.Success(value, ResultStatuses.ByName("SuccessWithWarnings"), warnings.ToArray());
}
