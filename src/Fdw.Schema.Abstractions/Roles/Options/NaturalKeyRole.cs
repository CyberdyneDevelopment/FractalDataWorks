using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Schema;

/// <summary>
/// Natural key role - business identifier that is human-meaningful.
/// </summary>
/// <remarks>
/// <para>
/// Natural keys are business identifiers that have real-world meaning, such as email addresses,
/// SKU codes, or social security numbers. They serve as both identifiers and data.
/// </para>
/// <para>
/// Characteristics:
/// <list type="bullet">
/// <item>IsKeyRole: true - This is a unique identifier</item>
/// <item>IsIndexable: true - Natural keys require indexes for lookups</item>
/// <item>IsAggregatable: false - Keys should not be summed or averaged</item>
/// </list>
/// </para>
/// </remarks>
[TypeOption(typeof(PropertyRoles), "NaturalKey")]
[ExcludeFromCodeCoverage]
public sealed class NaturalKeyRole : PropertyRoleBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NaturalKeyRole"/> class.
    /// </summary>
    public NaturalKeyRole()
        : base(
            id: 2,
            name: "NaturalKey",
            description: "Business identifier that is human-meaningful",
            isKeyRole: true,
            isIndexable: true,
            isAggregatable: false)
    {
    }
}
