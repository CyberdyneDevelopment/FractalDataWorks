using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Schema;

/// <summary>
/// MutableTypeCollection for property roles.
/// Source generator will create static properties for each role with [TypeOption] attribute.
/// </summary>
/// <remarks>
/// <para>
/// This collection provides compile-time discovery of all property role types.
/// No switch statements needed - roles know their own characteristics!
/// </para>
/// <para>
/// Example generated properties:
/// <list type="bullet">
/// <item>PropertyRoles.Surrogate - Auto-generated key with no business meaning</item>
/// <item>PropertyRoles.NaturalKey - Business identifier, human-meaningful</item>
/// <item>PropertyRoles.Lookup - Indexed for search, not part of key</item>
/// <item>PropertyRoles.Attribute - Descriptive, non-indexed</item>
/// <item>PropertyRoles.Measure - Aggregatable numeric</item>
/// </list>
/// </para>
/// <para>
/// Usage eliminates switch statements:
/// <code>
/// var property = new PropertyDefinition {
///     Name = "Id",
///     Role = PropertyRoles.Surrogate,  // Type-safe!
///     DataType = DataTypes.Integer
/// };
///
/// // No switch - just property access!
/// if (property.Role.IsKeyRole) {
///     GeneratePrimaryKeyConstraint(property);
/// }
/// if (property.Role.IsIndexable) {
///     GenerateIndex(property);
/// }
/// </code>
/// </para>
/// </remarks>
[TypeCollection(typeof(PropertyRoleBase), typeof(IPropertyRole), typeof(PropertyRoles))]
[ExcludeFromCodeCoverage]
public abstract partial class PropertyRoles : TypeCollectionBase<PropertyRoleBase, IPropertyRole>
{
    // Source generator will create:
    // - Static constructor
    // - Static properties for each [TypeOption] role
    // - All() method
    // - ById() method
    // - ByName() method
    // - Register() method (mutable)
    // - Unregister() method (mutable)
}
