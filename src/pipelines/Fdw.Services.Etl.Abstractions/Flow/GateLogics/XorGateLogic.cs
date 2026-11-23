using System.Collections.Generic;
using System.Linq;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>The Xor logic of a gate: true when an odd number of input lines are true.</summary>
[TypeOption(typeof(GateLogics), "Xor")]
public sealed class XorGateLogic : GateLogicBase
{
    /// <summary>Initializes a new instance of the <see cref="XorGateLogic"/> class.</summary>
    public XorGateLogic() : base("Xor", "True when an odd number of input lines are true.", 2, null)
    {
    }

    /// <inheritdoc />
    public override bool Judge(IReadOnlyList<bool> inputs) => inputs.Count(input => input) % 2 == 1;
}
