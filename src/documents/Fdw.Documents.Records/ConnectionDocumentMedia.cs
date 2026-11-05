using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Documents.Records.Abstractions;
using Fdw.Documents.Records.Abstractions.Commands;
using Fdw.Results;

namespace Fdw.Documents.Records;

/// <summary><see cref="IDocumentMedia"/> carried out as document record commands on the organization's DocumentRecord connection (FDW-3673).</summary>
internal sealed class ConnectionDocumentMedia : IDocumentMedia
{
    private readonly DocumentRecordCommandRunner _runner;

    /// <summary>Initializes a new instance of the <see cref="ConnectionDocumentMedia"/> class.</summary>
    /// <param name="runner">Sends the commands for the store's organization.</param>
    public ConnectionDocumentMedia(DocumentRecordCommandRunner runner)
    {
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
    }

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlyList<IStoredMedia>>> List(Guid organizationId, Guid documentId, CancellationToken cancellationToken = default) =>
        _runner.Run(organizationId, new ListStoredMediaCommand(organizationId, documentId), cancellationToken);

    /// <inheritdoc />
    public Task<IGenericResult<long>> Add(Guid organizationId, IStoredMedia media, long expectedRevision, CancellationToken cancellationToken = default) =>
        _runner.Run(organizationId, new AddStoredMediaCommand(organizationId, media, expectedRevision), cancellationToken);
}
