using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>Joins two inputs on key fields; the right input is held in memory, so put the smaller one there.</summary>
[TypeOption(typeof(ComponentTypes), "MergeJoin")]
public sealed class MergeJoinComponentType : ComponentTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="MergeJoinComponentType"/> class.</summary>
    public MergeJoinComponentType() : base(
        "MergeJoin",
        "Merge join",
        "Joins two inputs on key fields; the right input is held in memory, so put the smaller one there.",
        "Combine",
        [
            new FlowNodeSpec("left", false, 1, 1, false, "The rows kept as they are; matched against the right input."),
            new FlowNodeSpec("right", false, 1, 1, false, "The rows looked up by key; held in memory."),
        ],
        [
            new FlowNodeSpec("output", false, 1, 1, false, "The rows the component produces."),
        ],
        [
            new StageMember("leftKeys", "ColumnList", true, "The key fields of the left rows."),
            new StageMember("rightKeys", "ColumnList", true, "The key fields of the right rows, in the same order and number."),
            new StageMember("joinKind", "JoinKinds", true, "Which rows with no partner are kept."),
            new StageMember("rightPrefix", "Text", true, "What is put before each right field name in the output; empty only when no name collides."),
            new StageMember("batchSize", "Number", true, "How many rows go in a batch; at least one."),
        ])
    {
    }
}
