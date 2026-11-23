using System.Collections.Generic;
using System.Linq;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>The Nand logic of a gate: false only when every input line is true.</summary>
[TypeOption(typeof(GateLogics), "Nand")]
public sealed class NandGateLogic : GateLogicBase
{
    /// <summary>Initializes a new instance of the <see cref="NandGateLogic"/> class.</summary>
    public NandGateLogic() : base("Nand", "False only when every input line is true.", 2, null)
    {
    }

    /// <inheritdoc />
    public override bool Judge(IReadOnlyList<bool> inputs) => !(inputs.Count > 0 && inputs.All(input => input));
}
