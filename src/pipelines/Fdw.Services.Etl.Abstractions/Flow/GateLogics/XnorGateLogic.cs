using System.Collections.Generic;
using System.Linq;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>The Xnor logic of a gate: true when an even number of input lines are true.</summary>
[TypeOption(typeof(GateLogics), "Xnor")]
public sealed class XnorGateLogic : GateLogicBase
{
    /// <summary>Initializes a new instance of the <see cref="XnorGateLogic"/> class.</summary>
    public XnorGateLogic() : base("Xnor", "True when an even number of input lines are true.", 2, null)
    {
    }

    /// <inheritdoc />
    public override bool Judge(IReadOnlyList<bool> inputs) => inputs.Count(input => input) % 2 == 0;
}
