using System.Collections.Generic;

namespace Fdw.Services.Calculations.Abstractions.Sets;

/// <summary>
/// One row an output is built for: the values its formulas can read, and, when it stands for a group, the table and rows the
/// group's totals are taken over.
/// </summary>
public sealed class DriverRow
{
    /// <summary>Initializes a new instance of the <see cref="DriverRow"/> class.</summary>
    /// <param name="values">The values formulas read by name.</param>
    /// <param name="scopeTable">The table the totals are scoped to, or null when they are over the whole table.</param>
    /// <param name="scopeRows">The rows of that table this driver covers, or null.</param>
    public DriverRow(IReadOnlyDictionary<string, object?> values, string? scopeTable = null, IReadOnlyList<IReadOnlyDictionary<string, object?>>? scopeRows = null)
    {
        Values = values;
        ScopeTable = scopeTable;
        ScopeRows = scopeRows;
    }

    /// <summary>Gets the values formulas read by name.</summary>
    public IReadOnlyDictionary<string, object?> Values { get; }

    /// <summary>Gets the table the totals are scoped to, or null.</summary>
    public string? ScopeTable { get; }

    /// <summary>Gets the rows of the scope table this driver covers, or null.</summary>
    public IReadOnlyList<IReadOnlyDictionary<string, object?>>? ScopeRows { get; }
}
