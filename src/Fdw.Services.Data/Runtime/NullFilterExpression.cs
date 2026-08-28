using Fdw.Data.Abstractions;

namespace Fdw.Services.Data.Runtime;

/// <summary>
/// A no-op <see cref="IFilterExpression"/> with a null root node, used as a placeholder
/// when a join condition cannot be built without a resolved field reference.
/// </summary>
/// <remarks>
/// The DataGatewayService reads join field names directly from <c>JoinConfiguration</c>
/// at query execution time rather than from the <see cref="IDataSetJoin.Condition"/> expression,
/// so this sentinel value is never translated to SQL. It satisfies the non-null contract
/// on <see cref="IDataSetJoin.Condition"/> without hiding the missing configuration.
/// </remarks>
internal sealed class NullFilterExpression : IFilterExpression
{
    /// <summary>Gets the singleton instance.</summary>
    public static readonly NullFilterExpression Instance = new();

    private NullFilterExpression() { }

    /// <inheritdoc />
    public IFilterNode? Root => null;
}
