using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Documents.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Configuration;
using Fdw.Services.Connections.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Connections;

/// <summary>
/// Finds the document storage an organization uses for a purpose and narrows it to the organization's folder (FDW-1581).
/// </summary>
/// <remarks>
/// The assignment names a grant; the grant names a connection and a folder; the store is the connection narrowed to the grant's folder
/// plus the assignment's base folder. Every step that finds nothing is a clear refusal: there is no default connection, no default
/// folder and no other organization's store to fall back to.
/// </remarks>
public sealed class DocumentStores : IDocumentStores
{
    private readonly IConnectionAssignmentConfigurationProvider _assignments;
    private readonly IConnectionGrantConfigurationProvider _grants;
    private readonly IDocumentConnectionProvider _connections;
    private readonly ILoggerFactory? _loggerFactory;
    private readonly ILogger<DocumentStores> _logger;

    /// <summary>Initializes a new instance of the <see cref="DocumentStores"/> class.</summary>
    /// <param name="assignments">Reads the connection each organization is assigned.</param>
    /// <param name="grants">Reads the connections granted to tenants.</param>
    /// <param name="connections">Opens the granted connection.</param>
    /// <param name="loggerFactory">Creates the logger of each store.</param>
    /// <param name="logger">The logger.</param>
    public DocumentStores(
        IConnectionAssignmentConfigurationProvider assignments,
        IConnectionGrantConfigurationProvider grants,
        IDocumentConnectionProvider connections,
        ILoggerFactory? loggerFactory,
        ILogger<DocumentStores>? logger)
    {
        _assignments = assignments ?? throw new ArgumentNullException(nameof(assignments));
        _grants = grants ?? throw new ArgumentNullException(nameof(grants));
        _connections = connections ?? throw new ArgumentNullException(nameof(connections));
        _loggerFactory = loggerFactory;
        _logger = logger ?? NullLogger<DocumentStores>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IDocumentStore>> For(Guid organizationId, string purpose, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(purpose))
        {
            return GenericResult<IDocumentStore>.Failure(DocumentStoreLog.PurposeRequired(_logger, organizationId));
        }

        var assignment = await FindAssignment(organizationId, purpose, cancellationToken).ConfigureAwait(false);
        if (assignment.IsFailure || assignment.Value is null)
        {
            return assignment.IsFailure
                ? assignment.ToNewResult<IDocumentStore>()
                : GenericResult<IDocumentStore>.Failure(DocumentStoreLog.NothingAssigned(_logger, organizationId, purpose));
        }

        var grant = await _grants.Get(assignment.Value.GrantId, cancellationToken).ConfigureAwait(false);
        if (grant.IsFailure || grant.Value is null)
        {
            return GenericResult<IDocumentStore>.Failure(DocumentStoreLog.GrantMissing(_logger, organizationId, purpose, assignment.Value.GrantId));
        }

        var folder = Folder(organizationId, grant.Value, assignment.Value);
        if (folder.IsFailure)
        {
            return folder.ToNewResult<IDocumentStore>();
        }

        var connection = await _connections.Get(grant.Value.ConnectionName, cancellationToken).ConfigureAwait(false);
        if (connection.IsFailure || connection.Value is null)
        {
            return GenericResult<IDocumentStore>.Failure(DocumentStoreLog.ConnectionNotOpened(_logger, grant.Value.ConnectionName, purpose, connection.CurrentMessage));
        }

        DocumentStoreLog.StoreFound(_logger, organizationId, grant.Value.ConnectionName, purpose, folder.Value.Value);
        return GenericResult<IDocumentStore>.Success(
            new NarrowedDocumentStore(connection.Value, folder.Value, _loggerFactory?.CreateLogger<NarrowedDocumentStore>()));
    }

    private async Task<IGenericResult<IConnectionAssignmentImplementationConfiguration?>> FindAssignment(Guid organizationId, string purpose, CancellationToken cancellationToken)
    {
        var found = await _assignments.Find<ConnectionAssignmentImplementationConfiguration>(
            assignment => assignment.OrganizationId == organizationId && assignment.IsCurrent && !assignment.IsDeleted
                && string.Equals(assignment.Purpose, purpose, StringComparison.OrdinalIgnoreCase), cancellationToken).ConfigureAwait(false);
        return found.IsSuccess && found.Value is { } rows
            ? GenericResult<IConnectionAssignmentImplementationConfiguration?>.Success(rows.Count > 0 ? rows[0] : null)
            : GenericResult<IConnectionAssignmentImplementationConfiguration?>.Failure(
                DocumentStoreLog.ReadFailed(_logger, $"The connection assigned to organization '{organizationId}'", found.CurrentMessage));
    }

    // The grant's folder with the assignment's base folder inside it. Both are parsed as document paths, so a separator or a parent
    // segment that does not belong is refused here, and a missing folder is the root, which is a stated state: the whole connection
    // for a grant with no folder, the whole grant for an assignment with no base.
    private IGenericResult<DocumentPath> Folder(Guid organizationId, IConnectionGrantImplementationConfiguration grant, IConnectionAssignmentImplementationConfiguration assignment)
    {
        var grantFolder = DocumentPath.Parse(grant.FolderPath);
        if (grantFolder.IsFailure)
        {
            return GenericResult<DocumentPath>.Failure(DocumentStoreLog.FolderInvalid(_logger, "grant", grant.FolderPath, organizationId, grantFolder.CurrentMessage));
        }

        var baseFolder = DocumentPath.Parse(assignment.BaseFolder);
        return baseFolder.IsFailure
            ? GenericResult<DocumentPath>.Failure(DocumentStoreLog.FolderInvalid(_logger, "base", assignment.BaseFolder, organizationId, baseFolder.CurrentMessage))
            : GenericResult<DocumentPath>.Success(grantFolder.Value.Combine(baseFolder.Value));
    }
}
