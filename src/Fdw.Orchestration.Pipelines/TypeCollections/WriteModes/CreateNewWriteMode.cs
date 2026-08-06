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
/// Write mode that only creates new records, failing if they exist.
/// </summary>
/// <remarks>
/// CreateNew mode ensures only new records are inserted. If a record
/// with the same key already exists, the operation fails. Useful for
/// ensuring idempotent first-time inserts.
/// </remarks>
[TypeOption(typeof(WriteModesCollection), "CreateNew", RestrictToCurrentCompilation = true)]
public sealed class CreateNewWriteMode : WriteModeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateNewWriteMode"/> class.
    /// </summary>
    public CreateNewWriteMode()
        : base(
            id: 4,
            name: "CreateNew",
            requiresExistenceCheck: true,
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
        // CreateNew requires key field configuration to check existence
        return Task.FromResult<IGenericResult>(GenericResult.Success());
    }
}
