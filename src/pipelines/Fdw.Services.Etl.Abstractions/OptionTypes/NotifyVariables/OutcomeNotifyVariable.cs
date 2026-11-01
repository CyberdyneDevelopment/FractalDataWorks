using System.Globalization;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>How the run stands at the run point: Succeeded, or Failed at OnFailure</summary>
[TypeOption(typeof(NotifyVariables), "Outcome")]
public sealed class OutcomeNotifyVariable : NotifyVariableBase
{
    /// <summary>Initializes a new instance of the <see cref="OutcomeNotifyVariable"/> class.</summary>
    public OutcomeNotifyVariable() : base("Outcome", "Outcome", "How the run stands at the run point: Succeeded, or Failed at OnFailure")
    {
    }

    /// <inheritdoc />
    public override string ValueOf(INotifyRunFacts facts) => facts.Outcome;
}
