using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Documents.Records.Abstractions.Commands;
using Fdw.Documents.Records.Abstractions.Definitions;
using Fdw.Results;

namespace Fdw.Documents.Records;

/// <summary>
/// <see cref="IDocumentDefinitions{T}"/> of one kind, read with that kind's command from the organization's DocumentRecord connection (FDW-3673).
/// </summary>
/// <typeparam name="T">The kind of definition.</typeparam>
internal sealed class ConnectionDocumentDefinitions<T> : IDocumentDefinitions<T>
    where T : IScopedDefinition
{
    private readonly DocumentRecordCommandRunner _runner;
    private readonly Func<Guid, Guid?, IDocumentRecordCommand<IReadOnlyList<T>>> _read;

    /// <summary>Initializes a new instance of the <see cref="ConnectionDocumentDefinitions{T}"/> class.</summary>
    /// <param name="runner">Sends the commands for the store's tenant and organization.</param>
    /// <param name="read">Makes the kind's read command for a tenant and, when named, an organization.</param>
    public ConnectionDocumentDefinitions(DocumentRecordCommandRunner runner, Func<Guid, Guid?, IDocumentRecordCommand<IReadOnlyList<T>>> read)
    {
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        _read = read ?? throw new ArgumentNullException(nameof(read));
    }

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlyList<T>>> Rows(Guid tenantId, Guid? organizationId, CancellationToken cancellationToken = default) =>
        _runner.Run(tenantId, organizationId, _read(tenantId, organizationId), cancellationToken);
}
