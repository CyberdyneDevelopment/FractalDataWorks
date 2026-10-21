using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>Sends each row down the output whose filter it meets; a row may go to the first match or to every match.</summary>
[TypeOption(typeof(ComponentTypes), "ConditionalSplit")]
public sealed class ConditionalSplitComponentType : ComponentTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="ConditionalSplitComponentType"/> class.</summary>
    public ConditionalSplitComponentType() : base(
        "ConditionalSplit",
        "Conditional split",
        "Sends each row down the output whose filter it meets; a row may go to the first match or to every match.",
        "Split",
        [
            new FlowNodeSpec("input", false, 1, 1, false, "The rows the component works on."),
        ],
        [
            new FlowNodeSpec("output", true, 1, null, false, "Rows that meet this output's filter.", "outputs"),
            new FlowNodeSpec("unmatched", true, 0, 1, false, "Rows that meet no filter; named by the unmatchedOutput setting.", "unmatchedOutput"),
        ],
        [
            new StageMember("outputs", "SplitOutputs", true, "The filtered outputs, in the order they are tried: a name and a filter expression each."),
            new StageMember("routing", "SplitRoutings", true, "Where a row that meets several filters goes."),
            new StageMember("unmatchedOutput", "Text", false, "The name of the output that takes rows meeting no filter; absent, such a row fails the run.", ShapesGraph: true),
        ])
    {
    }
}
