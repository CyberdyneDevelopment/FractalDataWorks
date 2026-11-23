using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>Judges its test, then runs the components inside it, and again while the test holds, up to a stated most; the body may run no time (FDW-6134).</summary>
[TypeOption(typeof(ComponentTypes), "While")]
public sealed class WhileComponentType : ComponentTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="WhileComponentType"/> class.</summary>
    public WhileComponentType() : base(
        "While",
        "While",
        "Judges its test, then runs the components inside it, and again while the test holds, up to a stated most. The components inside it may run no time.",
        "Control flow",
        true,
        [
            new StageMember("condition", "Text", false, "The test as a condition over the variables, in the filter expression language. State this or testOutput, not both."),
            new StageMember(DoWhileComponentType.TestOutputSetting, "Text", false, DoWhileComponentType.TestOutputHelp),
            new StageMember("maximum", "Number", true, "The most times the components may run; the run fails when it is exceeded."),
        ])
    {
    }
}
