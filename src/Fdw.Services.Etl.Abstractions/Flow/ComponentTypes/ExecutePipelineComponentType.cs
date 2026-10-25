using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>Starts another pipeline, passing values, and optionally waits for it to end.</summary>
[TypeOption(typeof(ComponentTypes), "ExecutePipeline")]
public sealed class ExecutePipelineComponentType : ComponentTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="ExecutePipelineComponentType"/> class.</summary>
    public ExecutePipelineComponentType() : base(
        "ExecutePipeline",
        "Execute pipeline",
        "Starts another pipeline, passing values, and optionally waits for it to end; it can start it when the run fails.",
        "Control flow",
        false,
        [
            new StageMember("pipeline", "Text", true, "The pipeline to start.", SummarisesComponent: true),
            new StageMember("parameters", "Parameters", false, "The values of its parameters, as text; a value may name a variable in double braces."),
            new StageMember("awaitCompletion", "Boolean", true, "Whether the component waits for the started pipeline to end."),
            new StageMember("resultVariable", "Variable", false, "A declared variable set to whether the started pipeline succeeded; needs awaitCompletion."),
            new StageMember("failOnChildFailure", "Boolean", false, "Whether the component fails when the started pipeline does not succeed; needs awaitCompletion."),
            new StageMember("runPoint", "RunPoints", false, "When it starts the pipeline: AfterStage and AfterLoad start it when the run reaches the component (its stage); OnFailure starts it only if the run fails, and only on a component that is not inside a loop. Absent, it starts the pipeline when the run reaches the component. With a run point, the parameter values may use the run's facts: {{PipelineName}}, {{ExecutionId}}, {{StartedAt}}, {{EndedAt}}, {{Outcome}}, {{RowsExtracted}}, {{RowsRejected}} and {{RowsLoaded}}."),
        ])
    {
    }
}
