namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// A set-based write that can report the keys it was given that matched no row (FDW-4988). A caller opts in per command by stating how many such keys the
/// answer carries; such a command is run with <c>Execute&lt;BulkWriteAnswer&gt;</c>, and one that does not opt in answers a row count as before.
/// </summary>
public interface IReportsUnmatchedKeys
{
    /// <summary>
    /// Gets how many unmatched keys the answer carries (0: only the count), or <see langword="null"/> when this command does not report its unmatched keys.
    /// </summary>
    int? UnmatchedKeySample { get; }
}
