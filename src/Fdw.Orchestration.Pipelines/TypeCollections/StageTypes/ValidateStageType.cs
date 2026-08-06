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
/// Stage type for validating data.
/// </summary>
/// <remarks>
/// Validate stages check data quality, apply validation rules, and can filter
/// or flag invalid records. They consume input and produce output (potentially
/// with validation status attached).
/// </remarks>
[TypeOption(typeof(StageTypesCollection), "Validate", RestrictToCurrentCompilation = true)]
public sealed class ValidateStageType : StageTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidateStageType"/> class.
    /// </summary>
    public ValidateStageType()
        : base(
            id: 4,
            name: "Validate",
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
        // Validate stages need validation rules to be useful
        return Task.FromResult<IGenericResult>(GenericResult.Success());
    }
}
