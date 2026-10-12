using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// How a BatchCopy pipeline without load steps writes into its destination: Append, Replace or Upsert (FDW-1108).
/// </summary>
public interface ILoadMode : ITypeOption<int, ILoadMode>, ILoadOptionShape
{
    /// <summary>Gets whether the destination is truncated before the load (Replace).</summary>
    bool TruncatesFirst { get; }
}
