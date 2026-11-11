using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Commands.Data;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Operations.Data;
using Fdw.Operations.Logging;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Operations.Escalation;

/// <summary>
/// The rows of ops.EscalationAlert, read and written through the data gateway (FDW-3863), in the store the host names for its operational data.
/// </summary>
/// <remarks>
/// Why the store is a constructor value that may be null: every host registers Operations as part of its bootstrap, including hosts that never keep an alert. A host that names
/// no store is not misconfigured for that; each call reports it instead.
/// </remarks>
internal sealed class EscalationAlertStore : IEscalationAlertStore
{
    private const string PathName = "ops";
    private const string ContainerName = "EscalationAlert";

    private readonly IDataGatewayProvider _dataGateways;
    private readonly string? _dataStoreName;
    private readonly ILogger _logger;

    public EscalationAlertStore(IDataGatewayProvider dataGateways, string? dataStoreName, ILogger? logger = null)
    {
        _dataGateways = dataGateways;
        _dataStoreName = dataStoreName;
        _logger = logger ?? NullLogger.Instance;
    }

    // Resolved at the call, not held: the gateway is scoped and this store may outlive the scope it was made in.
    private IDataGateway Gateway => _dataGateways.ByName(DataGatewayNames.Main);

    private bool StoreIsNamed => !string.IsNullOrWhiteSpace(_dataStoreName);

    private DataStoreTarget Target => new(_dataStoreName!, PathName, ContainerName);

    public async Task<IGenericResult> Add(EscalationAlertRow alert, CancellationToken cancellationToken = default)
    {
        if (!StoreIsNamed)
        {
            return GenericResult.Failure(EscalationAlertLog.StoreNotNamed(_logger));
        }

        var inserted = await Gateway.Execute<int>(new InsertCommand<EscalationAlertRow>(alert), Target, cancellationToken).ConfigureAwait(false);
        return inserted.IsSuccess ? GenericResult.Success() : GenericResult.Failure(EscalationAlertLog.AlertNotKept(_logger, alert.Id, inserted.CurrentMessage));
    }

    public async Task<IGenericResult> Update(EscalationAlertRow alert, CancellationToken cancellationToken = default)
    {
        if (!StoreIsNamed)
        {
            return GenericResult.Failure(EscalationAlertLog.StoreNotNamed(_logger));
        }

        var updated = await Gateway.Execute<int>(new UpdateCommand<EscalationAlertRow>(alert) { Filter = IdIs(alert.Id) }, Target, cancellationToken).ConfigureAwait(false);
        return updated.IsSuccess ? GenericResult.Success() : GenericResult.Failure(EscalationAlertLog.AlertNotKept(_logger, alert.Id, updated.CurrentMessage));
    }

    public async Task<IGenericResult<EscalationAlertRow?>> Find(Guid id, CancellationToken cancellationToken = default)
    {
        var found = await Read(new QueryCommand<EscalationAlertRow> { Filter = IdIs(id) }, cancellationToken).ConfigureAwait(false);
        return found.IsFailure || found.Value is null
            ? found.ToNewResult<EscalationAlertRow?>()
            : GenericResult<EscalationAlertRow?>.Success(found.Value.Count > 0 ? found.Value[0] : null);
    }

    public async Task<IGenericResult<IReadOnlyList<EscalationAlertRow>>> OfThreshold(
        Guid tenantId, string subjectKind, string subjectName, string thresholdName, CancellationToken cancellationToken = default)
    {
        var found = await Read(new QueryCommand<EscalationAlertRow> { Filter = Equal("ThresholdName", thresholdName) }, cancellationToken).ConfigureAwait(false);
        return found.IsFailure || found.Value is null
            ? found
            : GenericResult<IReadOnlyList<EscalationAlertRow>>.Success(
                [.. found.Value
                    .Where(row => row.TenantId == tenantId
                        && string.Equals(row.SubjectKind, subjectKind, StringComparison.Ordinal)
                        && string.Equals(row.SubjectName, subjectName, StringComparison.Ordinal))
                    .OrderByDescending(row => row.FiredAt)]);
    }

    public async Task<IGenericResult<IReadOnlyList<EscalationAlertRow>>> List(string? state, CancellationToken cancellationToken = default)
    {
        var command = state is null ? new QueryCommand<EscalationAlertRow>() : new QueryCommand<EscalationAlertRow> { Filter = Equal("State", state) };
        var found = await Read(command, cancellationToken).ConfigureAwait(false);
        return found.IsFailure || found.Value is null
            ? found
            : GenericResult<IReadOnlyList<EscalationAlertRow>>.Success([.. found.Value.OrderByDescending(row => row.FiredAt)]);
    }

    private async Task<IGenericResult<IReadOnlyList<EscalationAlertRow>>> Read(QueryCommand<EscalationAlertRow> command, CancellationToken cancellationToken)
    {
        if (!StoreIsNamed)
        {
            return GenericResult<IReadOnlyList<EscalationAlertRow>>.Failure(EscalationAlertLog.StoreNotNamed(_logger));
        }

        var result = await Gateway.Execute<IEnumerable<EscalationAlertRow>>(command, Target, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess
            ? GenericResult<IReadOnlyList<EscalationAlertRow>>.Success([.. result.Value ?? []])
            : GenericResult<IReadOnlyList<EscalationAlertRow>>.Failure(EscalationAlertLog.AlertsNotRead(_logger, result.CurrentMessage));
    }

    private static FilterExpression IdIs(Guid id) => Equal("Id", id);

    private static FilterExpression Equal(string property, object value) => new()
    {
        Root = new FilterCondition { PropertyName = property, Operator = FilterOperators.ByName("Equal"), Value = value }
    };
}
