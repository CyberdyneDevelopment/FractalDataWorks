namespace Fdw.Data.Abstractions;

/// <summary>
/// The AND logical operator. Combines conditions so that every one must hold.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class AndLogicalOperator : LogicalOperator
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AndLogicalOperator"/> class.
    /// </summary>
    public AndLogicalOperator() : base("And", "AND", "and")
    {
    }
}
