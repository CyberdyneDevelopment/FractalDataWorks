using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Schema;

/// <summary>
/// Measure role - aggregatable numeric data.
/// </summary>
/// <remarks>
/// <para>
/// Measure properties contain numeric values that are meaningful to aggregate using functions
/// like SUM, AVG, MIN, or MAX. Examples include quantities, prices, totals, or metrics.
/// </para>
/// <para>
/// Characteristics:
/// <list type="bullet">
/// <item>IsKeyRole: false - Not a unique identifier</item>
/// <item>IsIndexable: false - Typically not used in WHERE clauses</item>
/// <item>IsAggregatable: true - Can be summed, averaged, etc.</item>
/// </list>
/// </para>
/// </remarks>
[TypeOption(typeof(PropertyRoles), "Measure")]
[ExcludeFromCodeCoverage]
public sealed class MeasureRole : PropertyRoleBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MeasureRole"/> class.
    /// </summary>
    public MeasureRole()
        : base(
            id: 5,
            name: "Measure",
            description: "Aggregatable numeric data",
            isKeyRole: false,
            isIndexable: false,
            isAggregatable: true)
    {
    }
}
