using Fdw.Collections;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Pipelines;

namespace Fdw.Services.Etl.Graphs;

/// <summary>
/// The graph pipeline type (FDW-1396, FDW-6244): the Pipeline domain's implementation named <c>Graph</c>, whose pipelines are graphs of components wired by
/// named input and output nodes, run by the row-flow engine over the ETL run (<see cref="PipelineExecutionOptions"/> in,
/// <see cref="IEtlPipelineExecutionResult"/> out) and queued for the host's executor.
/// </summary>
/// <remarks>
/// A graph pipeline is stored as a <see cref="GraphPipelineConfiguration"/> over <c>pipe.GraphPipelineConfiguration</c> with its components, links and dropped
/// outputs. Everything the type registers is set by <see cref="GraphPipelineTypeBase{TIn, TOut}"/>; this option states its name, which a pipeline's domain row
/// names as its implementation, and its run mode. The option name and this class name are what stored rows and the option's id resolve by.
/// </remarks>
[Implementation(typeof(PipelineTypes), OptionName)]
public sealed class GraphPipelineType : GraphPipelineTypeBase<PipelineExecutionOptions, IEtlPipelineExecutionResult>
{
    /// <summary>The name a pipeline's domain row gives this implementation.</summary>
    public const string OptionName = "Graph";

    /// <summary>Initializes a new instance of the <see cref="GraphPipelineType"/> class. The instance is made by the module initializer that collects
    /// <see cref="PipelineTypes"/>.</summary>
    public GraphPipelineType()
        : base(
            name: OptionName,
            displayName: OptionName,
            description: "Graph pipeline: components wired by named input and output nodes, run by the row-flow engine",
            runModeName: "Queued")
    {
        Registration((builder, loggerFactory) => GraphRegistration(this, builder, loggerFactory));
        Initialization((host, loggerFactory) => GraphInitialization(this, host, loggerFactory));
    }
}
