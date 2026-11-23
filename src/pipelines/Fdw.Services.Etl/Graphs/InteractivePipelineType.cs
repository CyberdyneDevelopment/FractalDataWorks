using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Pipelines;
using Fdw.Services.Pipelines.Abstractions;

namespace Fdw.Services.Etl.Graphs;

/// <summary>
/// The Interactive pipeline type (FDW-6141): the Pipeline domain's implementation named <c>Interactive</c>, a graph pipeline whose runs are carried out at once, in the host
/// that took the request, under stated limits.
/// </summary>
/// <remarks>
/// A second closing of <see cref="GraphPipelineTypeBase{TIn, TOut}"/>, as <see cref="GraphPipelineType"/> is the first: every closing reads its rows from the one graph row
/// container, so the stored row is the <see cref="GraphPipelineConfiguration"/> and nothing about the graph changes. What differs is the run mode (<c>AtOnce</c>) and what the
/// type requires of a pipeline's row: the facts a run found half-done, a run that takes too long and a run that takes too many steps are decided by (FDW-6141).
/// </remarks>
[Implementation(typeof(PipelineTypes), OptionName)]
public sealed class InteractivePipelineType : GraphPipelineTypeBase<PipelineExecutionOptions, IEtlPipelineExecutionResult>
{
    /// <summary>The name a pipeline's domain row gives this implementation.</summary>
    public const string OptionName = "Interactive";

    /// <summary>Initializes a new instance of the <see cref="InteractivePipelineType"/> class. The instance is made by the module initializer that collects
    /// <see cref="PipelineTypes"/>.</summary>
    public InteractivePipelineType()
        : base(
            name: OptionName,
            displayName: OptionName,
            description: "Interactive pipeline: a graph of actions that runs at once, in the host that took the request, under stated limits",
            runModeName: "AtOnce")
    {
        Registration((builder, loggerFactory) => GraphRegistration(this, builder, loggerFactory));
        Initialization((host, loggerFactory) => GraphInitialization(this, host, loggerFactory));
    }

    /// <inheritdoc />
    /// <remarks>A turn is carried out for a person: what it reads and the tools it calls are that person's.</remarks>
    public override bool RequiresPerson => true;

    /// <inheritdoc />
    public override IReadOnlyList<PipelineConfigurationRequirement> ConfigurationRequirements { get; } =
    [
        PipelineConfigurationRequirement.Of<IHalfDoneRulePipelineConfiguration>("HalfDoneRule", configuration => configuration.HalfDoneRule is not null),
        PipelineConfigurationRequirement.Of<IRunTimeLimitPipelineConfiguration>("MaxRunSeconds", configuration => configuration.MaxRunSeconds is not null),
        PipelineConfigurationRequirement.Of<IGraphPipelineConfiguration>("MaxSteps", configuration => configuration.MaxSteps is not null),
        PipelineConfigurationRequirement.Of<IClaimKeyPipelineConfiguration>("ClaimKeyTemplate", configuration => !string.IsNullOrWhiteSpace(configuration.ClaimKeyTemplate)),
    ];
}
