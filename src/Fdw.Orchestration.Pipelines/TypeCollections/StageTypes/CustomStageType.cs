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
/// Stage type for custom processing logic.
/// </summary>
/// <remarks>
/// Custom stages allow for user-defined processing that doesn't fit the
/// standard Extract/Transform/Load/Validate patterns. All configuration
/// options are determined by the specific implementation.
/// </remarks>
[TypeOption(typeof(StageTypesCollection), "Custom", RestrictToCurrentCompilation = true)]
public sealed class CustomStageType : StageTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CustomStageType"/> class.
    /// </summary>
    public CustomStageType()
        : base(
            id: 5,
            name: "Custom",
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
        // Custom stages defer validation to their specific implementation
        return Task.FromResult<IGenericResult>(GenericResult.Success());
    }
}
