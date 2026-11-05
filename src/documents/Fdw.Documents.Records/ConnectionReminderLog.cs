using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Documents.Records.Abstractions;
using Fdw.Documents.Records.Abstractions.Commands;
using Fdw.Results;

namespace Fdw.Documents.Records;

/// <summary><see cref="IReminderLog"/> carried out as document record commands on the organization's DocumentRecord connection (FDW-3673).</summary>
internal sealed class ConnectionReminderLog : IReminderLog
{
    private readonly DocumentRecordCommandRunner _runner;

    /// <summary>Initializes a new instance of the <see cref="ConnectionReminderLog"/> class.</summary>
    /// <param name="runner">Sends the commands for the store's organization.</param>
    public ConnectionReminderLog(DocumentRecordCommandRunner runner)
    {
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
    }

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlyList<IReminderEntry>>> List(Guid organizationId, Guid requirementId, CancellationToken cancellationToken = default) =>
        _runner.Run(organizationId, new ListReminderEntriesCommand(organizationId, requirementId), cancellationToken);

    /// <inheritdoc />
    public async Task<IGenericResult> Record(Guid organizationId, IReminderEntry entry, CancellationToken cancellationToken = default) =>
        await _runner.Run(organizationId, new RecordReminderCommand(organizationId, entry), cancellationToken).ConfigureAwait(false);
}
