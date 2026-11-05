using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Documents.Records.Abstractions;
using Fdw.Documents.Records.Abstractions.Commands;
using Fdw.Results;

namespace Fdw.Documents.Records;

/// <summary><see cref="IDocumentStateHistory"/> carried out as document record commands on the organization's DocumentRecord connection (FDW-3673).</summary>
internal sealed class ConnectionDocumentStateHistory : IDocumentStateHistory
{
    private readonly DocumentRecordCommandRunner _runner;

    /// <summary>Initializes a new instance of the <see cref="ConnectionDocumentStateHistory"/> class.</summary>
    /// <param name="runner">Sends the commands for the store's organization.</param>
    public ConnectionDocumentStateHistory(DocumentRecordCommandRunner runner)
    {
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
    }

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlyList<IStateChange>>> List(Guid organizationId, Guid documentId, CancellationToken cancellationToken = default) =>
        _runner.Run(organizationId, new ListStateChangesCommand(organizationId, documentId), cancellationToken);

    /// <inheritdoc />
    public Task<IGenericResult<long>> Enter(Guid organizationId, IStateChange change, long expectedRevision, CancellationToken cancellationToken = default) =>
        _runner.Run(organizationId, new EnterStateCommand(organizationId, change, expectedRevision), cancellationToken);
}
