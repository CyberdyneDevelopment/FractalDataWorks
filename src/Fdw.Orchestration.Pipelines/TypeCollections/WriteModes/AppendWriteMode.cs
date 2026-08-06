using Fdw.Configuration;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Orchestration.Pipelines.Abstractions;
using Fdw.Orchestration.Pipelines.Abstractions.TypeCollections.WriteModeOptions;
using Fdw.Results;
using WriteModesCollection = Fdw.Orchestration.Pipelines.Abstractions.TypeCollections.WriteModeOptions.WriteModes;

namespace Fdw.Orchestration.Pipelines.TypeCollections.WriteModes;

/// <summary>
/// Write mode that appends new data to existing data.
/// </summary>
/// <remarks>
/// Append mode adds new records without checking for duplicates or
/// modifying existing data. Best for log-style or append-only destinations.
/// </remarks>
[TypeOption(typeof(WriteModesCollection), "Append", RestrictToCurrentCompilation = true)]
public sealed class AppendWriteMode : WriteModeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AppendWriteMode"/> class.
    /// </summary>
    public AppendWriteMode()
        : base(
            id: 1,
            name: "Append",
            requiresExistenceCheck: false,
            preservesExistingData: true,
            canCreate: true,
            canUpdate: false,
            canDelete: false)
    {
    }

    /// <inheritdoc/>
    public override Task<IGenericResult> Validate(
        IGenericConfiguration stageConfiguration,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IGenericResult>(GenericResult.Success());
    }
}
