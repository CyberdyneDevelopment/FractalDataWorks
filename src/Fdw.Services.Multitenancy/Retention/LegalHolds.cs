using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Extensions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Multitenancy.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using UpdateCommands = Fdw.Commands.Data.Extensions.Update;

namespace Fdw.Services.Multitenancy;

/// <summary>
/// Reads and writes the legal holds of organizations' documents, over the table named by <see cref="RetentionTables"/> (FDW-1575).
/// </summary>
/// <remarks>
/// A document has at most one standing hold. Lifting a hold writes who lifted it and when, and keeps the row as the record of the hold.
/// </remarks>
public sealed class LegalHolds : ILegalHolds
{
    private readonly RetentionStore _store;
    private readonly ILogger<LegalHolds> _logger;

    /// <summary>Initializes a new instance of the <see cref="LegalHolds"/> class.</summary>
    /// <param name="gateways">Supplies the configuration gateway of the tables.</param>
    /// <param name="tables">Names the store, path and tables retention is kept in.</param>
    /// <param name="logger">The logger.</param>
    public LegalHolds(IConfigurationGatewayProvider gateways, RetentionTables tables, ILogger<LegalHolds>? logger)
    {
        _logger = logger ?? NullLogger<LegalHolds>.Instance;
        _store = new RetentionStore(gateways, tables, _logger);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<LegalHold>>> List(Guid tenantId, Guid organizationId, bool heldOnly, CancellationToken cancellationToken = default)
    {
        var rows = await Rows(tenantId, organizationId, cancellationToken).ConfigureAwait(false);
        return rows.IsFailure || rows.Value is not { } found
            ? rows.ToNewResult<IReadOnlyList<LegalHold>>()
            : GenericResult<IReadOnlyList<LegalHold>>.Success(
                [.. found.Select(Hold).Where(hold => !heldOnly || hold.IsHeld).OrderByDescending(hold => hold.PlacedAt)]);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<LegalHold>> Place(
        Guid tenantId, Guid organizationId, string documentId, string reason, string placedBy, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(documentId) || string.IsNullOrWhiteSpace(reason) || string.IsNullOrWhiteSpace(placedBy))
        {
            return GenericResult<LegalHold>.Failure(RetentionLog.HoldInvalid(_logger, "The document, the reason and who places the hold are all required"));
        }

        var rows = await Rows(tenantId, organizationId, cancellationToken).ConfigureAwait(false);
        if (rows.IsFailure || rows.Value is not { } found)
        {
            return rows.ToNewResult<LegalHold>();
        }

        if (found.Any(row => Standing(row, documentId)))
        {
            return GenericResult<LegalHold>.Failure(RetentionLog.AlreadyHeld(_logger, documentId, organizationId));
        }

        var gateway = _store.Gateway();
        if (gateway.IsFailure || gateway.Value is not { } resolved)
        {
            return GenericResult<LegalHold>.Failure(RetentionLog.WriteFailed(_logger, What(documentId, organizationId), gateway.CurrentMessage));
        }

        var row = new LegalHoldRow
        {
            Id = Guid.CreateVersion7(), TenantId = tenantId, OrganizationId = organizationId, DocumentId = documentId, Reason = reason,
            PlacedBy = placedBy, PlacedAt = DateTimeOffset.UtcNow,
        };
        var written = await resolved.Execute<int>(
            Insert.Into<LegalHoldRow>(_store.Tables.HoldTable).DataStore(_store.Tables.DataStoreName).Path(_store.Tables.PathName).Value(row),
            cancellationToken).ConfigureAwait(false);
        if (written.IsFailure)
        {
            return GenericResult<LegalHold>.Failure(RetentionLog.WriteFailed(_logger, What(documentId, organizationId), written.CurrentMessage));
        }

        RetentionLog.HoldPlaced(_logger, documentId, organizationId);
        return GenericResult<LegalHold>.Success(Hold(row));
    }

    /// <inheritdoc />
    public async Task<IGenericResult<LegalHold>> Lift(
        Guid tenantId, Guid organizationId, string documentId, string liftedBy, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(liftedBy))
        {
            return GenericResult<LegalHold>.Failure(RetentionLog.HoldInvalid(_logger, "Who lifts the hold is required"));
        }

        var rows = await Rows(tenantId, organizationId, cancellationToken).ConfigureAwait(false);
        if (rows.IsFailure || rows.Value is not { } found)
        {
            return rows.ToNewResult<LegalHold>();
        }

        var standing = found.FirstOrDefault(row => Standing(row, documentId));
        if (standing is null)
        {
            return GenericResult<LegalHold>.Failure(RetentionLog.HoldNotFound(_logger, documentId, organizationId));
        }

        var gateway = _store.Gateway();
        if (gateway.IsFailure || gateway.Value is not { } resolved)
        {
            return GenericResult<LegalHold>.Failure(RetentionLog.WriteFailed(_logger, What(documentId, organizationId), gateway.CurrentMessage));
        }

        var lifted = new LegalHoldLiftUpdate { LiftedBy = liftedBy, LiftedAt = DateTimeOffset.UtcNow };
        var written = await resolved.Execute<int>(
            UpdateCommands.In<LegalHoldLiftUpdate>(_store.Tables.HoldTable).DataStore(_store.Tables.DataStoreName).Path(_store.Tables.PathName)
                .Where("Id", standing.Id).Value(lifted),
            cancellationToken).ConfigureAwait(false);
        if (written.IsFailure)
        {
            return GenericResult<LegalHold>.Failure(RetentionLog.WriteFailed(_logger, What(documentId, organizationId), written.CurrentMessage));
        }

        RetentionLog.HoldLifted(_logger, documentId, organizationId);
        return GenericResult<LegalHold>.Success(Hold(standing) with { LiftedBy = lifted.LiftedBy, LiftedAt = lifted.LiftedAt });
    }

    /// <inheritdoc />
    public async Task<IGenericResult<ISet<string>>> HeldDocuments(Guid tenantId, Guid organizationId, CancellationToken cancellationToken = default)
    {
        var rows = await Rows(tenantId, organizationId, cancellationToken).ConfigureAwait(false);
        return rows.IsFailure || rows.Value is not { } found
            ? rows.ToNewResult<ISet<string>>()
            : GenericResult<ISet<string>>.Success(new HashSet<string>(found.Where(row => row.LiftedAt is null).Select(row => row.DocumentId), StringComparer.Ordinal));
    }

    private static string What(string documentId, Guid organizationId) => $"The legal hold on document '{documentId}' of organization '{organizationId}'";

    private static bool Standing(LegalHoldRow row, string documentId)
        => row.LiftedAt is null && string.Equals(row.DocumentId, documentId, StringComparison.Ordinal);

    private static LegalHold Hold(LegalHoldRow row)
        => new(row.Id, row.OrganizationId, row.DocumentId, row.Reason, row.PlacedBy, row.PlacedAt, row.LiftedBy, row.LiftedAt);

    private Task<IGenericResult<IReadOnlyList<LegalHoldRow>>> Rows(Guid tenantId, Guid organizationId, CancellationToken cancellationToken)
        => _store.Rows<LegalHoldRow>(_store.Tables.HoldTable, tenantId, organizationId, $"The legal holds of organization '{organizationId}'", cancellationToken);
}
