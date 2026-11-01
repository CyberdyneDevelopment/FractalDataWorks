using System.Globalization;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>When the run started, in UTC (ISO 8601)</summary>
[TypeOption(typeof(NotifyVariables), "StartedAt")]
public sealed class StartedAtNotifyVariable : NotifyVariableBase
{
    /// <summary>Initializes a new instance of the <see cref="StartedAtNotifyVariable"/> class.</summary>
    public StartedAtNotifyVariable() : base("StartedAt", "Started at", "When the run started, in UTC (ISO 8601)")
    {
    }

    /// <inheritdoc />
    public override string ValueOf(INotifyRunFacts facts) => facts.StartedAt.ToString("o", CultureInfo.InvariantCulture);
}
