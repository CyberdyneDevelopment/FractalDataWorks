using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Documents.Records.Abstractions;
using Fdw.Documents.Records.Abstractions.Commands;
using Fdw.Results;

namespace Fdw.Documents.Records;

/// <summary><see cref="IOrganizationRevisions"/> carried out as document record commands on the organization's DocumentRecord connection (FDW-3673).</summary>
internal sealed class ConnectionOrganizationRevisions : IOrganizationRevisions
{
    private readonly DocumentRecordCommandRunner _runner;

    /// <summary>Initializes a new instance of the <see cref="ConnectionOrganizationRevisions"/> class.</summary>
    /// <param name="runner">Sends the commands for the store's organization.</param>
    public ConnectionOrganizationRevisions(DocumentRecordCommandRunner runner)
    {
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
    }

    /// <inheritdoc />
    public Task<IGenericResult<IOrganizationRevision>> Current(Guid organizationId, CancellationToken cancellationToken = default) =>
        _runner.Run(organizationId, new GetOrganizationRevisionCommand(organizationId), cancellationToken);

    /// <inheritdoc />
    public Task<IGenericResult<IOrganizationRevision>> Start(Guid organizationId, CancellationToken cancellationToken = default) =>
        _runner.Run(organizationId, new StartOrganizationRevisionCommand(organizationId), cancellationToken);
}
