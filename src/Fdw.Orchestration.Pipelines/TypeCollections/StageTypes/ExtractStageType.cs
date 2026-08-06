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
/// Stage type for extracting data from a source.
/// </summary>
/// <remarks>
/// Extract stages read data from sources (databases, files, APIs) and produce
/// output for downstream stages. They require a source connection but no destination.
/// </remarks>
[TypeOption(typeof(StageTypesCollection), "Extract", RestrictToCurrentCompilation = true)]
public sealed class ExtractStageType : StageTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExtractStageType"/> class.
    /// </summary>
    public ExtractStageType()
        : base(
            id: 1,
            name: "Extract",
            requiresSource: true,
            requiresDestination: false,
            supportsParallel: true,
            producesOutput: true,
            consumesInput: false)
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
                GenericResult.Failure(PipelineResultCodes.ByName("ExtractStageRequiresConfiguration")));
        }

        return Task.FromResult<IGenericResult>(GenericResult.Success());
    }
}
