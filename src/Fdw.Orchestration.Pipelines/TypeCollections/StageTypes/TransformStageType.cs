using Fdw.Configuration;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Orchestration.Pipelines.Abstractions;
using Fdw.Orchestration.Pipelines.Abstractions.TypeCollections.StageTypeOptions;
using Fdw.Results;
using StageTypesCollection = Fdw.Orchestration.Pipelines.Abstractions.TypeCollections.StageTypeOptions.StageTypes;

namespace Fdw.Orchestration.Pipelines.TypeCollections.StageTypes;

/// <summary>
/// Stage type for transforming data.
/// </summary>
/// <remarks>
/// Transform stages modify, filter, aggregate, or reshape data. They consume
/// input from upstream stages and produce output for downstream stages.
/// No external source or destination connections are required.
/// </remarks>
[TypeOption(typeof(StageTypesCollection), "Transform", RestrictToCurrentCompilation = true)]
public sealed class TransformStageType : StageTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TransformStageType"/> class.
    /// </summary>
    public TransformStageType()
        : base(
            id: 2,
            name: "Transform",
            requiresSource: false,
            requiresDestination: false,
            supportsParallel: true,
            producesOutput: true,
            consumesInput: true)
    {
    }

    /// <inheritdoc/>
    public override Task<IGenericResult> ValidateConfiguration(
        IGenericConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        // Transform stages have minimal required configuration
        return Task.FromResult<IGenericResult>(GenericResult.Success());
    }
}
