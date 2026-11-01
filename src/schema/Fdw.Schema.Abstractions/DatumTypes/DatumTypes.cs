using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Schema;

/// <summary>
/// The datum types: what a column's data is (FDW-988). Replaces PropertyRoles, whose five options map
/// onto these: Surrogate is DbKey, NaturalKey is Identifier, Lookup is Categorical, and Attribute and Measure
/// keep their names.
/// </summary>
/// <remarks>
/// A column's datum type is stored by its Id (the FNV-1a hash of the option's fully qualified type name)
/// in <c>DatumTypeId</c> on data set fields and data store fields, and every type has a row in
/// <c>data.DatumType</c>. A column nobody has typed carries <see cref="NotYetTypedDatumType"/>.
/// <para>
/// No option may take the name of a member every type option inherits (Id, Name, Category,
/// ConfigurationKey, DisplayName, Description): the generated collection exposes each option as a static
/// property of that name, which would hide the inherited member (CS0108). Hence Categorical and
/// Descriptive.
/// </para>
/// </remarks>
[TypeCollection(typeof(DatumTypeBase), typeof(IDatumType), typeof(DatumTypes))]
[ExcludeFromCodeCoverage]
public abstract partial class DatumTypes : TypeCollectionBase<DatumTypeBase, IDatumType>
{
}
