using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Documents.Records.Abstractions;
using Fdw.Documents.Records.Abstractions.Links;
using Fdw.Documents.Records.Abstractions.Commands;
using Fdw.Results;

namespace Fdw.Documents.Records;

/// <summary><see cref="ILinkTargets"/> carried out as document record commands on the organization's DocumentRecord connection (FDW-3673).</summary>
internal sealed class ConnectionLinkTargets : ILinkTargets
{
    private readonly DocumentRecordCommandRunner _runner;

    /// <summary>Initializes a new instance of the <see cref="ConnectionLinkTargets"/> class.</summary>
    /// <param name="runner">Sends the commands for the store's organization.</param>
    public ConnectionLinkTargets(DocumentRecordCommandRunner runner)
    {
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
    }

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlyList<string>>> Kinds(Guid organizationId, CancellationToken cancellationToken = default) =>
        _runner.Run(organizationId, new ListLinkTargetKindsCommand(organizationId), cancellationToken);

    /// <inheritdoc />
    public Task<IGenericResult<bool>> Exists(Guid organizationId, string targetKind, Guid targetId, CancellationToken cancellationToken = default) =>
        _runner.Run(organizationId, new LinkTargetExistsCommand(organizationId, targetKind, targetId), cancellationToken);
}
