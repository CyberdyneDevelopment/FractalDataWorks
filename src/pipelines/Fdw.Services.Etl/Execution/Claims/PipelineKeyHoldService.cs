using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Execution.Claims;

/// <summary>
/// The hold on a key (FDW-6939): each call is one of the OpsDb procedures <c>etl.PlaceKeyHold</c> and <c>etl.ReleaseKeyHold</c>, run through the data gateway as a command with scalar parameters against the
/// stored procedure container of the operational connection. The key is built by <see cref="PipelineRunClaimKeys.Stated"/>, the same text the claim of a run states for it.
/// </summary>
/// <remarks>The service writes no SQL and no value of its own: the lock wait comes from <see cref="PipelineRunClaimSettings"/>, the host names the operational connection, and an answer it cannot read is a coded failure.</remarks>
public sealed class PipelineKeyHoldService : IPipelineKeyHolds
{
    private const string PlaceProcedure = "PlaceKeyHold";
    private const string ReleaseProcedure = "ReleaseKeyHold";

    private readonly IDataGateway _gateway;
    private readonly string? _operationalConnection;
    private readonly PipelineRunClaimSettings _settings;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="PipelineKeyHoldService"/> class.</summary>
    /// <param name="gateway">The gateway that runs the procedures.</param>
    /// <param name="operationalConnection">The name of the operational connection the host names; null when it names none.</param>
    /// <param name="settings">The settings of the claim, of which the lock wait is used.</param>
    /// <param name="logger">The logger.</param>
    public PipelineKeyHoldService(IDataGateway gateway, string? operationalConnection, PipelineRunClaimSettings settings, ILogger<PipelineKeyHoldService>? logger = null)
    {
        _gateway = gateway;
        _operationalConnection = operationalConnection;
        _settings = settings;
        _logger = logger ?? NullLogger<PipelineKeyHoldService>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<PipelineKeyHold>> Place(Guid tenantId, string key, Guid heldBy, string reason, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return GenericResult<PipelineKeyHold>.Failure(PipelineRunClaimLog.HoldReasonMissing(_logger, key));

        if (_operationalConnection is not { Length: > 0 } connection)
            return GenericResult<PipelineKeyHold>.Failure(PipelineRunClaimLog.OperationalConnectionNotNamed(_logger, PlaceProcedure));

        var stated = PipelineRunClaimKeys.Stated(tenantId, key, _logger);
        if (stated.IsFailure || stated.Value is not { } held)
            return stated.ToNewResult<PipelineKeyHold>();

        var answered = await Run(
            connection,
            PlaceProcedure,
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["TenantId"] = tenantId,
                ["ClaimKey"] = held.ClaimKey,
                ["HeldBy"] = heldBy,
                ["Reason"] = reason,
                ["LockTimeoutMilliseconds"] = _settings.LockTimeoutMilliseconds,
            },
            cancellationToken).ConfigureAwait(false);
        if (answered.IsFailure || answered.Value is not { } rows)
            return answered.ToNewResult<PipelineKeyHold>();

        if (rows.FirstOrDefault(row => row.ContainsKey("Placed")) is not { } stateRow)
            return GenericResult<PipelineKeyHold>.Failure(PipelineRunClaimLog.AnswerNotReadable(_logger, PlaceProcedure, "Placed", "the procedure answered no row"));

        if (stateRow["HeldBy"] is not Guid standingHolder || stateRow["HeldAt"] is not DateTimeOffset at)
            return GenericResult<PipelineKeyHold>.Failure(PipelineRunClaimLog.AnswerNotReadable(_logger, PlaceProcedure, "HeldBy or HeldAt", "it is not a Guid and a time"));

        var standing = new PipelineKeyHold(standingHolder, at, Text(stateRow, "Reason"));
        return Convert.ToBoolean(stateRow["Placed"], CultureInfo.InvariantCulture)
            ? GenericResult<PipelineKeyHold>.Success(standing)
            : GenericResult<PipelineKeyHold>.Failure(PipelineRunClaimLog.KeyAlreadyHeld(_logger, key, standing.HeldBy, standing.HeldAt.ToString("O", CultureInfo.InvariantCulture), standing.Reason));
    }

    /// <inheritdoc />
    public async Task<IGenericResult<PipelineKeyHoldReleased>> Release(Guid tenantId, string key, CancellationToken cancellationToken = default)
    {
        if (_operationalConnection is not { Length: > 0 } connection)
            return GenericResult<PipelineKeyHoldReleased>.Failure(PipelineRunClaimLog.OperationalConnectionNotNamed(_logger, ReleaseProcedure));

        var stated = PipelineRunClaimKeys.Stated(tenantId, key, _logger);
        if (stated.IsFailure || stated.Value is not { } held)
            return stated.ToNewResult<PipelineKeyHoldReleased>();

        var answered = await Run(
            connection,
            ReleaseProcedure,
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["TenantId"] = tenantId,
                ["ClaimKey"] = held.ClaimKey,
                ["LockTimeoutMilliseconds"] = _settings.LockTimeoutMilliseconds,
            },
            cancellationToken).ConfigureAwait(false);
        if (answered.IsFailure || answered.Value is not { } rows)
            return answered.ToNewResult<PipelineKeyHoldReleased>();

        if (rows.FirstOrDefault(row => row.ContainsKey("Released")) is not { } releasedRow)
            return GenericResult<PipelineKeyHoldReleased>.Failure(PipelineRunClaimLog.AnswerNotReadable(_logger, ReleaseProcedure, "Released", "the procedure answered no row"));

        if (!Convert.ToBoolean(releasedRow["Released"], CultureInfo.InvariantCulture))
            return GenericResult<PipelineKeyHoldReleased>.Failure(PipelineRunClaimLog.KeyNotHeld(_logger, key));

        if (releasedRow["HeldBy"] is not Guid placedBy)
            return GenericResult<PipelineKeyHoldReleased>.Failure(PipelineRunClaimLog.AnswerNotReadable(_logger, ReleaseProcedure, "HeldBy", "it is not a Guid"));

        var promoted = rows.Where(row => !row.ContainsKey("Released"))
            .Select(row => new PipelinePromotedRun(Guid.Parse(Text(row, "ExecutionId")), Text(row, "ExecutingHost"), row["ExecutingSlot"] is null or DBNull ? null : Text(row, "ExecutingSlot")))
            .ToList();
        return GenericResult<PipelineKeyHoldReleased>.Success(new PipelineKeyHoldReleased(placedBy, promoted));
    }

    private async Task<IGenericResult<List<Dictionary<string, object?>>>> Run(
        string connection, string procedure, IReadOnlyDictionary<string, object?> parameters, CancellationToken cancellationToken)
    {
        var ran = await _gateway.Execute<IEnumerable<Dictionary<string, object?>>>(
            new ExecuteProcedureCommand<Dictionary<string, object?>> { Parameters = parameters },
            new DataStoreTarget(connection, "etl", procedure),
            cancellationToken).ConfigureAwait(false);

        return ran.IsFailure || ran.Value is not { } rows
            ? ran.ToNewResult<List<Dictionary<string, object?>>>()
            : GenericResult<List<Dictionary<string, object?>>>.Success(new List<Dictionary<string, object?>>(rows));
    }

    private static string Text(Dictionary<string, object?> row, string column) => Convert.ToString(row[column], CultureInfo.InvariantCulture) ?? string.Empty;
}
