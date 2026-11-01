using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>Runs the components inside it once for each value of a counter from a start to an end.</summary>
[TypeOption(typeof(ComponentTypes), "ForLoop")]
public sealed class ForLoopComponentType : ComponentTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="ForLoopComponentType"/> class.</summary>
    public ForLoopComponentType() : base(
        "ForLoop",
        "For loop",
        "Runs the components inside it once for each value of a counter from a start to an end.",
        "Control flow",
        true,
        [
            new StageMember("variable", "Variable", true, "The counter's name; declared for each iteration."),
            new StageMember("start", "Number", true, "The first value."),
            new StageMember("end", "Number", true, "The last value, inclusive."),
            new StageMember("step", "Number", true, "The increment; not zero."),
        ])
    {
    }
}
