using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>The variable <c>{{ExecutionId}}</c>: The id of this execution of the pipeline.</summary>
[TypeOption(typeof(NotifyVariables), "ExecutionId")]
public sealed class ExecutionIdNotifyVariable : NotifyVariableBase
{
    /// <summary>Initializes a new instance of the <see cref="ExecutionIdNotifyVariable"/> class.</summary>
    public ExecutionIdNotifyVariable() : base("ExecutionId", "Execution id", "The id of this execution of the pipeline")
    {
    }

    /// <inheritdoc/>
    public override string ValueOf(INotifyRunFacts facts) => facts.ExecutionId.ToString();
}
