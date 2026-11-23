using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Extensions;
using Fdw.Data;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Logging;
using Fdw.Services.Etl.Pipelines.Loads;
using Fdw.Services.Pipelines;
using Fdw.Services.Pipelines.Hubs;
using Fdw.Services.Pipelines.Notifications;
using Fdw.Services.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// The pieces a run gives its person (FDW-6140), the rows of <c>etl.PipelineRunStream</c> in the operational store. The row id is the sequence, so a read after a sequence is one range read in order, and a restart
/// writes under the chain's first run so its pieces follow the first run's. Only the person the piece is addressed to reads it, or whoever the host's <see cref="IRunAudience"/> names.
/// </summary>
/// <remarks>
/// A run that acts as no person writes nothing: there is nobody to follow it. Pieces older than the setting <c>RunStreamRetentionSeconds</c> are removed as the next step starts, so the table does not grow without
/// a sweeper of its own; the setting and <c>RunStreamPageSize</c> are required, and a host that sets neither neither writes nor reads.
/// </remarks>
public sealed class RunStream : IRunStream
{
    private const string PathName = "etl";
    private const string ContainerName = "PipelineRunStream";

    private readonly IDataGatewayProvider _gateways;
    private readonly IEffectiveSettingsProvider _settings;
    private readonly IReadOnlyList<IRunAudience> _audiences;
    private readonly TimeProvider _time;
    private readonly IPipelineStatusBroadcaster? _broadcaster;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="RunStream"/> class.</summary>
    /// <param name="gateways">Gives the data gateway the operational store is reached through.</param>
    /// <param name="settings">Reads the retention and the page size.</param>
    /// <param name="audiences">The audiences the host registered; none lets the person alone follow.</param>
    /// <param name="time">The clock a piece is stamped with.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="broadcaster">Pushes each piece written to the hub group of its person, through the relay on a host that serves no hub (FDW-6140); a host with none keeps the pieces and the person reads them from the stream.</param>
    public RunStream(
        IDataGatewayProvider gateways, IEffectiveSettingsProvider settings, IEnumerable<IRunAudience> audiences, TimeProvider time, ILogger<RunStream>? logger = null,
        IPipelineStatusBroadcaster? broadcaster = null)
    {
        _broadcaster = broadcaster;
        _gateways = gateways;
        _settings = settings;
        _audiences = [.. audiences];
        _time = time;
        _logger = logger ?? NullLogger<RunStream>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Write(FlowRunIdentity identity, string kind, string? step, string? text, CancellationToken cancellationToken = default)
    {
        if (identity.PersonId is not { } person)
        {
            return GenericResult.Success();
        }

        var store = PipelineTypes.OperationalConnection;
        if (string.IsNullOrWhiteSpace(store))
        {
            return GenericResult.Failure(RunStreamLog.NoStore(_logger));
        }

        if (!_settings.TryGetEffectiveValue<int>(SettingDefinitions.RunStreamRetentionSeconds, out var retention, identity.TenantId) || retention <= 0)
        {
            return GenericResult.Failure(RunStreamLog.SettingNotSet(_logger, SettingDefinitions.RunStreamRetentionSeconds, "written"));
        }

        var gateway = _gateways.ByName(DataGatewayNames.Main);
        var now = _time.GetUtcNow();
        if (string.Equals(kind, RunStreamKinds.StepStarted, StringComparison.Ordinal))
        {
            // Why swept as a step starts: a step is not a hot path, and the table needs no sweeper of its own to stay within what the setting keeps.
            var swept = await gateway.Execute<int>(
                Delete.From(ContainerName).DataStore(store).Path(PathName).Where("CreatedAt", FilterOperators.ByName("LessThan"), now.AddSeconds(-retention)).Build(), cancellationToken).ConfigureAwait(false);
            if (swept.IsFailure)
            {
                return GenericResult.Failure(RunStreamLog.NotWritten(_logger, kind, identity.ExecutionId, LoadFailureText.Of(swept)));
            }
        }

        var pieceId = Guid.CreateVersion7();
        var inserted = await gateway.Execute<int>(
            Insert.Into<EtlPipelineRunStreamRecord>(ContainerName).DataStore(store).Path(PathName).Value(new EtlPipelineRunStreamRecord
            {
                PieceId = pieceId,
                TenantId = identity.TenantId,
                ClaimRootId = identity.ClaimRootId,
                ExecutionId = identity.ExecutionId,
                PersonId = person,
                Kind = kind,
                StepName = step,
                Text = text,
                CreatedAt = now,
            }),
            cancellationToken).ConfigureAwait(false);
        if (inserted.IsFailure)
        {
            return GenericResult.Failure(RunStreamLog.NotWritten(_logger, kind, identity.ExecutionId, LoadFailureText.Of(inserted)));
        }

        await Push(gateway, store, pieceId, identity, person, (kind, step, text), now, cancellationToken).ConfigureAwait(false);
        return GenericResult.Success();
    }

    // The piece is kept; telling the person is best effort and never fails the step: what is not pushed is read from the stream after the last sequence the person holds. The sequence is the one
    // the table gave the row, read back by the piece's own id.
    private async Task Push(
        IDataGateway gateway, string store, Guid pieceId, FlowRunIdentity identity, Guid person, (string Kind, string? Step, string? Text) piece, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (_broadcaster is not { } broadcaster)
        {
            return;
        }

        var read = await gateway.Execute<IEnumerable<EtlPipelineRunStreamRecord>>(
            Query.From<EtlPipelineRunStreamRecord>(store, PathName, ContainerName).Where(r => r.PieceId).Equal(pieceId).Build(), cancellationToken).ConfigureAwait(false);
        if (read.IsFailure || read.Value?.FirstOrDefault() is not { } kept)
        {
            _ = RunStreamLog.NotPushed(_logger, pieceId, identity.ExecutionId, read.IsFailure ? LoadFailureText.Of(read) : "the piece just written was not found");
            return;
        }

        await broadcaster.BroadcastRunPiece(new PipelineRunPieceUpdate
        {
            ClaimRootId = identity.ClaimRootId,
            PersonId = person,
            Sequence = kept.RowId,
            ExecutionId = identity.ExecutionId,
            Kind = piece.Kind,
            StepName = piece.Step,
            Text = piece.Text,
            CreatedAt = now,
        }).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<RunStreamPiece>>> ReadAfter(
        Guid tenantId, Guid person, Guid claimRootId, long afterSequence, int? take, CancellationToken cancellationToken = default)
    {
        var store = PipelineTypes.OperationalConnection;
        if (string.IsNullOrWhiteSpace(store))
        {
            return GenericResult<IReadOnlyList<RunStreamPiece>>.Failure(RunStreamLog.NoStore(_logger));
        }

        if (!_settings.TryGetEffectiveValue<int>(SettingDefinitions.RunStreamPageSize, out var page, tenantId) || page <= 0)
        {
            return GenericResult<IReadOnlyList<RunStreamPiece>>.Failure(RunStreamLog.SettingNotSet(_logger, SettingDefinitions.RunStreamPageSize, "read"));
        }

        var read = await _gateways.ByName(DataGatewayNames.Main).Execute<IEnumerable<EtlPipelineRunStreamRecord>>(
            Query.From<EtlPipelineRunStreamRecord>(store, PathName, ContainerName)
                .Where(r => r.TenantId).Equal(tenantId)
                .Where(r => r.ClaimRootId).Equal(claimRootId)
                .Where(r => r.RowId).GreaterThan(afterSequence)
                .OrderBy(r => r.RowId)
                .Paging(0, take is { } asked && asked > 0 && asked < page ? asked : page)
                .Build(),
            cancellationToken).ConfigureAwait(false);
        if (read.IsFailure || read.Value is null)
        {
            return GenericResult<IReadOnlyList<RunStreamPiece>>.Failure(RunStreamLog.NotRead(_logger, claimRootId, LoadFailureText.Of(read)));
        }

        var rows = read.Value.ToList();
        if (rows.Count > 0 && rows.Any(row => row.PersonId != person))
        {
            var allowed = await MayFollow(person, tenantId, claimRootId, cancellationToken).ConfigureAwait(false);
            if (!allowed)
            {
                return GenericResult<IReadOnlyList<RunStreamPiece>>.Failure(RunStreamLog.NotYours(_logger, person, claimRootId));
            }
        }

        return GenericResult<IReadOnlyList<RunStreamPiece>>.Success(
            [.. rows.Select(row => new RunStreamPiece(row.RowId, row.ExecutionId, row.Kind, row.StepName, row.Text, row.CreatedAt))]);
    }

    // An audience that cannot answer is not a yes: a person is followed only when one says so.
    private async Task<bool> MayFollow(Guid person, Guid tenantId, Guid claimRootId, CancellationToken cancellationToken)
    {
        foreach (var audience in _audiences)
        {
            var answer = await audience.MayFollow(person, tenantId, claimRootId, cancellationToken).ConfigureAwait(false);
            if (answer.IsSuccess && answer.Value)
            {
                return true;
            }
        }

        return false;
    }
}
