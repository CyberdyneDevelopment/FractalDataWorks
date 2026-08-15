using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>
/// The visibilities a container field can carry.
/// </summary>
/// <remarks>
/// Read from <c>data.DataContainerField.VisibilityId</c> via <c>ByName</c>, the same way
/// <c>data.DataContainerKey.TypeId</c> resolves a key type.
/// </remarks>
[TypeCollection(typeof(FieldVisibilityBase), typeof(IFieldVisibility), typeof(FieldVisibilities))]
public abstract partial class FieldVisibilities : TypeCollectionBase<FieldVisibilityBase, IFieldVisibility>
{
}
