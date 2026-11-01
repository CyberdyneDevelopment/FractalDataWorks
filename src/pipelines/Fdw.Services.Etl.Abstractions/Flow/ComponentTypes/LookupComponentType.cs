using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>Adds columns from a reference input to each row that has a matching key.</summary>
[TypeOption(typeof(ComponentTypes), "Lookup")]
public sealed class LookupComponentType : ComponentTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="LookupComponentType"/> class.</summary>
    public LookupComponentType() : base(
        "Lookup",
        "Lookup",
        "Adds columns from a reference input to each row that has a matching key.",
        "Combine",
        [
            new FlowNodeSpec("input", false, 1, 1, false, "The rows to add columns to."),
            new FlowNodeSpec("reference", false, 1, 1, false, "The reference rows, held in memory; a key must not repeat."),
        ],
        [
            new FlowNodeSpec("output", false, 1, 1, false, "The rows the component produces."),
            new FlowNodeSpec("unmatched", true, 0, 1, false, "Rows with no reference row; named by the unmatchedOutput setting.", "unmatchedOutput"),
        ],
        [
            new StageMember("keys", "ColumnList", true, "The key fields of the main rows."),
            new StageMember("referenceKeys", "ColumnList", true, "The key fields of the reference rows, in the same order and number."),
            new StageMember("bring", "FieldMaps", true, "The columns brought across, each from a field of the reference row."),
            new StageMember("unmatchedOutput", "Text", false, "The name of the output that takes rows with no reference row; absent, such a row fails the run.", ShapesGraph: true),
            new StageMember("batchSize", "Number", true, "How many rows go in a batch; at least one."),
        ])
    {
    }
}
