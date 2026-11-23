using System;
using System.Collections.Generic;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>Combines the lines that feed it by a logic (And, Or, Not, Nand, Nor, Xor, Xnor) and fires its <c>true</c> or its <c>false</c> output (FDW-6134).</summary>
[TypeOption(typeof(ComponentTypes), "Gate")]
public sealed class GateComponentType : ComponentTypeBase, IDecisionComponentType
{
    /// <summary>The name of the setting that states the gate's logic.</summary>
    public const string LogicSetting = "logic";

    /// <summary>Initializes a new instance of the <see cref="GateComponentType"/> class.</summary>
    public GateComponentType() : base(
        "Gate",
        "Gate",
        "Combines the output lines of conditions and other gates by a logic and fires its true or its false output. A line that did not fire is false.",
        "Control flow",
        false,
        [
            new StageMember(LogicSetting, "Text", true, "The logic: And, Or, Not, Nand, Nor, Xor or Xnor. Not takes one line, every other logic at least two."),
        ])
    {
    }

    /// <inheritdoc />
    public IReadOnlyList<string> FiredOutputs(Func<string, string?> stated) => [ConditionComponentType.True, ConditionComponentType.False];
}
