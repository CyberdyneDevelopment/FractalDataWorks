using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Fdw.Collections.Attributes;
using Fdw.Services.Calculations.Abstractions.Runtime;

namespace Fdw.Services.Calculations.Abstractions.CalculationNodeOptions;

/// <summary>
/// The operand, which must have a value. An empty operand fails with RequiredValueEmpty.
/// </summary>
[TypeOption(typeof(CalculationNodeTypes), "Require", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class RequireNode : CalculationNodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequireNode"/> class.
    /// </summary>
    public RequireNode()
        : base("Require", "The operand, which must have a value; an empty operand fails with RequiredValueEmpty.", 1, 1)
    {
    }

    /// <inheritdoc />
    public override Type? OperandType(int index) => typeof(decimal?);

    /// <inheritdoc />
    public override Expression Emit(NodeEmission emission) =>
        NodeEmission.Call(nameof(CalculationRuntime.Required), emission.Operands[0], emission.State, emission.Index);
}
