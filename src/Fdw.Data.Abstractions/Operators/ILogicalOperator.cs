using Fdw.Collections;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Interface for logical operators (AND / OR).
/// </summary>
public interface ILogicalOperator : ITypeOption<int, LogicalOperator>
{
    /// <summary>
    /// Gets the SQL representation (AND / OR).
    /// </summary>
    string SqlOperator { get; }

    /// <summary>
    /// Gets the OData representation (and / or).
    /// </summary>
    string ODataOperator { get; }
}
