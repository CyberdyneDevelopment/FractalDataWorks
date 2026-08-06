using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Etl.Abstractions.Monitoring.ComparisonOperatorOptions;

/// <summary>
/// Base class for comparison operators used in alert rules.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption base class - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
public abstract class ComparisonOperatorBase : TypeOptionBase<int, ComparisonOperatorBase>, IComparisonOperator
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ComparisonOperatorBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this comparison operator.</param>
    /// <param name="name">The name of this comparison operator.</param>
    /// <param name="sqlOperator">The SQL operator symbol.</param>
    protected ComparisonOperatorBase(int id, string name, string sqlOperator)
        : base(id, name)
    {
        SqlOperator = sqlOperator;
    }

    /// <inheritdoc />
    public string SqlOperator { get; }

    /// <inheritdoc />
    public abstract bool Evaluate(double left, double right);
}
