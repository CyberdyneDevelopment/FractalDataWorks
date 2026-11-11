using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.Trees;

namespace Fdw.Services.Calculations.Abstractions.CalculationNodeOptions;

/// <summary>
/// The value of an input field of the record: a possibly empty exact number, or, where a condition is needed, a flag, or, where a date
/// is needed, a date.
/// </summary>
[TypeOption(typeof(CalculationNodeTypes), "Field", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class FieldNode : CalculationNodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FieldNode"/> class.
    /// </summary>
    public FieldNode()
        : base("Field", "The value of an input field of the record.", 0, 0)
    {
    }

    /// <inheritdoc />
    public override IReadOnlyList<string> Members { get; } = new[] { "name" };

    /// <inheritdoc />
    public override Type ResultType(CalculationNode node, Type expected, IReadOnlyList<Type> operandTypes) => SlotType(expected);

    /// <inheritdoc />
    public override IGenericResult Check(CalculationNode node, ICalculationScope scope)
    {
        if (node.Name is null || string.IsNullOrWhiteSpace(node.Name))
        {
            return Invalid(node, "a Field needs the name of the input field");
        }

        scope.Reads(node.Name);
        return GenericResult.Success();
    }

    // The parent decides how the field is read: a condition reads a flag, a date reads a date, anything else a possibly empty number.
    private static Type SlotType(Type expected) =>
        expected == typeof(bool) || expected == typeof(DateOnly) ? expected : typeof(decimal?);

    /// <inheritdoc />
    public override Expression Emit(NodeEmission emission) =>
        emission.Node.Name is { } name
            ? emission.Slot(name, SlotType(emission.Expected))
            : throw new InvalidOperationException("A Field node without a name reached Emit; Check refuses it");
}
