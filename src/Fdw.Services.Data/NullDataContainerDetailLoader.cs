using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;

namespace Fdw.Services.Data;

/// <summary>
/// No-op implementation of <see cref="IDataContainerDetailLoader"/> used when no storage-specific
/// implementation is registered (e.g., in unit tests or non-MsSql environments).
/// Returns empty lists for all containers without logging or throwing.
/// </summary>
// Why: sentinel pattern avoids null-checks throughout the loader's tree-builder pass and prevents
// NullReferenceException when field/key detail is not needed (e.g., cfg CRUD endpoints
// only need container Name and Path — they never access Fields or Keys at bootstrap).
public sealed class NullDataContainerDetailLoader : IDataContainerDetailLoader
{
    /// <summary>Gets the shared singleton instance.</summary>
    public static readonly NullDataContainerDetailLoader Instance = new();

    private NullDataContainerDetailLoader()
    {
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<IDataField>> LoadFields(Guid containerRowId, string typeId, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<IDataField>>([]);

    /// <inheritdoc />
    public Task<IReadOnlyList<IContainerKey>> LoadKeys(Guid containerRowId, string typeId, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<IContainerKey>>([]);
}
