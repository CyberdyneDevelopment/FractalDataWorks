using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>Runs the components inside it, then again while a condition over the variables holds, up to a stated most.</summary>
[TypeOption(typeof(ComponentTypes), "DoWhile")]
public sealed class DoWhileComponentType : ComponentTypeBase
{
    /// <summary>The name of the setting that takes a decision's output as the loop's test (FDW-6134).</summary>
    public const string TestOutputSetting = "testOutput";

    /// <summary>What the <c>testOutput</c> setting says, for a loop that tests before and one that tests after.</summary>
    public const string TestOutputHelp =
        "The output of a decision (a condition, a gate or a switch) inside this loop that is the test, written component.output, instead of a condition text. True to run again. State this or condition, not both.";

    /// <summary>Initializes a new instance of the <see cref="DoWhileComponentType"/> class.</summary>
    public DoWhileComponentType() : base(
        "DoWhile",
        "Do while",
        "Runs the components inside it, then again while a condition over the variables holds, up to a stated most.",
        "Control flow",
        true,
        [
            new StageMember("condition", "Text", false, "True to run the components again; judged over the variables in scope after each run, in the filter expression language. State this or testOutput, not both."),
            new StageMember(TestOutputSetting, "Text", false, TestOutputHelp),
            new StageMember("maximum", "Number", true, "The most times the components may run; the run fails when it is exceeded."),
        ])
    {
    }
}
