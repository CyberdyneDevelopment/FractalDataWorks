using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>Runs the components inside it, then again while a condition over the variables holds, up to a stated most.</summary>
[TypeOption(typeof(ComponentTypes), "DoWhile")]
public sealed class DoWhileComponentType : ComponentTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="DoWhileComponentType"/> class.</summary>
    public DoWhileComponentType() : base(
        "DoWhile",
        "Do while",
        "Runs the components inside it, then again while a condition over the variables holds, up to a stated most.",
        "Control flow",
        true,
        [
            new StageMember("condition", "Text", true, "True to run the components again; judged over the variables in scope after each run, in the filter expression language."),
            new StageMember("maximum", "Number", true, "The most times the components may run; the run fails when it is exceeded."),
        ])
    {
    }
}
