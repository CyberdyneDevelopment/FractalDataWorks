using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Documents.Records.Abstractions;
using Fdw.Documents.Records.Abstractions.Commands;
using Fdw.Results;

namespace Fdw.Documents.Records;

/// <summary><see cref="IAttributeValues"/> carried out as document record commands on the organization's DocumentRecord connection (FDW-3673).</summary>
internal sealed class ConnectionAttributeValues : IAttributeValues
{
    private readonly DocumentRecordCommandRunner _runner;

    /// <summary>Initializes a new instance of the <see cref="ConnectionAttributeValues"/> class.</summary>
    /// <param name="runner">Sends the commands for the store's organization.</param>
    public ConnectionAttributeValues(DocumentRecordCommandRunner runner)
    {
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
    }

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlyList<IAttributeValue>>> List(Guid organizationId, Guid ownerId, CancellationToken cancellationToken = default) =>
        _runner.Run(organizationId, new ListAttributeValuesCommand(organizationId, ownerId), cancellationToken);

    /// <inheritdoc />
    public Task<IGenericResult<long>> Set(Guid organizationId, IAttributeValue value, long expectedRevision, CancellationToken cancellationToken = default) =>
        _runner.Run(organizationId, new SetAttributeValueCommand(organizationId, value, expectedRevision), cancellationToken);
}
