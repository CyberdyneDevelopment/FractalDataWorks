using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Schema;

/// <summary>
/// Attribute role - descriptive data that is not indexed.
/// </summary>
/// <remarks>
/// <para>
/// Attribute properties contain descriptive information that is rarely queried directly,
/// such as comments, descriptions, or large text fields. They don't require indexing.
/// </para>
/// <para>
/// Characteristics:
/// <list type="bullet">
/// <item>IsKeyRole: false - Not a unique identifier</item>
/// <item>IsIndexable: false - Rarely used in WHERE clauses</item>
/// <item>IsAggregatable: false - Not numeric data for aggregation</item>
/// </list>
/// </para>
/// </remarks>
[TypeOption(typeof(PropertyRoles), "Attribute")]
[ExcludeFromCodeCoverage]
public sealed class AttributeRole : PropertyRoleBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AttributeRole"/> class.
    /// </summary>
    public AttributeRole()
        : base(
            id: 4,
            name: "Attribute",
            description: "Descriptive data that is not indexed",
            isKeyRole: false,
            isIndexable: false,
            isAggregatable: false)
    {
    }
}
