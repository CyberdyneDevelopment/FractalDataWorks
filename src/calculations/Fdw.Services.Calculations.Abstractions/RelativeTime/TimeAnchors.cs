using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions.RelativeTime;

/// <summary>
/// TypeCollection of the anchors the current moment can be cut back to. A developer adds an anchor by adding an option.
/// </summary>
[TypeCollection(typeof(TimeAnchorBase), typeof(ITimeAnchor), typeof(TimeAnchors))]
[ExcludeFromCodeCoverage]
public abstract partial class TimeAnchors : TypeCollectionBase<TimeAnchorBase, ITimeAnchor>
{
}
