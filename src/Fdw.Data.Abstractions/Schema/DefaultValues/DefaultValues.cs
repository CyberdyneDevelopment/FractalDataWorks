using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Root collection of connection types participating in default-value write behavior.
/// </summary>
/// <remarks>
/// A member here is a dispatcher, not a policy: <c>DefaultValues.ByName("MsSql")</c> resolves to
/// that connection type's <see cref="IDefaultValueConnection"/>, and its own instance
/// <see cref="IDefaultValueConnection.ByName"/> resolves a policy name against that connection
/// type's own child collection (e.g. <c>MsSqlDefaults</c>). A connection type joins this collection
/// by adding a <c>[TypeOption(typeof(DefaultValues), "TypeName")]</c> against it from its own
/// package — nothing here references a specific connection type.
/// </remarks>
[TypeCollection(typeof(DefaultValueConnectionBase), typeof(IDefaultValueConnection), typeof(DefaultValues))]
public abstract partial class DefaultValues : TypeCollectionBase<DefaultValueConnectionBase, IDefaultValueConnection>
{
}
