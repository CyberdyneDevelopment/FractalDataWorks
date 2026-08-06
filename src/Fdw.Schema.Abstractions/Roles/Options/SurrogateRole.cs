using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Schema;

/// <summary>
/// Surrogate key role - auto-generated key with no business meaning.
/// </summary>
/// <remarks>
/// <para>
/// Surrogate keys are system-generated identifiers (typically auto-increment integers or GUIDs)
/// that have no business meaning. They exist solely for database performance and referential integrity.
/// </para>
/// <para>
/// Characteristics:
/// <list type="bullet">
/// <item>IsKeyRole: true - This is a primary key</item>
/// <item>IsIndexable: true - Primary keys are always indexed</item>
/// <item>IsAggregatable: false - Keys should not be summed or averaged</item>
/// </list>
/// </para>
/// </remarks>
[TypeOption(typeof(PropertyRoles), "Surrogate")]
[ExcludeFromCodeCoverage]
public sealed class SurrogateRole : PropertyRoleBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SurrogateRole"/> class.
    /// </summary>
    public SurrogateRole()
        : base(
            id: 1,
            name: "Surrogate",
            description: "Auto-generated key with no business meaning",
            isKeyRole: true,
            isIndexable: true,
            isAggregatable: false)
    {
    }
}
