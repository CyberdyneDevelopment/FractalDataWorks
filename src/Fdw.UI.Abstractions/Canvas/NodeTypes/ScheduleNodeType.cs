using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Canvas.NodeTypes;

/// <summary>
/// A schedule trigger node that initiates pipeline execution on a time-based or event-based schedule.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(CanvasNodeTypes), "Schedule")]
public sealed class ScheduleNodeType : CanvasNodeTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScheduleNodeType"/> class.
    /// </summary>
    public ScheduleNodeType()
        : base(7, "Schedule", "Schedule", "Orchestration", "clock")
    {
    }
}
