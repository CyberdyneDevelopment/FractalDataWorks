using System.Globalization;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>The variable <c>{{RowsRejected}}</c>: The records rejected so far, by a Validate or Branch stage or a failing step.</summary>
[TypeOption(typeof(NotifyVariables), "RowsRejected")]
public sealed class RowsRejectedNotifyVariable : NotifyVariableBase
{
    /// <summary>Initializes a new instance of the <see cref="RowsRejectedNotifyVariable"/> class.</summary>
    public RowsRejectedNotifyVariable() : base("RowsRejected", "Rows rejected", "The records rejected so far, by a Validate or Branch stage or a failing step")
    {
    }

    /// <inheritdoc/>
    public override string ValueOf(INotifyRunFacts facts) => facts.RowsRejected.ToString(CultureInfo.InvariantCulture);
}
