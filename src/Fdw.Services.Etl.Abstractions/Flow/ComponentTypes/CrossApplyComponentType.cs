using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>Turns a list-valued or delimited field into one row per value.</summary>
[TypeOption(typeof(ComponentTypes), "CrossApply")]
public sealed class CrossApplyComponentType : ComponentTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="CrossApplyComponentType"/> class.</summary>
    public CrossApplyComponentType() : base(
        "CrossApply",
        "Cross apply",
        "Turns a list-valued or delimited field into one row per value.",
        "Combine",
        [
            new FlowNodeSpec("input", false, 1, 1, false, "The rows the component works on."),
        ],
        [
            new FlowNodeSpec("output", false, 1, 1, false, "The rows the component produces."),
        ],
        [
            new StageMember("field", "Text", true, "The field that holds the list or the delimited text."),
            new StageMember("target", "Text", true, "The output field that takes each value."),
            new StageMember("delimiter", "Text", false, "The delimiter, when the field holds delimited text; absent, the field holds a list."),
            new StageMember("keepsEmpty", "Boolean", true, "Whether a row whose field yields no value is kept, with the target null."),
        ])
    {
    }
}
