using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Thresholds;

/// <summary>
/// The kinds of owner a wait threshold level can have, named as <c>WaitThresholdOwners</c> names them: <c>Server</c>, <c>Pipeline</c> and <c>Schedule</c>.
/// </summary>
/// <remarks>A stored level names its owner's kind by the option's name, matched exactly through <c>ByName</c>; a kind that matches none gets <c>NotFound</c>, whose owner the read does not look for.</remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(WaitThresholdOwnerKindBase), typeof(IWaitThresholdOwnerKind), typeof(WaitThresholdOwnerKinds))]
public abstract partial class WaitThresholdOwnerKinds : TypeCollectionBase<WaitThresholdOwnerKindBase, IWaitThresholdOwnerKind>
{
}
