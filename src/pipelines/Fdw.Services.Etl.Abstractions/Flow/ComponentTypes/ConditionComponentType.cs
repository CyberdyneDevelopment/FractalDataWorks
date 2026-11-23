using System;
using System.Collections.Generic;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>Judges one condition over the variables and fires its <c>true</c> or its <c>false</c> output (FDW-6134).</summary>
[TypeOption(typeof(ComponentTypes), "Condition")]
public sealed class ConditionComponentType : ComponentTypeBase, IDecisionComponentType
{
    /// <summary>The output that fires when the condition holds.</summary>
    public const string True = "true";

    /// <summary>The output that fires when it does not.</summary>
    public const string False = "false";

    /// <summary>Initializes a new instance of the <see cref="ConditionComponentType"/> class.</summary>
    public ConditionComponentType() : base(
        "Condition",
        "Condition",
        "Judges one condition over the variables and fires its true or its false output; the components its lines lead to run only when the output they come from fired.",
        "Control flow",
        false,
        [
            new StageMember("condition", "Text", true, "The condition, judged over the variables in scope when the component runs, in the filter expression language."),
        ])
    {
    }

    /// <inheritdoc />
    public IReadOnlyList<string> FiredOutputs(Func<string, string?> stated) => [True, False];
}
