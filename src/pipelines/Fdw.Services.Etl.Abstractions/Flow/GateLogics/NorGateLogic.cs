using System.Collections.Generic;
using System.Linq;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>The Nor logic of a gate: true only when no input line is true.</summary>
[TypeOption(typeof(GateLogics), "Nor")]
public sealed class NorGateLogic : GateLogicBase
{
    /// <summary>Initializes a new instance of the <see cref="NorGateLogic"/> class.</summary>
    public NorGateLogic() : base("Nor", "True only when no input line is true.", 2, null)
    {
    }

    /// <inheritdoc />
    public override bool Judge(IReadOnlyList<bool> inputs) => !inputs.Any(input => input);
}
