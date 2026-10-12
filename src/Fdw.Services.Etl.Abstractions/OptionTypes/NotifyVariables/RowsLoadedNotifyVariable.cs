using System.Globalization;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>The variable <c>{{RowsLoaded}}</c>: The records loaded so far.</summary>
[TypeOption(typeof(NotifyVariables), "RowsLoaded")]
public sealed class RowsLoadedNotifyVariable : NotifyVariableBase
{
    /// <summary>Initializes a new instance of the <see cref="RowsLoadedNotifyVariable"/> class.</summary>
    public RowsLoadedNotifyVariable() : base("RowsLoaded", "Rows loaded", "The records loaded so far")
    {
    }

    /// <inheritdoc/>
    public override string ValueOf(INotifyRunFacts facts) => facts.RowsLoaded.ToString(CultureInfo.InvariantCulture);
}
