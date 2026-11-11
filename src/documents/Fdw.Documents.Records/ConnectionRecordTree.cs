using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Documents.Records.Abstractions;
using Fdw.Documents.Records.Abstractions.Commands;
using Fdw.Results;

namespace Fdw.Documents.Records;

/// <summary><see cref="IRecordTree"/> carried out as document record commands on the organization's DocumentRecord connection (FDW-3673).</summary>
internal sealed class ConnectionRecordTree : IRecordTree
{
    private readonly DocumentRecordCommandRunner _runner;

    /// <summary>Initializes a new instance of the <see cref="ConnectionRecordTree"/> class.</summary>
    /// <param name="runner">Sends the commands for the store's organization.</param>
    public ConnectionRecordTree(DocumentRecordCommandRunner runner)
    {
        _runner = runner;
    }

    /// <inheritdoc />
    public Task<IGenericResult<IRecordNode>> Get(Guid organizationId, Guid id, CancellationToken cancellationToken = default) =>
        _runner.Run(organizationId, new GetRecordNodeCommand(organizationId, id), cancellationToken);

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlyList<IRecordNode>>> Children(Guid organizationId, Guid? parentId, CancellationToken cancellationToken = default) =>
        _runner.Run(organizationId, new ListRecordChildrenCommand(organizationId, parentId), cancellationToken);

    /// <inheritdoc />
    public Task<IGenericResult<long>> Save(Guid organizationId, IRecordNode node, long expectedRevision, CancellationToken cancellationToken = default) =>
        _runner.Run(organizationId, new SaveRecordNodeCommand(organizationId, node, expectedRevision), cancellationToken);
}
