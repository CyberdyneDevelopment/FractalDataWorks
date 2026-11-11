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
/// folder and no other organization's store to fall back to. New files go to the assignment in force; a file already written is read
/// through the assignment it was stored under, in force or superseded (FDW-3396).
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
        _assignments = assignments;
        _grants = grants;
        _connections = connections;
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

        var assignment = await AssignmentsInForce.For(_assignments, organizationId, purpose, _logger, cancellationToken).ConfigureAwait(false);
        if (assignment.IsFailure || assignment.Value is null)
        {
            return assignment.IsFailure
                ? assignment.ToNewResult<IDocumentStore>()
                : GenericResult<IDocumentStore>.Failure(DocumentStoreLog.NothingAssigned(_logger, organizationId, purpose));
        }

        var store = await Open(organizationId, assignment.Value, cancellationToken).ConfigureAwait(false);
        if (store.IsFailure || store.Value is not { } opened)
        {
            return store.ToNewResult<IDocumentStore>();
        }

        DocumentStoreLog.StoreFound(_logger, organizationId, opened.ConnectionName, purpose, opened.Root);
        return GenericResult<IDocumentStore>.Success(opened.Store);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IDocumentStore>> Stored(Guid organizationId, Guid storedUnder, CancellationToken cancellationToken = default)
    {
        var assignment = await _assignments.Get(storedUnder, cancellationToken).ConfigureAwait(false);
        if (assignment.IsFailure || assignment.Value is null || assignment.Value.IsDeleted)
        {
            return GenericResult<IDocumentStore>.Failure(DocumentStoreLog.StoredUnderNotFound(_logger, organizationId, storedUnder));
        }

        if (assignment.Value.OrganizationId != organizationId)
        {
            return GenericResult<IDocumentStore>.Failure(
                DocumentStoreLog.StoredUnderOtherOrganization(_logger, organizationId, storedUnder, assignment.Value.OrganizationId));
        }

        var store = await Open(organizationId, assignment.Value, cancellationToken).ConfigureAwait(false);
        if (store.IsFailure || store.Value is not { } opened)
        {
            return store.ToNewResult<IDocumentStore>();
        }

        DocumentStoreLog.StoredUnderFound(_logger, organizationId, storedUnder, opened.ConnectionName, assignment.Value.Purpose, opened.Root);
        return GenericResult<IDocumentStore>.Success(opened.Store);
    }

    // The store an assignment leads to: its grant's connection, narrowed to the grant's folder plus the assignment's base folder, and
    // named by the assignment so a file written through it can be read through it again.
    private async Task<IGenericResult<OpenedStore>> Open(
        Guid organizationId, IConnectionAssignmentImplementationConfiguration assignment, CancellationToken cancellationToken)
    {
        var grant = await _grants.Get(assignment.GrantId, cancellationToken).ConfigureAwait(false);
        if (grant.IsFailure || grant.Value is null)
        {
            return GenericResult<OpenedStore>.Failure(DocumentStoreLog.GrantMissing(_logger, organizationId, assignment.Purpose, assignment.GrantId));
        }

        var folder = Folder(organizationId, grant.Value, assignment);
        if (folder.IsFailure)
        {
            return folder.ToNewResult<OpenedStore>();
        }

        var connection = await _connections.Get(grant.Value.ConnectionName, cancellationToken).ConfigureAwait(false);
        if (connection.IsFailure || connection.Value is null)
        {
            return GenericResult<OpenedStore>.Failure(
                DocumentStoreLog.ConnectionNotOpened(_logger, grant.Value.ConnectionName, assignment.Purpose, connection.CurrentMessage));
        }

        return GenericResult<OpenedStore>.Success(new OpenedStore(
            new NarrowedDocumentStore(connection.Value, folder.Value, assignment.Id, _loggerFactory?.CreateLogger<NarrowedDocumentStore>()),
            grant.Value.ConnectionName,
            folder.Value.Value));
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
