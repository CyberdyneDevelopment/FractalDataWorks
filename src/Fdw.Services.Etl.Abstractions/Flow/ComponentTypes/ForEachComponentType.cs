using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>Runs the components inside it once for each element of a list held in a variable.</summary>
[TypeOption(typeof(ComponentTypes), "ForEach")]
public sealed class ForEachComponentType : ComponentTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="ForEachComponentType"/> class.</summary>
    public ForEachComponentType() : base(
        "ForEach",
        "For each",
        "Runs the components inside it once for each element of a list held in a variable.",
        "Control flow",
        true,
        [
            new StageMember("variable", "Variable", true, "The element's name; declared for each iteration."),
            new StageMember("list", "Variable", true, "The variable that holds the list."),
        ])
    {
    }
}
