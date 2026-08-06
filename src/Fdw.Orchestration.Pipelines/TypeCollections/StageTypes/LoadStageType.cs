using Fdw.Configuration;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Orchestration.Pipelines.Abstractions;
using Fdw.Orchestration.Pipelines.Abstractions.TypeCollections.StageTypeOptions;
using Fdw.Orchestration.Pipelines.Results;
using Fdw.Results;
using StageTypesCollection = Fdw.Orchestration.Pipelines.Abstractions.TypeCollections.StageTypeOptions.StageTypes;

namespace Fdw.Orchestration.Pipelines.TypeCollections.StageTypes;

/// <summary>
/// Stage type for loading data to a destination.
/// </summary>
/// <remarks>
/// Load stages write data to destinations (databases, files, APIs). They consume
/// input from upstream stages and require a destination connection. Load stages
/// typically do not produce output for further processing.
/// </remarks>
[TypeOption(typeof(StageTypesCollection), "Load", RestrictToCurrentCompilation = true)]
public sealed class LoadStageType : StageTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LoadStageType"/> class.
    /// </summary>
    public LoadStageType()
        : base(
            id: 3,
            name: "Load",
            requiresSource: false,
            requiresDestination: true,
            supportsParallel: false,
            producesOutput: false,
            consumesInput: true)
    {
    }

    /// <inheritdoc/>
    public override Task<IGenericResult> ValidateConfiguration(
        IGenericConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        if (configuration == null)
        {
            return Task.FromResult<IGenericResult>(
                GenericResult.Failure(PipelineResultCodes.ByName("LoadStageRequiresConfiguration")));
        }

        return Task.FromResult<IGenericResult>(GenericResult.Success());
    }
}
