using Fdw.Collections;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Logical operator for combining filter conditions (AND / OR).
/// Uses TypeOption pattern - simple enough for only 2 values.
/// </summary>
/// <remarks>
/// <para>
/// This replaces a traditional enum but adds properties for SQL and OData representations,
/// eliminating the need for switch statements.
/// </para>
/// <para>
/// Usage:
/// <code>
/// var logicalOp = LogicalOperator.And;
/// var sqlJoin = logicalOp.SqlOperator;     // "AND"
/// var odataJoin = logicalOp.ODataOperator; // "and"
/// </code>
/// </para>
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public abstract class LogicalOperator : TypeOptionBase<LogicalOperator>, ILogicalOperator
{
    /// <summary>
    /// AND logical operator.
    /// </summary>
    public static readonly LogicalOperator And = new AndLogicalOperator();

    /// <summary>
    /// OR logical operator.
    /// </summary>
    public static readonly LogicalOperator Or = new OrLogicalOperator();

    /// <summary>
    /// Initializes a logical operator. Each operator is its own class because an option's id derives
    /// from its type: two operators sharing one class would share one id and compare equal.
    /// </summary>
    /// <param name="name">The operator name.</param>
    /// <param name="sqlOperator">The SQL representation.</param>
    /// <param name="odataOperator">The OData representation.</param>
    private protected LogicalOperator(string name, string sqlOperator, string odataOperator)
        : base(name)
    {
        SqlOperator = sqlOperator;
        ODataOperator = odataOperator;
    }

    /// <summary>
    /// Gets the SQL representation (AND / OR).
    /// No switch statements needed - direct property access!
    /// </summary>
    public string SqlOperator { get; }

    /// <summary>
    /// Gets the OData representation (and / or).
    /// No switch statements needed - direct property access!
    /// </summary>
    public string ODataOperator { get; }
}
