using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Documents.Records.Abstractions;
using Fdw.Documents.Records.Abstractions.Commands;
using Fdw.Results;

namespace Fdw.Documents.Records;

/// <summary><see cref="IRecordRequirements"/> carried out as document record commands on the organization's DocumentRecord connection (FDW-3673).</summary>
internal sealed class ConnectionRecordRequirements : IRecordRequirements
{
    private readonly DocumentRecordCommandRunner _runner;

    /// <summary>Initializes a new instance of the <see cref="ConnectionRecordRequirements"/> class.</summary>
    /// <param name="runner">Sends the commands for the store's organization.</param>
    public ConnectionRecordRequirements(DocumentRecordCommandRunner runner)
    {
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
    }

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlyList<IRecordRequirement>>> List(Guid organizationId, Guid recordId, CancellationToken cancellationToken = default) =>
        _runner.Run(organizationId, new ListRecordRequirementsCommand(organizationId, recordId), cancellationToken);

    /// <inheritdoc />
    public Task<IGenericResult<long>> Save(Guid organizationId, IRecordRequirement requirement, long expectedRevision, CancellationToken cancellationToken = default) =>
        _runner.Run(organizationId, new SaveRecordRequirementCommand(organizationId, requirement, expectedRevision), cancellationToken);
}
