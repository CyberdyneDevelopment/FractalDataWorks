using System.Collections.Generic;
using Fdw.Data.Abstractions;

namespace Fdw.Commands.Data.SoftDeletes;

/// <summary>
/// The keys a load staged batch by batch (FDW-4956). A soft delete over it flags the target rows whose keys have no row in the stage for this stage key
/// (rows that disappeared from the source), and refuses when the stage does not hold exactly the rows the load appended.
/// </summary>
public sealed class StagedKeySet
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StagedKeySet"/> class.
    /// </summary>
    /// <param name="stage">The resolved stage container; it sits on the target's connection and declares the key columns and <paramref name="stageKeyColumn"/>.</param>
    /// <param name="stageKeyColumn">The stage column that tells this load's rows apart.</param>
    /// <param name="stageKey">This load's value in <paramref name="stageKeyColumn"/>.</param>
    /// <param name="keyColumns">The key columns, named the same in the stage and the target.</param>
    /// <param name="expectedRows">The rows the load appended to the stage under <paramref name="stageKey"/>: rows, not distinct keys.</param>
    public StagedKeySet(IStorageContainer stage, string stageKeyColumn, string stageKey, IReadOnlyList<string> keyColumns, long expectedRows)
    {
        Stage = stage;
        StageKeyColumn = stageKeyColumn;
        StageKey = stageKey;
        KeyColumns = keyColumns;
        ExpectedRows = expectedRows;
    }

    /// <summary>Gets the resolved stage container.</summary>
    public IStorageContainer Stage { get; }

    /// <summary>Gets the stage column that tells this load's rows apart.</summary>
    public string StageKeyColumn { get; }

    /// <summary>Gets this load's value in <see cref="StageKeyColumn"/>.</summary>
    public string StageKey { get; }

    /// <summary>Gets the key columns, named the same in the stage and the target.</summary>
    public IReadOnlyList<string> KeyColumns { get; }

    /// <summary>Gets the rows the load appended to the stage under <see cref="StageKey"/>; the sweep refuses when the stage holds any other number.</summary>
    public long ExpectedRows { get; }
}
