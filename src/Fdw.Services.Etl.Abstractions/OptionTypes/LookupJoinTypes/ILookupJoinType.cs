using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// Interface for lookup join type options consumed by Lookup transforms.
/// </summary>
public interface ILookupJoinType : ITypeOption<int, ILookupJoinType>
{
    /// <summary>
    /// Gets whether a missing lookup key should be reported as a transform error (Inner semantics)
    /// rather than left null (Left semantics).
    /// </summary>
    bool FailOnMissing { get; }
}
