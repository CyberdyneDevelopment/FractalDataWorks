using System.Collections.Generic;
using System.Linq;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>The And logic of a gate: true when every input line is true.</summary>
[TypeOption(typeof(GateLogics), "And")]
public sealed class AndGateLogic : GateLogicBase
{
    /// <summary>Initializes a new instance of the <see cref="AndGateLogic"/> class.</summary>
    public AndGateLogic() : base("And", "True when every input line is true.", 2, null)
    {
    }

    /// <inheritdoc />
    public override bool Judge(IReadOnlyList<bool> inputs) => inputs.Count > 0 && inputs.All(input => input);
}
