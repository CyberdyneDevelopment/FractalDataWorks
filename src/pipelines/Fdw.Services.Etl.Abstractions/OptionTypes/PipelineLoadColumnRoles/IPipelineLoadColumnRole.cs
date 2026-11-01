using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// The part a column plays in a load: Key, Compare, Write or SequenceOrder (FDW-1108).
/// </summary>
public interface IPipelineLoadColumnRole : ITypeOption<int, IPipelineLoadColumnRole>
{
    /// <summary>Gets whether the role is only valid on a value source (SequenceOrder) rather than on a load step.</summary>
    bool BelongsToValueSource { get; }

    /// <summary>Gets whether the role makes the column part of the key rows are matched on (Key).</summary>
    bool IsMatchKey { get; }

    /// <summary>Gets whether the role makes the column one whose difference means a matched row changed (Compare).</summary>
    bool IsComparison { get; }

    /// <summary>Gets whether the role makes the column one the load writes (Write).</summary>
    bool IsWritten { get; }
}
