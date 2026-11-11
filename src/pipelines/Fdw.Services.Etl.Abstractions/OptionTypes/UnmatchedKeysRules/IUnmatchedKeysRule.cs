using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// What an update or delete destination does with a key that matched no row (FDW-4988): Fail or Warn, stated by the destination; no default.
/// </summary>
public interface IUnmatchedKeysRule : ITypeOption<int, IUnmatchedKeysRule>
{
    /// <summary>Gets a value indicating whether a batch with an unmatched key fails the destination and rolls back (Fail) instead of committing and being counted (Warn).</summary>
    bool FailsLoad { get; }
}
