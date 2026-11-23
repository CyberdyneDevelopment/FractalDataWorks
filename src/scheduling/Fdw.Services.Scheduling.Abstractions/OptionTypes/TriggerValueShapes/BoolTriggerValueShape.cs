using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Scheduling.Abstractions.OptionTypes;

/// <summary>A bool, as it is.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(TriggerValueShapes), "Bool")]
public sealed class BoolTriggerValueShape : TriggerValueShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="BoolTriggerValueShape"/> class.</summary>
    public BoolTriggerValueShape()
        : base("Bool")
    {
    }

    /// <inheritdoc />
    public override bool Matches(object value) => value is bool;

    /// <inheritdoc />
    public override bool TryBool(object value, out bool result)
    {
        result = (bool)value;
        return true;
    }
}
