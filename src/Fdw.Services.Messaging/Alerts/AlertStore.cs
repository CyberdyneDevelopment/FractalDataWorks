using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Messaging.Abstractions;
using Fdw.Services.Messaging.Logging;
using Microsoft.Extensions.Logging;
using CmdBuilders = Fdw.Commands.Data.Extensions;

namespace Fdw.Services.Messaging;

/// <summary>
/// Reads and writes the two msg tables of what was done to alerts through the data gateway (FDW-2420). It decides nothing: which actions an alert offers, who may
/// take them and what a time means are the service's.
/// </summary>
internal sealed class AlertStore
{
    // The row's own name, not a store choice: see MessageService's identical constant.
    private const string MessagingServiceName = "Messaging";
    private const string ActionContainer = "AlertAction";
    private const string EveryoneActionContainer = "AlertEveryoneAction";
    private const string KindSettingContainer = "AlertKindSetting";
    private const string KindSettingActionContainer = "AlertKindSettingAction";

    private readonly ILogger _logger;
    private readonly IDataGatewayProvider _dataGateways;
    private readonly IMessagingConfigurationProvider _messaging;

    /// <summary>
    /// Initializes a new instance of the <see cref="AlertStore"/> class.
    /// </summary>
    /// <param name="logger">The logger a failure is written to.</param>
    /// <param name="dataGateways">Resolves the data gateway when a call is made.</param>
    /// <param name="messaging">Resolves where this deployment keeps its messaging data.</param>
    public AlertStore(ILogger logger, IDataGatewayProvider dataGateways, IMessagingConfigurationProvider messaging)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dataGateways = dataGateways ?? throw new ArgumentNullException(nameof(dataGateways));
        _messaging = messaging ?? throw new ArgumentNullException(nameof(messaging));
    }

    // Why resolved here rather than injected: the gateway is scoped and the service is not, so holding one would be a captive dependency.
    private IDataGateway Gateway => _dataGateways.ByName("Main");

    /// <summary>Reads the actions a person has taken on some alerts of one kind.</summary>
    /// <param name="kind">The kind of alert.</param>
    /// <param name="ids">The alerts' identifiers; not more than the store takes in one condition.</param>
    /// <param name="userId">The person.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The person's rows for those alerts.</returns>
    public async Task<IGenericResult<IReadOnlyList<AlertActionRow>>> FindActions(string kind, IReadOnlyCollection<Guid> ids, Guid userId, CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
            return GenericResult<IReadOnlyList<AlertActionRow>>.Success([]);

        var location = await Location(cancellationToken).ConfigureAwait(false);
        if (!location.IsSuccess)
            return location.ToNewResult<IReadOnlyList<AlertActionRow>>();

        var command = Query.From<AlertActionRow>(location.Value.DataStoreName, location.Value.PathName, ActionContainer)
            .Where(row => row.Kind).Equal(kind)
            .Where(row => row.UserId).Equal(userId)
            .Where(row => row.AlertId).In(ids)
            .Build();

        var rows = await Gateway.Execute<IEnumerable<AlertActionRow>>(command, cancellationToken).ConfigureAwait(false);
        return rows.IsSuccess && rows.Value is { } found
            ? GenericResult<IReadOnlyList<AlertActionRow>>.Success(found.ToList())
            : Failed<IReadOnlyList<AlertActionRow>>(rows, "read the actions on");
    }

    /// <summary>Reads the actions taken for everyone on some alerts of one kind.</summary>
    /// <param name="kind">The kind of alert.</param>
    /// <param name="ids">The alerts' identifiers; not more than the store takes in one condition.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The rows for those alerts.</returns>
    public async Task<IGenericResult<IReadOnlyList<AlertEveryoneActionRow>>> FindEveryoneActions(string kind, IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
            return GenericResult<IReadOnlyList<AlertEveryoneActionRow>>.Success([]);

        var location = await Location(cancellationToken).ConfigureAwait(false);
        if (!location.IsSuccess)
            return location.ToNewResult<IReadOnlyList<AlertEveryoneActionRow>>();

        var command = Query.From<AlertEveryoneActionRow>(location.Value.DataStoreName, location.Value.PathName, EveryoneActionContainer)
            .Where(row => row.Kind).Equal(kind)
            .Where(row => row.AlertId).In(ids)
            .Build();

        var rows = await Gateway.Execute<IEnumerable<AlertEveryoneActionRow>>(command, cancellationToken).ConfigureAwait(false);
        return rows.IsSuccess && rows.Value is { } found
            ? GenericResult<IReadOnlyList<AlertEveryoneActionRow>>.Success(found.ToList())
            : Failed<IReadOnlyList<AlertEveryoneActionRow>>(rows, "read the actions for everyone on");
    }

    /// <summary>Reads which of some alerts a person has taken one named action on.</summary>
    /// <param name="kind">The kind of alert.</param>
    /// <param name="ids">The alerts' identifiers; not more than the store takes in one condition.</param>
    /// <param name="userId">The person.</param>
    /// <param name="action">The action's name.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The identifiers of the alerts the person has taken it on.</returns>
    public async Task<IGenericResult<HashSet<Guid>>> FindAlertsActedOn(string kind, IReadOnlyCollection<Guid> ids, Guid userId, string action, CancellationToken cancellationToken)
    {
        var rows = await FindActions(kind, ids, userId, cancellationToken).ConfigureAwait(false);
        return rows.IsSuccess
            ? GenericResult<HashSet<Guid>>.Success(rows.Value!.Where(row => string.Equals(row.Action, action, StringComparison.OrdinalIgnoreCase)).Select(row => row.AlertId).ToHashSet())
            : rows.ToNewResult<HashSet<Guid>>();
    }

    /// <summary>Keeps a person's action on an alert, replacing the action they already took if any.</summary>
    /// <param name="row">The action.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Success when the row was written.</returns>
    public async Task<IGenericResult> Put(AlertActionRow row, CancellationToken cancellationToken)
    {
        var location = await Location(cancellationToken).ConfigureAwait(false);
        if (!location.IsSuccess)
            return (IGenericResult)location;

        var removed = await Execute(DeleteOne(row.Kind, row.AlertId, row.UserId, row.Action, location.Value), "replace the action on", row.AlertId, cancellationToken).ConfigureAwait(false);
        if (!removed.IsSuccess)
            return removed;

        return await Execute(
            CmdBuilders.Insert.Into<AlertActionRow>(ActionContainer).DataStore(location.Value.DataStoreName).Path(location.Value.PathName).Value(row),
            "act on", row.AlertId, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Keeps an action taken for everyone on an alert, replacing the one already taken if any.</summary>
    /// <param name="row">The action.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Success when the row was written.</returns>
    public async Task<IGenericResult> PutForEveryone(AlertEveryoneActionRow row, CancellationToken cancellationToken)
    {
        var location = await Location(cancellationToken).ConfigureAwait(false);
        if (!location.IsSuccess)
            return (IGenericResult)location;

        var removed = await Execute(
            CmdBuilders.Delete.From(EveryoneActionContainer).DataStore(location.Value.DataStoreName).Path(location.Value.PathName)
                .Where(nameof(AlertEveryoneActionRow.Kind), row.Kind).Where(nameof(AlertEveryoneActionRow.AlertId), row.AlertId).Where(nameof(AlertEveryoneActionRow.Action), row.Action).Build(),
            "replace the action for everyone on", row.AlertId, cancellationToken).ConfigureAwait(false);
        if (!removed.IsSuccess)
            return removed;

        return await Execute(
            CmdBuilders.Insert.Into<AlertEveryoneActionRow>(EveryoneActionContainer).DataStore(location.Value.DataStoreName).Path(location.Value.PathName).Value(row),
            "act for everyone on", row.AlertId, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Takes back a person's action on an alert.</summary>
    /// <param name="kind">The kind of alert.</param>
    /// <param name="id">The alert's identifier.</param>
    /// <param name="userId">The person.</param>
    /// <param name="action">The action's name.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Success when the row is gone, also when there was none.</returns>
    public async Task<IGenericResult> Remove(string kind, Guid id, Guid userId, string action, CancellationToken cancellationToken)
    {
        var location = await Location(cancellationToken).ConfigureAwait(false);
        if (!location.IsSuccess)
            return (IGenericResult)location;

        return await Execute(DeleteOne(kind, id, userId, action, location.Value), "take back the action on", id, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Removes everything done to an alert, for the person and for everyone, when the alert itself is deleted.</summary>
    /// <param name="kind">The kind of alert.</param>
    /// <param name="id">The alert's identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Success when every row is gone.</returns>
    public async Task<IGenericResult> RemoveAllFor(string kind, Guid id, CancellationToken cancellationToken)
    {
        var location = await Location(cancellationToken).ConfigureAwait(false);
        if (!location.IsSuccess)
            return (IGenericResult)location;

        var personal = await Execute(
            CmdBuilders.Delete.From(ActionContainer).DataStore(location.Value.DataStoreName).Path(location.Value.PathName)
                .Where(nameof(AlertActionRow.Kind), kind).Where(nameof(AlertActionRow.AlertId), id).Build(),
            "delete the actions on", id, cancellationToken).ConfigureAwait(false);
        if (!personal.IsSuccess)
            return personal;

        return await Execute(
            CmdBuilders.Delete.From(EveryoneActionContainer).DataStore(location.Value.DataStoreName).Path(location.Value.PathName)
                .Where(nameof(AlertEveryoneActionRow.Kind), kind).Where(nameof(AlertEveryoneActionRow.AlertId), id).Build(),
            "delete the actions for everyone on", id, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Reads the actions a tenant chose for each kind of alert it configured.</summary>
    /// <param name="tenantId">The tenant.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The chosen actions of each configured kind, in order; a kind the tenant did not configure has no entry.</returns>
    public async Task<IGenericResult<Dictionary<string, List<string>>>> FindKindSettings(Guid tenantId, CancellationToken cancellationToken)
    {
        var location = await Location(cancellationToken).ConfigureAwait(false);
        if (!location.IsSuccess)
            return location.ToNewResult<Dictionary<string, List<string>>>();

        var settingsCommand = Query.From<AlertKindSettingRow>(location.Value.DataStoreName, location.Value.PathName, KindSettingContainer)
            .Where(row => row.TenantId).Equal(tenantId)
            .Build();
        var settings = await Gateway.Execute<IEnumerable<AlertKindSettingRow>>(settingsCommand, cancellationToken).ConfigureAwait(false);
        if (!settings.IsSuccess || settings.Value is null)
            return Failed<Dictionary<string, List<string>>>(settings, "read the settings of the kinds of");

        var actionsCommand = Query.From<AlertKindSettingActionRow>(location.Value.DataStoreName, location.Value.PathName, KindSettingActionContainer)
            .Where(row => row.TenantId).Equal(tenantId)
            .Build();
        var actions = await Gateway.Execute<IEnumerable<AlertKindSettingActionRow>>(actionsCommand, cancellationToken).ConfigureAwait(false);
        if (!actions.IsSuccess || actions.Value is null)
            return Failed<Dictionary<string, List<string>>>(actions, "read the chosen actions of the kinds of");

        var chosen = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var setting in settings.Value)
            chosen[setting.Kind] = [];

        // Why ordered here and not trusted to the query: the order is the administrator's, and a setting without a row for its kind would be read as no setting.
        foreach (var action in actions.Value.OrderBy(row => row.Ordinal))
        {
            if (chosen.TryGetValue(action.Kind, out var list))
                list.Add(action.Action);
        }

        return GenericResult<Dictionary<string, List<string>>>.Success(chosen);
    }

    /// <summary>Keeps a tenant's choice of the actions a kind of alert offers, replacing the choice it had.</summary>
    /// <param name="setting">Who chose, and when.</param>
    /// <param name="actions">The chosen actions' names, in order; none means the kind offers nothing.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Success when the choice was written; when part of it could not be, the tenant has no choice for the kind and the kind's own list applies.</returns>
    public async Task<IGenericResult> SaveKindSetting(AlertKindSettingRow setting, IReadOnlyList<string> actions, CancellationToken cancellationToken)
    {
        var location = await Location(cancellationToken).ConfigureAwait(false);
        if (!location.IsSuccess)
            return (IGenericResult)location;

        var removed = await RemoveKindSettingRows(setting.TenantId, setting.Kind, location.Value, cancellationToken).ConfigureAwait(false);
        if (!removed.IsSuccess)
            return removed;

        var inserted = await Execute(
            CmdBuilders.Insert.Into<AlertKindSettingRow>(KindSettingContainer).DataStore(location.Value.DataStoreName).Path(location.Value.PathName).Value(setting),
            "save the setting of a kind of", setting.TenantId, cancellationToken).ConfigureAwait(false);
        if (!inserted.IsSuccess)
            return inserted;

        for (var ordinal = 0; ordinal < actions.Count; ordinal++)
        {
            var row = new AlertKindSettingActionRow { TenantId = setting.TenantId, Kind = setting.Kind, Action = actions[ordinal], Ordinal = ordinal };
            var kept = await Execute(
                CmdBuilders.Insert.Into<AlertKindSettingActionRow>(KindSettingActionContainer).DataStore(location.Value.DataStoreName).Path(location.Value.PathName).Value(row),
                "save an action of the setting of a kind of", setting.TenantId, cancellationToken).ConfigureAwait(false);
            if (!kept.IsSuccess)
            {
                var undone = await RemoveKindSettingRows(setting.TenantId, setting.Kind, location.Value, cancellationToken).ConfigureAwait(false);
                if (!undone.IsSuccess)
                    MessagingLog.AlertStoreFailed(_logger, "undo a half-saved setting of a kind of alert", undone.CurrentMessage);

                return kept;
            }
        }

        return GenericResult.Success();
    }

    /// <summary>Removes a tenant's choice for a kind of alert, so the kind's own list applies again.</summary>
    /// <param name="tenantId">The tenant.</param>
    /// <param name="kind">The kind of alert.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Success when the choice is gone, also when there was none.</returns>
    public async Task<IGenericResult> RemoveKindSetting(Guid tenantId, string kind, CancellationToken cancellationToken)
    {
        var location = await Location(cancellationToken).ConfigureAwait(false);
        if (!location.IsSuccess)
            return (IGenericResult)location;

        return await RemoveKindSettingRows(tenantId, kind, location.Value, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IGenericResult> RemoveKindSettingRows(Guid tenantId, string kind, (string DataStoreName, string PathName) location, CancellationToken cancellationToken)
    {
        var actions = await Execute(
            CmdBuilders.Delete.From(KindSettingActionContainer).DataStore(location.DataStoreName).Path(location.PathName)
                .Where(nameof(AlertKindSettingActionRow.TenantId), tenantId).Where(nameof(AlertKindSettingActionRow.Kind), kind).Build(),
            "delete the chosen actions of a kind of", tenantId, cancellationToken).ConfigureAwait(false);
        if (!actions.IsSuccess)
            return actions;

        return await Execute(
            CmdBuilders.Delete.From(KindSettingContainer).DataStore(location.DataStoreName).Path(location.PathName)
                .Where(nameof(AlertKindSettingRow.TenantId), tenantId).Where(nameof(AlertKindSettingRow.Kind), kind).Build(),
            "delete the setting of a kind of", tenantId, cancellationToken).ConfigureAwait(false);
    }

    private static DataGatewayCall DeleteOne(string kind, Guid id, Guid userId, string action, (string DataStoreName, string PathName) location)
        => CmdBuilders.Delete.From(ActionContainer).DataStore(location.DataStoreName).Path(location.PathName)
            .Where(nameof(AlertActionRow.Kind), kind).Where(nameof(AlertActionRow.AlertId), id).Where(nameof(AlertActionRow.UserId), userId).Where(nameof(AlertActionRow.Action), action).Build();

    private async Task<IGenericResult> Execute(DataGatewayCall command, string action, Guid id, CancellationToken cancellationToken)
    {
        var result = await Gateway.Execute<int>(command, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? GenericResult.Success() : FailedWrite(result, action, id);
    }

    private IGenericResult FailedWrite(IGenericResult result, string action, Guid id)
        => result.Messages.Any()
            ? result
            : GenericResult.Failure(MessagingLog.AlertStoreFailed(_logger, $"{action} alert {id:D}", "the command failed"));

    private IGenericResult<T> Failed<T>(IGenericResult result, string action)
        => result.Messages.Any()
            ? result.ToNewResult<T>()
            : GenericResult<T>.Failure(MessagingLog.AlertStoreFailed(_logger, $"{action} alerts", "the command failed"));

    private async Task<IGenericResult<(string DataStoreName, string PathName)>> Location(CancellationToken cancellationToken)
    {
        var messaging = await _messaging.Get(MessagingServiceName, cancellationToken).ConfigureAwait(false);
        if (!messaging.IsSuccess)
            return messaging.ToNewResult<(string, string)>();

        if (messaging.Value is null)
            return GenericResult<(string, string)>.Failure(MessagingLog.LocationNotConfigured(_logger, $"no Messaging row named '{MessagingServiceName}' exists"));

        return string.IsNullOrWhiteSpace(messaging.Value.DataStoreName) || string.IsNullOrWhiteSpace(messaging.Value.PathName)
            ? GenericResult<(string, string)>.Failure(MessagingLog.LocationNotConfigured(_logger, "DataStoreName or PathName is unset on the Messaging row"))
            : GenericResult<(string, string)>.Success((messaging.Value.DataStoreName, messaging.Value.PathName));
    }
}
