using System.Collections.Generic;
using System.Linq;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>The Not logic of a gate: true when its one input line is false.</summary>
[TypeOption(typeof(GateLogics), "Not")]
public sealed class NotGateLogic : GateLogicBase
{
    /// <summary>Initializes a new instance of the <see cref="NotGateLogic"/> class.</summary>
    public NotGateLogic() : base("Not", "True when its one input line is false.", 1, 1)
    {
    }

    /// <inheritdoc />
    public override bool Judge(IReadOnlyList<bool> inputs) => !inputs[0];
}
