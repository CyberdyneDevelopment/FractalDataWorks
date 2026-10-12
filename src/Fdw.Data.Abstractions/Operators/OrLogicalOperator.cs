namespace Fdw.Data.Abstractions;

/// <summary>
/// The OR logical operator. Combines conditions so that any one may hold.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class OrLogicalOperator : LogicalOperator
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OrLogicalOperator"/> class.
    /// </summary>
    public OrLogicalOperator() : base("Or", "OR", "or")
    {
    }
}
