using System.Globalization;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>When the run point fired, in UTC (ISO 8601): the end of the run as far as the run point can know it</summary>
[TypeOption(typeof(NotifyVariables), "EndedAt")]
public sealed class EndedAtNotifyVariable : NotifyVariableBase
{
    /// <summary>Initializes a new instance of the <see cref="EndedAtNotifyVariable"/> class.</summary>
    public EndedAtNotifyVariable() : base("EndedAt", "Ended at", "When the run point fired, in UTC (ISO 8601): the end of the run as far as the run point can know it")
    {
    }

    /// <inheritdoc />
    public override string ValueOf(INotifyRunFacts facts) => facts.EndedAt.ToString("o", CultureInfo.InvariantCulture);
}
