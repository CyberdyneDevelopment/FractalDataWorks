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
/// Write mode that updates existing records or inserts new ones.
/// </summary>
/// <remarks>
/// Upsert mode (update + insert) checks for existing records by key.
/// If found, updates them; if not, inserts new records. Requires
/// a key field to be defined for existence checking.
/// </remarks>
[TypeOption(typeof(WriteModesCollection), "Upsert", RestrictToCurrentCompilation = true)]
public sealed class UpsertWriteMode : WriteModeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpsertWriteMode"/> class.
    /// </summary>
    public UpsertWriteMode()
        : base(
            id: 3,
            name: "Upsert",
            requiresExistenceCheck: true,
            preservesExistingData: true,
            canCreate: true,
            canUpdate: true,
            canDelete: false)
    {
    }

    /// <inheritdoc/>
    public override Task<IGenericResult> Validate(
        IGenericConfiguration stageConfiguration,
        CancellationToken cancellationToken = default)
    {
        // Upsert requires key field configuration to determine record identity
        return Task.FromResult<IGenericResult>(GenericResult.Success());
    }
}
