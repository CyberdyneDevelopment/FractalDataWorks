using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Schema;

/// <summary>
/// Lookup role - indexed for search but not part of key.
/// </summary>
/// <remarks>
/// <para>
/// Lookup properties are frequently used in queries and filters but don't uniquely identify records.
/// Examples include status codes, category names, or foreign key references.
/// </para>
/// <para>
/// Characteristics:
/// <list type="bullet">
/// <item>IsKeyRole: false - Not a unique identifier</item>
/// <item>IsIndexable: true - Frequently used in WHERE clauses</item>
/// <item>IsAggregatable: false - Not numeric data for aggregation</item>
/// </list>
/// </para>
/// </remarks>
[TypeOption(typeof(PropertyRoles), "Lookup")]
[ExcludeFromCodeCoverage]
public sealed class LookupRole : PropertyRoleBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LookupRole"/> class.
    /// </summary>
    public LookupRole()
        : base(
            id: 3,
            name: "Lookup",
            description: "Indexed for search but not part of key",
            isKeyRole: false,
            isIndexable: true,
            isAggregatable: false)
    {
    }
}
