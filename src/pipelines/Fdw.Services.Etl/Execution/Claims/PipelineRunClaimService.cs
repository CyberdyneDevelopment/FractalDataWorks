using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
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
/// The durable claim of pipeline runs (FDW-6629): each call is one of the OpsDb procedures <c>etl.ClaimPipelineRun</c>, <c>etl.RenewPipelineRunClaims</c> and <c>etl.ReleasePipelineRunClaims</c>,
/// run through the data gateway as a command with scalar parameters against the stored procedure container of the operational connection. The keys travel as ONE json text the procedure reads.
/// </summary>
/// <remarks>
/// The service writes no SQL and no value of its own: the numbers come from <see cref="PipelineRunClaimSettings"/>, the host names the operational connection, and an answer it cannot read is a
/// coded failure and never a claim. The procedures answer rows of different shapes in one list (the connection reads every result set); a row with a <c>TenantId</c> is a lapsed claim, a row with
/// <c>Ahead</c> is the state of a claim, a row with <c>Renewed</c> a renewal and any other row a promotion.
/// </remarks>
public sealed class PipelineRunClaimService : IPipelineRunClaimService
{
    private const string ClaimProcedure = "ClaimPipelineRun";
    private const string RenewProcedure = "RenewPipelineRunClaims";
    private const string ReleaseProcedure = "ReleasePipelineRunClaims";

    private readonly IDataGateway _gateway;
    private readonly string? _operationalConnection;
    private readonly PipelineRunClaimSettings _settings;
    private readonly ILogger _logger;
    private readonly IPipelineLapsedClaims? _lapsed;

    /// <summary>Initializes a new instance of the <see cref="PipelineRunClaimService"/> class.</summary>
    /// <param name="gateway">The gateway that runs the procedures.</param>
    /// <param name="operationalConnection">The name of the operational connection the host names; null when it names none.</param>
    /// <param name="settings">The four numbers of the claim.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="lapsed">Takes the lapsed claims every procedure answers (FDW-6986); null for a caller that has nothing to do with them.</param>
    public PipelineRunClaimService(
        IDataGateway gateway, string? operationalConnection, PipelineRunClaimSettings settings, ILogger<PipelineRunClaimService>? logger = null, IPipelineLapsedClaims? lapsed = null)
    {
        _lapsed = lapsed;
        _gateway = gateway;
        _operationalConnection = operationalConnection;
        _settings = settings;
        _logger = logger ?? NullLogger<PipelineRunClaimService>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<PipelineRunClaimOutcome>> Claim(PipelineRunClaimRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Keys.Count == 0)
            return GenericResult<PipelineRunClaimOutcome>.Success(new PipelineRunClaimOutcome(request.ExecutionId, PipelineRunClaimStates.ByName("Claimed"), 0, []));

        if (_operationalConnection is not { Length: > 0 } connection)
            return GenericResult<PipelineRunClaimOutcome>.Failure(PipelineRunClaimLog.OperationalConnectionNotNamed(_logger, ClaimProcedure));

        var answered = await Run(
            connection,
            ClaimProcedure,
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["TenantId"] = request.TenantId,
                ["ExecutionId"] = request.ExecutionId,
                ["PipelineId"] = request.PipelineId,
                ["Keys"] = JsonSerializer.Serialize(request.Keys.Select(key => new { keyType = key.Type.Name, claimKey = key.ClaimKey, cap = key.Cap })),
                ["ExecutingHost"] = request.ExecutingHost,
                ["ExecutingSlot"] = request.ExecutingSlot,
                ["LeaseSeconds"] = _settings.LeaseSeconds,
                ["LockTimeoutMilliseconds"] = _settings.LockTimeoutMilliseconds,
            },
            cancellationToken).ConfigureAwait(false);
        if (answered.IsFailure || answered.Value is not { } rows)
            return answered.ToNewResult<PipelineRunClaimOutcome>();

        if (rows.FirstOrDefault(row => row.ContainsKey("Ahead")) is not { } stateRow)
            return GenericResult<PipelineRunClaimOutcome>.Failure(PipelineRunClaimLog.ProcedureAnsweredNoState(_logger, ClaimProcedure, request.ExecutionId));

        var stateText = Text(stateRow, "State");
        var state = PipelineRunClaimStates.ByName(stateText);
        if (ReferenceEquals(state, PipelineRunClaimStates.NotFound))
            return GenericResult<PipelineRunClaimOutcome>.Failure(PipelineRunClaimLog.ProcedureAnsweredUnknownState(_logger, ClaimProcedure, request.ExecutionId, stateText));

        var lapsed = Lapsed(rows, ClaimProcedure);
        if (lapsed.IsFailure || lapsed.Value is not { } lapsedClaims)
            return lapsed.ToNewResult<PipelineRunClaimOutcome>();

        _lapsed?.Seen(lapsedClaims);

        return GenericResult<PipelineRunClaimOutcome>.Success(new PipelineRunClaimOutcome(
            request.ExecutionId,
            state,
            Convert.ToInt32(stateRow["Ahead"], CultureInfo.InvariantCulture),
            lapsedClaims));
    }

    /// <inheritdoc />
    public async Task<IGenericResult<PipelineRunRenewed>> Renew(IReadOnlyList<Guid> executionIds, string executingHost, string? executingSlot, CancellationToken cancellationToken = default)
    {
        if (_operationalConnection is not { Length: > 0 } connection)
            return GenericResult<PipelineRunRenewed>.Failure(PipelineRunClaimLog.OperationalConnectionNotNamed(_logger, RenewProcedure));

        var answered = await Run(
            connection,
            RenewProcedure,
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["ExecutionIds"] = JsonSerializer.Serialize(executionIds),
                ["ExecutingHost"] = executingHost,
                ["ExecutingSlot"] = executingSlot,
                ["LeaseSeconds"] = _settings.LeaseSeconds,
                ["LockTimeoutMilliseconds"] = _settings.LockTimeoutMilliseconds,
            },
            cancellationToken).ConfigureAwait(false);
        if (answered.IsFailure || answered.Value is not { } rows)
            return answered.ToNewResult<PipelineRunRenewed>();

        var lapsed = Lapsed(rows, RenewProcedure);
        if (lapsed.IsFailure || lapsed.Value is not { } lapsedClaims)
            return lapsed.ToNewResult<PipelineRunRenewed>();

        _lapsed?.Seen(lapsedClaims);

        return GenericResult<PipelineRunRenewed>.Success(new PipelineRunRenewed(
            [.. rows.Where(row => row.ContainsKey("Renewed")).Select(row => new PipelineRunRenewal(Id(row), Convert.ToInt32(row["Renewed"], CultureInfo.InvariantCulture) != 0))],
            lapsedClaims));
    }

    /// <inheritdoc />
    public async Task<IGenericResult<PipelineRunReleased>> Release(Guid executionId, CancellationToken cancellationToken = default)
    {
        if (_operationalConnection is not { Length: > 0 } connection)
            return GenericResult<PipelineRunReleased>.Failure(PipelineRunClaimLog.OperationalConnectionNotNamed(_logger, ReleaseProcedure));

        var answered = await Run(
            connection,
            ReleaseProcedure,
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["ExecutionId"] = executionId,
                ["LockTimeoutMilliseconds"] = _settings.LockTimeoutMilliseconds,
            },
            cancellationToken).ConfigureAwait(false);
        if (answered.IsFailure || answered.Value is not { } rows)
            return answered.ToNewResult<PipelineRunReleased>();

        var lapsed = Lapsed(rows, ReleaseProcedure);
        if (lapsed.IsFailure || lapsed.Value is not { } lapsedClaims)
            return lapsed.ToNewResult<PipelineRunReleased>();

        _lapsed?.Seen(lapsedClaims);

        return GenericResult<PipelineRunReleased>.Success(new PipelineRunReleased(
            [.. rows.Where(row => !row.ContainsKey("TenantId")).Select(row => new PipelinePromotedRun(Id(row), Text(row, "ExecutingHost"), NullableText(row, "ExecutingSlot")))],
            lapsedClaims));
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

    private IGenericResult<IReadOnlyList<PipelineRunLapsedClaim>> Lapsed(List<Dictionary<string, object?>> rows, string procedure)
    {
        var lapsed = new List<PipelineRunLapsedClaim>();
        foreach (var row in rows.Where(row => row.ContainsKey("TenantId")))
        {
            if (row["TenantId"] is not Guid tenant || row["PipelineId"] is not Guid pipeline)
                return GenericResult<IReadOnlyList<PipelineRunLapsedClaim>>.Failure(
                    PipelineRunClaimLog.AnswerNotReadable(_logger, procedure, "TenantId or PipelineId", "it is not a Guid"));

            lapsed.Add(new PipelineRunLapsedClaim(Id(row), tenant, pipeline, Text(row, "ExecutingHost"), NullableText(row, "ExecutingSlot"), Text(row, "State")));
        }

        return GenericResult<IReadOnlyList<PipelineRunLapsedClaim>>.Success(lapsed);
    }

    private static Guid Id(Dictionary<string, object?> row) => Guid.Parse(Text(row, "ExecutionId"));

    private static string Text(Dictionary<string, object?> row, string column) => Convert.ToString(row[column], CultureInfo.InvariantCulture) ?? string.Empty;

    private static string? NullableText(Dictionary<string, object?> row, string column) => row[column] is null or DBNull ? null : Convert.ToString(row[column], CultureInfo.InvariantCulture);
}
