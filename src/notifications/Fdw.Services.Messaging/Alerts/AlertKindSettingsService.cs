using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Messaging.Abstractions;
using Fdw.Services.Messaging.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Messaging;

/// <summary>
/// Default implementation of <see cref="IAlertKindSettingsService"/> (FDW-2430): which actions each kind of alert offers the caller's company, kept in the msg
/// tables of the Messaging store. The company is the caller's active tenant.
/// </summary>
public sealed class AlertKindSettingsService : IAlertKindSettingsService
{
    private readonly ILogger<AlertKindSettingsService> _logger;
    private readonly AlertStore _store;
    private readonly AlertKindChoices _choices;
    private readonly IAuthenticationContextAccessor _authContext;
    private readonly IUserNames _userNames;
    private readonly TimeProvider _clock;
    private readonly Dictionary<string, IAlertSource> _sources;

    /// <summary>
    /// Initializes a new instance of the <see cref="AlertKindSettingsService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="dataGateways">Resolves the data gateway when a call is made.</param>
    /// <param name="messaging">Resolves where this deployment keeps its messaging data.</param>
    /// <param name="authContext">Supplies the caller, who is the person choosing and whose active tenant is the company.</param>
    /// <param name="userNames">Reads the caller's user name from their account, for the row that records who chose.</param>
    /// <param name="clock">The clock the time of a choice is set on.</param>
    /// <param name="sources">Every kind of alert; each kind is named once.</param>
    public AlertKindSettingsService(
        ILogger<AlertKindSettingsService> logger,
        IDataGatewayProvider dataGateways,
        IMessagingConfigurationProvider messaging,
        IAuthenticationContextAccessor authContext,
        IUserNames userNames,
        TimeProvider clock,
        IEnumerable<IAlertSource> sources)
    {
        _logger = logger ?? NullLogger<AlertKindSettingsService>.Instance;
        _store = new AlertStore(_logger, dataGateways, messaging);
        _choices = new AlertKindChoices(_store);
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        _userNames = userNames ?? throw new ArgumentNullException(nameof(userNames));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        ArgumentNullException.ThrowIfNull(sources);
        _sources = sources.ToDictionary(source => source.Kind, StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<AlertKindSummary>>> List(CancellationToken cancellationToken = default)
    {
        MessagingLog.TraceAlertEntry(_logger, "List the kinds of");

        try
        {
            var caller = Caller("list the kinds of");
            if (!caller.IsSuccess)
                return caller.ToNewResult<IReadOnlyList<AlertKindSummary>>();

            var chosen = await _store.FindKindSettings(caller.Value.TenantId, cancellationToken).ConfigureAwait(false);
            if (!chosen.IsSuccess)
                return chosen.ToNewResult<IReadOnlyList<AlertKindSummary>>();

            var summaries = new List<AlertKindSummary>();
            foreach (var source in _sources.Values.OrderBy(known => known.Kind, StringComparer.OrdinalIgnoreCase))
            {
                var summary = Summarize(source, chosen.Value!.TryGetValue(source.Kind, out var list) ? list : null);
                if (!summary.IsSuccess)
                    return summary.ToNewResult<IReadOnlyList<AlertKindSummary>>();

                summaries.Add(summary.Value!);
            }

            return GenericResult<IReadOnlyList<AlertKindSummary>>.Success(summaries);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return GenericResult<IReadOnlyList<AlertKindSummary>>.Failure(MessagingLog.AlertUnexpected(_logger, "list the kinds of", ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<IGenericResult<AlertKindSummary>> Save(string kind, IReadOnlyList<string>? actions, CancellationToken cancellationToken = default)
    {
        MessagingLog.TraceAlertEntry(_logger, "Choose the actions of a kind of");

        try
        {
            var caller = Caller("choose the actions of a kind of");
            if (!caller.IsSuccess)
                return caller.ToNewResult<AlertKindSummary>();

            if (!_sources.TryGetValue(kind, out var source))
                return GenericResult<AlertKindSummary>.Failure(MessagingLog.AlertKindUnknown(_logger, kind, string.Join(", ", _sources.Values.Select(s => s.Kind))));

            var named = Names(actions);
            if (!named.IsSuccess)
                return named.ToNewResult<AlertKindSummary>();

            var acting = await _userNames.Get(caller.Value.UserId, cancellationToken).ConfigureAwait(false);
            if (!acting.IsSuccess)
                return acting.ToNewResult<AlertKindSummary>();

            var setting = new AlertKindSettingRow
            {
                TenantId = caller.Value.TenantId,
                Kind = source.Kind,
                ChangedByUserId = caller.Value.UserId,
                ChangedByName = acting.Value!,
                ChangedAt = _clock.GetUtcNow()
            };

            var saved = await _store.SaveKindSetting(setting, named.Value!, cancellationToken).ConfigureAwait(false);
            if (!saved.IsSuccess)
                return saved.ToNewResult<AlertKindSummary>();

            MessagingLog.AlertKindActionsSet(_logger, source.Kind, acting.Value!, string.Join(", ", named.Value!), caller.Value.TenantId.ToString("D"));
            return Summarize(source, named.Value);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return GenericResult<AlertKindSummary>.Failure(MessagingLog.AlertUnexpected(_logger, "choose the actions of a kind of", ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<IGenericResult<AlertKindSummary>> Reset(string kind, CancellationToken cancellationToken = default)
    {
        MessagingLog.TraceAlertEntry(_logger, "Go back to the actions of a kind of");

        try
        {
            var caller = Caller("go back to the actions of a kind of");
            if (!caller.IsSuccess)
                return caller.ToNewResult<AlertKindSummary>();

            if (!_sources.TryGetValue(kind, out var source))
                return GenericResult<AlertKindSummary>.Failure(MessagingLog.AlertKindUnknown(_logger, kind, string.Join(", ", _sources.Values.Select(s => s.Kind))));

            var removed = await _store.RemoveKindSetting(caller.Value.TenantId, source.Kind, cancellationToken).ConfigureAwait(false);
            if (!removed.IsSuccess)
                return removed.ToNewResult<AlertKindSummary>();

            MessagingLog.AlertKindActionsReset(_logger, source.Kind, caller.Value.UserId.ToString("D"), caller.Value.TenantId.ToString("D"));
            return Summarize(source, null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return GenericResult<AlertKindSummary>.Failure(MessagingLog.AlertUnexpected(_logger, "go back to the actions of a kind of", ex.Message));
        }
    }

    // The names a caller sent, each an option of the collection and none twice, written as the collection names them.
    private IGenericResult<List<string>> Names(IReadOnlyList<string>? actions)
    {
        if (actions is null)
            return GenericResult<List<string>>.Failure(MessagingLog.AlertAskedWrong(_logger, "actions", "must be a list; send an empty one for none"));

        var names = new List<string>();
        foreach (var action in actions)
        {
            var option = AlertActions.ByName(action);
            if (ReferenceEquals(option, AlertActions.NotFound))
                return GenericResult<List<string>>.Failure(MessagingLog.AlertActionUnknown(_logger, action, string.Join(", ", AlertActions.All().Select(a => a.Name))));

            if (names.Contains(option.Name, StringComparer.Ordinal))
                return GenericResult<List<string>>.Failure(MessagingLog.AlertActionRepeated(_logger, option.Name));

            names.Add(option.Name);
        }

        return GenericResult<List<string>>.Success(names);
    }

    // The kind as the caller's company sees it: the actions it chose when it chose, else the kind's own, each named by the collection (a stored name that is no
    // longer an option fails loudly).
    private IGenericResult<AlertKindSummary> Summarize(IAlertSource source, List<string>? chosen)
    {
        // Why the kind's own list when the company chose none: it is the default the kind states in code, shown as such (Configured is false).
        IReadOnlyList<string> names = chosen is not null ? chosen : source.OfferedActions;
        var offered = new List<AlertKindAction>();
        foreach (var name in names)
        {
            var option = AlertActions.ByName(name);
            if (ReferenceEquals(option, AlertActions.NotFound))
                return GenericResult<AlertKindSummary>.Failure(MessagingLog.AlertActionNotDeclared(_logger, name, source.Kind));

            offered.Add(Described(option));
        }

        // Why the kind's own actions first: they are the ones its code names, and the rest follow in the collection's order, so a screen can show a choice among
        // every declared action without naming one.
        var available = new List<AlertKindAction>();
        foreach (var name in source.OfferedActions)
        {
            var option = AlertActions.ByName(name);
            if (ReferenceEquals(option, AlertActions.NotFound))
                return GenericResult<AlertKindSummary>.Failure(MessagingLog.AlertActionNotDeclared(_logger, name, source.Kind));

            available.Add(Described(option));
        }

        foreach (var option in AlertActions.All())
        {
            if (!available.Any(known => string.Equals(known.Name, option.Name, StringComparison.Ordinal)))
                available.Add(Described(option));
        }

        return GenericResult<AlertKindSummary>.Success(new AlertKindSummary
        {
            Kind = source.Kind,
            DefaultActions = source.OfferedActions,
            Configured = chosen is not null,
            Actions = offered,
            Available = available
        });
    }

    private static AlertKindAction Described(IAlertAction option)
        => new() { Name = option.Name, DisplayName = option.DisplayName, Description = option.Description };

    private IGenericResult<AlertKindCaller> Caller(string operation)
    {
        if (_authContext.Current is not { IsAuthenticated: true } context
            || !Guid.TryParse(context.UserId, out var userId)
            || context.ActiveTenantId is not { } tenantId)
            return GenericResult<AlertKindCaller>.Failure(MessagingLog.AlertCallerRequired(_logger, operation));

        return GenericResult<AlertKindCaller>.Success(new AlertKindCaller(userId, tenantId));
    }
}
