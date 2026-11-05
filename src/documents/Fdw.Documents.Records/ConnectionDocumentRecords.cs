using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Documents.Records.Abstractions;
using Fdw.Documents.Records.Abstractions.Commands;
using Fdw.Results;

namespace Fdw.Documents.Records;

/// <summary><see cref="IDocumentRecords"/> carried out as document record commands on the organization's DocumentRecord connection (FDW-3673).</summary>
internal sealed class ConnectionDocumentRecords : IDocumentRecords
{
    private readonly DocumentRecordCommandRunner _runner;

    /// <summary>Initializes a new instance of the <see cref="ConnectionDocumentRecords"/> class.</summary>
    /// <param name="runner">Sends the commands for the store's organization.</param>
    public ConnectionDocumentRecords(DocumentRecordCommandRunner runner)
    {
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
    }

    /// <inheritdoc />
    public Task<IGenericResult<IDocumentRecord>> Get(Guid organizationId, Guid id, CancellationToken cancellationToken = default) =>
        _runner.Run(organizationId, new GetDocumentRecordCommand(organizationId, id), cancellationToken);

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlyList<IDocumentRecord>>> List(Guid organizationId, string documentType, CancellationToken cancellationToken = default) =>
        _runner.Run(organizationId, new ListDocumentRecordsCommand(organizationId, documentType), cancellationToken);

    /// <inheritdoc />
    public Task<IGenericResult<long>> Save(Guid organizationId, IDocumentRecord document, long expectedRevision, CancellationToken cancellationToken = default) =>
        _runner.Run(organizationId, new SaveDocumentRecordCommand(organizationId, document, expectedRevision), cancellationToken);
}
