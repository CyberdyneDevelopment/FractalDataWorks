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
/// Write mode that replaces all existing data with new data.
/// </summary>
/// <remarks>
/// Overwrite mode truncates or deletes existing data before writing.
/// Use with caution as this is a destructive operation.
/// </remarks>
[TypeOption(typeof(WriteModesCollection), "Overwrite", RestrictToCurrentCompilation = true)]
public sealed class OverwriteWriteMode : WriteModeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OverwriteWriteMode"/> class.
    /// </summary>
    public OverwriteWriteMode()
        : base(
            id: 2,
            name: "Overwrite",
            requiresExistenceCheck: false,
            preservesExistingData: false,
            canCreate: true,
            canUpdate: false,
            canDelete: true)
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
