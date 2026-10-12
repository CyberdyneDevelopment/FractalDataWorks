using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// Named default-value policies for MsSql connections. Child collection of
/// <see cref="DefaultValues"/> — resolve as <c>DefaultValues.ByName("MsSql").ByName(policyName)</c>.
/// </summary>
/// <remarks>
/// The framework ships <c>"Default"</c>. An organization adds its own named policy by adding a
/// <c>[TypeOption(typeof(MsSqlDefaults), "OwnPolicyName")]</c> against this collection from its own
/// assembly — no change here is required.
/// </remarks>
[TypeCollection(typeof(DefaultValuePolicyBase), typeof(IDefaultValuePolicy), typeof(MsSqlDefaults),
    TypeOption = typeof(DefaultValues), TypeOptionName = "MsSql")]
public abstract partial class MsSqlDefaults : TypeCollectionBase<DefaultValuePolicyBase, IDefaultValuePolicy>
{
}
