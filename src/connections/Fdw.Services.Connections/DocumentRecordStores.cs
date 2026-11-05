using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Documents.Records;
using Fdw.Documents.Records.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Connections;

/// <summary>
/// Finds an organization's document records (FDW-3673) the way <see cref="DocumentStores"/> finds its files: its assignment in force for the
/// purpose <c>DocumentRecords</c> names a grant its tenant holds, and the grant names the DocumentRecord connection.
/// </summary>
/// <remarks>
/// Every step that finds nothing is a refusal naming the organization: no assignment, a grant that is gone, a connection that cannot be opened
/// as a DocumentRecord connection, or one that does not fit the purpose. There is no other connection to fall back to.
/// </remarks>
public sealed class DocumentRecordStores : IDocumentRecordStores
{
    private const string Purpose = "DocumentRecords";

    private readonly IConnectionAssignmentConfigurationProvider _assignments;
    private readonly IConnectionGrantConfigurationProvider _grants;
    private readonly IDocumentRecordConnectionProvider _connections;
    private readonly ILoggerFactory? _loggerFactory;
    private readonly ILogger<DocumentRecordStores> _logger;

    /// <summary>Initializes a new instance of the <see cref="DocumentRecordStores"/> class.</summary>
    /// <param name="assignments">Reads the connection each organization is assigned.</param>
    /// <param name="grants">Reads the connections granted to tenants.</param>
    /// <param name="connections">Opens the granted connection as a DocumentRecord connection.</param>
    /// <param name="loggerFactory">Creates the logger of each store.</param>
    /// <param name="logger">The logger.</param>
    public DocumentRecordStores(
        IConnectionAssignmentConfigurationProvider assignments,
        IConnectionGrantConfigurationProvider grants,
        IDocumentRecordConnectionProvider connections,
        ILoggerFactory? loggerFactory,
        ILogger<DocumentRecordStores>? logger)
    {
        _assignments = assignments ?? throw new ArgumentNullException(nameof(assignments));
        _grants = grants ?? throw new ArgumentNullException(nameof(grants));
        _connections = connections ?? throw new ArgumentNullException(nameof(connections));
        _loggerFactory = loggerFactory;
        _logger = logger ?? NullLogger<DocumentRecordStores>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IDocumentRecordStore>> For(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var assignment = await AssignmentsInForce.For(_assignments, organizationId, Purpose, _logger, cancellationToken).ConfigureAwait(false);
        if (assignment.IsFailure || assignment.Value is not { } assigned)
        {
            return assignment.IsFailure
                ? assignment.ToNewResult<IDocumentRecordStore>()
                : GenericResult<IDocumentRecordStore>.Failure(DocumentStoreLog.NothingAssigned(_logger, organizationId, Purpose));
        }

        var grant = await _grants.Get(assigned.GrantId, cancellationToken).ConfigureAwait(false);
        if (grant.IsFailure || grant.Value is not { } granted)
        {
            return GenericResult<IDocumentRecordStore>.Failure(DocumentStoreLog.GrantMissing(_logger, organizationId, Purpose, assigned.GrantId));
        }

        var connection = await _connections.Get(granted.ConnectionName, cancellationToken).ConfigureAwait(false);
        if (connection.IsFailure || connection.Value is not { } opened)
        {
            return GenericResult<IDocumentRecordStore>.Failure(
                DocumentStoreLog.ConnectionNotOpened(_logger, granted.ConnectionName, Purpose, connection.CurrentMessage));
        }

        // Checked again at use, not only when granted: a connection's capabilities are its implementation's, and that can change after the grant.
        if (PurposeLookup.Named(Purpose) is not { } purpose)
        {
            return GenericResult<IDocumentRecordStore>.Failure(DocumentStoreLog.RecordConnectionUnfit(
                _logger, organizationId, granted.ConnectionName, Purpose, $"the purpose '{Purpose}' is not registered on this host"));
        }

        if (purpose.Unfit(opened) is { } reason)
        {
            return GenericResult<IDocumentRecordStore>.Failure(
                DocumentStoreLog.RecordConnectionUnfit(_logger, organizationId, granted.ConnectionName, Purpose, reason));
        }

        DocumentStoreLog.RecordStoreFound(_logger, organizationId, granted.ConnectionName);
        return GenericResult<IDocumentRecordStore>.Success(new ConnectionDocumentRecordStore(
            opened, organizationId, granted.GranteeTenantId,
            _loggerFactory is null ? NullLogger<ConnectionDocumentRecordStore>.Instance : _loggerFactory.CreateLogger<ConnectionDocumentRecordStore>()));
    }
}
