using System.Collections.Generic;
using System.Linq;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>The Or logic of a gate: true when at least one input line is true.</summary>
[TypeOption(typeof(GateLogics), "Or")]
public sealed class OrGateLogic : GateLogicBase
{
    /// <summary>Initializes a new instance of the <see cref="OrGateLogic"/> class.</summary>
    public OrGateLogic() : base("Or", "True when at least one input line is true.", 2, null)
    {
    }

    /// <inheritdoc />
    public override bool Judge(IReadOnlyList<bool> inputs) => inputs.Any(input => input);
}
