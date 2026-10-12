using System.Globalization;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>The variable <c>{{RowsExtracted}}</c>: The records read from the source so far.</summary>
[TypeOption(typeof(NotifyVariables), "RowsExtracted")]
public sealed class RowsExtractedNotifyVariable : NotifyVariableBase
{
    /// <summary>Initializes a new instance of the <see cref="RowsExtractedNotifyVariable"/> class.</summary>
    public RowsExtractedNotifyVariable() : base("RowsExtracted", "Rows extracted", "The records read from the source so far")
    {
    }

    /// <inheritdoc/>
    public override string ValueOf(INotifyRunFacts facts) => facts.RowsExtracted.ToString(CultureInfo.InvariantCulture);
}
