using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>The variable <c>{{PipelineName}}</c>: The name of the pipeline that is running.</summary>
[TypeOption(typeof(NotifyVariables), "PipelineName")]
public sealed class PipelineNameNotifyVariable : NotifyVariableBase
{
    /// <summary>Initializes a new instance of the <see cref="PipelineNameNotifyVariable"/> class.</summary>
    public PipelineNameNotifyVariable() : base("PipelineName", "Pipeline name", "The name of the pipeline that is running")
    {
    }

    /// <inheritdoc/>
    public override string ValueOf(INotifyRunFacts facts) => facts.PipelineName;
}
