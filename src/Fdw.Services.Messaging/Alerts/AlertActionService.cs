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
/// Default implementation of <see cref="IAlertActionService"/> (FDW-2420): what was done to alerts is kept in the msg tables of the Messaging store, and each kind
/// of alert is an <see cref="IAlertSource"/> that finds its alerts for the caller.
/// </summary>
/// <remarks>
/// The caller is the authentication context's: a person acts as themselves. Whether they may see an alert is the source's to say, and an alert they may not see
/// does not exist for them. Which actions an alert offers is its source's; what an action is, who may take it and whether it hides the alert is the
/// <see cref="AlertActions"/> option's, so nothing here names an action.
/// </remarks>
public sealed class AlertActionService : IAlertActionService
{
    private readonly ILogger<AlertActionService> _logger;
    private readonly AlertStore _store;
    private readonly AlertKindChoices _choices;
    private readonly IAuthenticationContextAccessor _authContext;
    private readonly IUserNames _userNames;
    private readonly TimeProvider _clock;
    private readonly Dictionary<string, IAlertSource> _sources;

    /// <summary>
    /// Initializes a new instance of the <see cref="AlertActionService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="dataGateways">Resolves the data gateway when a call is made.</param>
    /// <param name="messaging">Resolves where this deployment keeps its messaging data.</param>
    /// <param name="authContext">Supplies the caller, who is the person acting.</param>
    /// <param name="userNames">Reads the caller's user name from their account, for the rows that record who acted.</param>
    /// <param name="clock">The clock a time is judged and set on.</param>
    /// <param name="sources">Every kind of alert; each kind is named once.</param>
    public AlertActionService(
        ILogger<AlertActionService> logger,
        IDataGatewayProvider dataGateways,
        IMessagingConfigurationProvider messaging,
        IAuthenticationContextAccessor authContext,
        IUserNames userNames,
        TimeProvider clock,
        IEnumerable<IAlertSource> sources)
    {
        _logger = logger ?? NullLogger<AlertActionService>.Instance;
        _store = new AlertStore(_logger, dataGateways, messaging);
        _choices = new AlertKindChoices(_store);
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        _userNames = userNames ?? throw new ArgumentNullException(nameof(userNames));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        ArgumentNullException.ThrowIfNull(sources);
        _sources = sources.ToDictionary(source => source.Kind, StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<AlertList>> List(IReadOnlyList<AlertKey> alerts, CancellationToken cancellationToken = default)
    {
        MessagingLog.TraceAlertEntry(_logger, "List");
        ArgumentNullException.ThrowIfNull(alerts);

        try
        {
            var caller = Caller("list");
            if (!caller.IsSuccess)
                return caller.ToNewResult<AlertList>();

            if (alerts.Count == 0)
                return GenericResult<AlertList>.Failure(MessagingLog.AlertAskedWrong(_logger, "alert", "must name at least one alert"));

            if (alerts.Count > AlertLimits.MaxAlerts)
                return GenericResult<AlertList>.Failure(MessagingLog.AlertAskedWrong(_logger, "alert", $"may name at most {AlertLimits.MaxAlerts} alerts"));

            var unknown = alerts.Select(alert => alert.Kind).FirstOrDefault(kind => !_sources.ContainsKey(kind));
            if (unknown is not null)
                return GenericResult<AlertList>.Failure(MessagingLog.AlertKindUnknown(_logger, unknown, Kinds()));

            var now = _clock.GetUtcNow();
            var summaries = new Dictionary<(string Kind, Guid Id), AlertSummary>();
            var missing = new List<AlertKey>();

            foreach (var group in alerts.GroupBy(alert => _sources[alert.Kind].Kind, StringComparer.OrdinalIgnoreCase))
            {
                var source = _sources[group.Key];
                var found = new List<AlertSubject>();
                foreach (var id in group.Select(alert => alert.Id).Distinct())
                {
                    var subject = await source.Find(id, cancellationToken).ConfigureAwait(false);
                    if (!subject.IsSuccess)
                        return subject.ToNewResult<AlertList>();

                    if (subject.Value is { } alert)
                        found.Add(alert);
                    else
                        missing.Add(new AlertKey(source.Kind, id));
                }

                var described = await Describe(source, found, caller.Value, now, cancellationToken).ConfigureAwait(false);
                if (!described.IsSuccess)
                    return described.ToNewResult<AlertList>();

                foreach (var summary in described.Value!)
                    summaries[(source.Kind, summary.Id)] = summary;
            }

            return GenericResult<AlertList>.Success(new AlertList
            {
                Items = [.. alerts.Select(alert => (_sources[alert.Kind].Kind, alert.Id)).Distinct().Where(summaries.ContainsKey).Select(key => summaries[key])],
                Missing = missing
            });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return GenericResult<AlertList>.Failure(MessagingLog.AlertUnexpected(_logger, "list", ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<IGenericResult<AlertSummary>> Apply(string kind, Guid id, string action, string? choice, CancellationToken cancellationToken = default)
    {
        MessagingLog.TraceAlertEntry(_logger, "Apply");

        try
        {
            var caller = Caller("act on");
            if (!caller.IsSuccess)
                return caller.ToNewResult<AlertSummary>();

            var located = await Locate(kind, id, action, cancellationToken).ConfigureAwait(false);
            if (!located.IsSuccess)
                return located.ToNewResult<AlertSummary>();

            var (source, subject, option) = located.Value;

            if (option.RequiredPermission is { } permission && !PermissionCoverage.Covers(caller.Value.Permissions, permission))
                return GenericResult<AlertSummary>.Failure(MessagingLog.AlertActionForbidden(_logger, caller.Value.UserId.ToString("D"), option.Name, permission));

            var length = Length(option, choice);
            if (!length.IsSuccess)
                return length.ToNewResult<AlertSummary>();

            var acting = await _userNames.Get(caller.Value.UserId, cancellationToken).ConfigureAwait(false);
            if (!acting.IsSuccess)
                return acting.ToNewResult<AlertSummary>();

            var now = _clock.GetUtcNow();
            var written = await Write(source, subject, option, caller.Value, acting.Value!, length.Value, now, cancellationToken).ConfigureAwait(false);
            if (!written.IsSuccess)
                return written.ToNewResult<AlertSummary>();

            var told = await source.Acted(id, option, cancellationToken).ConfigureAwait(false);
            if (!told.IsSuccess)
                return told.ToNewResult<AlertSummary>();

            return await DescribeOne(source, subject, caller.Value, now, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return GenericResult<AlertSummary>.Failure(MessagingLog.AlertUnexpected(_logger, "act on", ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<IGenericResult<AlertSummary>> TakeBack(string kind, Guid id, string action, CancellationToken cancellationToken = default)
    {
        MessagingLog.TraceAlertEntry(_logger, "TakeBack");

        try
        {
            var caller = Caller("take back an action on");
            if (!caller.IsSuccess)
                return caller.ToNewResult<AlertSummary>();

            var located = await Locate(kind, id, action, cancellationToken).ConfigureAwait(false);
            if (!located.IsSuccess)
                return located.ToNewResult<AlertSummary>();

            var (source, subject, option) = located.Value;

            if (option.ForEveryone)
                return GenericResult<AlertSummary>.Failure(MessagingLog.AlertActionNotYoursToTakeBack(_logger, option.Name, caller.Value.UserId.ToString("D")));

            var removed = await _store.Remove(source.Kind, id, caller.Value.UserId, option.Name, cancellationToken).ConfigureAwait(false);
            if (!removed.IsSuccess)
                return removed.ToNewResult<AlertSummary>();

            MessagingLog.AlertActionWithdrawn(_logger, source.Kind, id.ToString("D"), caller.Value.UserId.ToString("D"), option.Name);
            return await DescribeOne(source, subject, caller.Value, _clock.GetUtcNow(), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return GenericResult<AlertSummary>.Failure(MessagingLog.AlertUnexpected(_logger, "take back an action on", ex.Message));
        }
    }

    // Finds the kind, the action and the alert, and checks that the kind offers the action: the checks Apply and TakeBack share, in the order a caller can mend.
    private async Task<IGenericResult<(IAlertSource Source, AlertSubject Subject, IAlertAction Action)>> Locate(string kind, Guid id, string action, CancellationToken cancellationToken)
    {
        if (!_sources.TryGetValue(kind, out var source))
            return GenericResult<(IAlertSource, AlertSubject, IAlertAction)>.Failure(MessagingLog.AlertKindUnknown(_logger, kind, Kinds()));

        var option = AlertActions.ByName(action);
        if (ReferenceEquals(option, AlertActions.NotFound))
            return GenericResult<(IAlertSource, AlertSubject, IAlertAction)>.Failure(MessagingLog.AlertActionUnknown(_logger, action, string.Join(", ", AlertActions.All().Select(a => a.Name))));

        var subject = await source.Find(id, cancellationToken).ConfigureAwait(false);
        if (!subject.IsSuccess)
            return subject.ToNewResult<(IAlertSource, AlertSubject, IAlertAction)>();

        if (subject.Value is not { } alert)
            return GenericResult<(IAlertSource, AlertSubject, IAlertAction)>.Failure(MessagingLog.AlertNotFound(_logger, source.Kind, id.ToString("D")));

        // Why the alert's tenant: the company that owns the alert decides which actions its people are offered.
        var offered = await _choices.For(source, [alert.TenantId], cancellationToken).ConfigureAwait(false);
        if (!offered.IsSuccess)
            return offered.ToNewResult<(IAlertSource, AlertSubject, IAlertAction)>();

        return offered.Value![alert.TenantId].Contains(option.Name, StringComparer.OrdinalIgnoreCase)
            ? GenericResult<(IAlertSource, AlertSubject, IAlertAction)>.Success((source, alert, option))
            : GenericResult<(IAlertSource, AlertSubject, IAlertAction)>.Failure(MessagingLog.AlertActionNotOffered(_logger, source.Kind, id.ToString("D"), option.Name));
    }

    // The length of time an action lasts, from the choice sent: none for an action that offers none, and a choice that is one of the action's for one that does.
    private IGenericResult<int?> Length(IAlertAction option, string? choice)
    {
        if (option.Choices.Count == 0)
            return string.IsNullOrWhiteSpace(choice)
                ? GenericResult<int?>.Success(null)
                : GenericResult<int?>.Failure(MessagingLog.AlertChoiceInvalid(_logger, option.Name, $"is not taken by {option.Name}"));

        if (string.IsNullOrWhiteSpace(choice))
            return GenericResult<int?>.Failure(MessagingLog.AlertChoiceInvalid(_logger, option.Name, $"is needed; the choices are {string.Join(", ", option.Choices.Select(c => c.Name))}"));

        var picked = option.Choices.FirstOrDefault(c => string.Equals(c.Name, choice, StringComparison.OrdinalIgnoreCase));
        return picked is null
            ? GenericResult<int?>.Failure(MessagingLog.AlertChoiceInvalid(_logger, option.Name, $"{choice} is not one of {string.Join(", ", option.Choices.Select(c => c.Name))}"))
            : GenericResult<int?>.Success(picked.Minutes);
    }

    // Writes the action as the person's or as one for everyone. An action that lasts a chosen time is written again each time it is taken, so its end moves; one that
    // does not is written once and left as it was when taken again.
    private async Task<IGenericResult> Write(IAlertSource source, AlertSubject subject, IAlertAction option, AlertCaller caller, string userName, int? minutes, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var until = minutes is { } length ? now.AddMinutes(length) : (DateTimeOffset?)null;

        if (option.ForEveryone)
        {
            var held = await _store.FindEveryoneActions(source.Kind, [subject.Id], cancellationToken).ConfigureAwait(false);
            if (!held.IsSuccess)
                return held;

            if (until is null && held.Value!.Any(row => Named(row.Action, option)))
                return Unchanged(source, subject, option, caller);

            var row = new AlertEveryoneActionRow { TenantId = subject.TenantId, Kind = source.Kind, AlertId = subject.Id, Action = option.Name, ActedByUserId = caller.UserId, ActedByName = userName, ActedAt = now, Until = until };
            var put = await _store.PutForEveryone(row, cancellationToken).ConfigureAwait(false);
            return Taken(put, source, subject, option, userName);
        }

        var mine = await _store.FindActions(source.Kind, [subject.Id], caller.UserId, cancellationToken).ConfigureAwait(false);
        if (!mine.IsSuccess)
            return mine;

        if (until is null && mine.Value!.Any(row => Named(row.Action, option)))
            return Unchanged(source, subject, option, caller);

        var own = new AlertActionRow { TenantId = subject.TenantId, Kind = source.Kind, AlertId = subject.Id, UserId = caller.UserId, Action = option.Name, UserName = userName, ActedAt = now, Until = until };
        return Taken(await _store.Put(own, cancellationToken).ConfigureAwait(false), source, subject, option, userName);
    }

    private IGenericResult Unchanged(IAlertSource source, AlertSubject subject, IAlertAction option, AlertCaller caller)
    {
        MessagingLog.AlertActionAlreadyTaken(_logger, source.Kind, subject.Id.ToString("D"), caller.UserId.ToString("D"), option.Name);
        return GenericResult.Success();
    }

    private IGenericResult Taken(IGenericResult put, IAlertSource source, AlertSubject subject, IAlertAction option, string userName)
    {
        if (put.IsSuccess)
            MessagingLog.AlertActionApplied(_logger, source.Kind, subject.Id.ToString("D"), userName, option.Name);

        return put;
    }

    private async Task<IGenericResult<AlertSummary>> DescribeOne(IAlertSource source, AlertSubject subject, AlertCaller caller, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var described = await Describe(source, [subject], caller, now, cancellationToken).ConfigureAwait(false);
        return described.IsSuccess ? GenericResult<AlertSummary>.Success(described.Value![0]) : described.ToNewResult<AlertSummary>();
    }

    // What the alerts allow the caller and what stands on them now. An action still in force is one with no end or an end in the future; one whose time is over no
    // longer hides the alert and is not listed, so the alert returns by itself.
    private async Task<IGenericResult<IReadOnlyList<AlertSummary>>> Describe(IAlertSource source, List<AlertSubject> subjects, AlertCaller caller, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (subjects.Count == 0)
            return GenericResult<IReadOnlyList<AlertSummary>>.Success([]);

        var ids = subjects.Select(subject => subject.Id).ToList();
        var mine = await _store.FindActions(source.Kind, ids, caller.UserId, cancellationToken).ConfigureAwait(false);
        if (!mine.IsSuccess)
            return mine.ToNewResult<IReadOnlyList<AlertSummary>>();

        var everyone = await _store.FindEveryoneActions(source.Kind, ids, cancellationToken).ConfigureAwait(false);
        if (!everyone.IsSuccess)
            return everyone.ToNewResult<IReadOnlyList<AlertSummary>>();

        var offered = await _choices.For(source, subjects.Select(subject => subject.TenantId), cancellationToken).ConfigureAwait(false);
        if (!offered.IsSuccess)
            return offered.ToNewResult<IReadOnlyList<AlertSummary>>();

        var summaries = new List<AlertSummary>();
        foreach (var subject in subjects)
        {
            var summary = Summarize(source, subject, caller, now, mine.Value!.Where(row => row.AlertId == subject.Id).ToList(), everyone.Value!.Where(row => row.AlertId == subject.Id).ToList(), offered.Value![subject.TenantId]);
            if (!summary.IsSuccess)
                return summary.ToNewResult<IReadOnlyList<AlertSummary>>();

            summaries.Add(summary.Value!);
        }

        return GenericResult<IReadOnlyList<AlertSummary>>.Success(summaries);
    }

    private IGenericResult<AlertSummary> Summarize(IAlertSource source, AlertSubject subject, AlertCaller caller, DateTimeOffset now, List<AlertActionRow> mine, List<AlertEveryoneActionRow> everyone, List<string> offeredNames)
    {
        var taken = new List<(IAlertAction Option, AlertActionTaken Fact)>();

        foreach (var row in mine.Where(row => row.Until is null || row.Until > now))
        {
            var option = Declared(row.Action, "a stored action");
            if (!option.IsSuccess)
                return option.ToNewResult<AlertSummary>();

            taken.Add((option.Value!, new AlertActionTaken { Action = row.Action, ForEveryone = false, At = row.ActedAt, Until = row.Until }));
        }

        foreach (var row in everyone.Where(row => row.Until is null || row.Until > now))
        {
            var option = Declared(row.Action, "a stored action");
            if (!option.IsSuccess)
                return option.ToNewResult<AlertSummary>();

            taken.Add((option.Value!, new AlertActionTaken { Action = row.Action, ForEveryone = true, At = row.ActedAt, ByName = row.ActedByName, Until = row.Until }));
        }

        var hiding = taken.Where(fact => fact.Option.HidesTheAlert).ToList();
        var shown = hiding.Count == 0;

        // Why the latest end: the alert comes back when every action hiding it is over, and one with no end hides it for good.
        var returnsAt = !shown && hiding.All(fact => fact.Fact.Until is not null) ? hiding.Max(fact => fact.Fact.Until) : null;

        var offers = new List<AlertActionOffer>();
        var hiddenForEveryone = hiding.Any(fact => fact.Fact.ForEveryone);
        if (!hiddenForEveryone)
        {
            foreach (var name in offeredNames)
            {
                var option = Declared(name, source.Kind);
                if (!option.IsSuccess)
                    return option.ToNewResult<AlertSummary>();

                if (option.Value!.RequiredPermission is { } permission && !PermissionCoverage.Covers(caller.Permissions, permission))
                    continue;

                offers.Add(new AlertActionOffer
                {
                    Name = option.Value.Name,
                    DisplayName = option.Value.DisplayName,
                    Description = option.Value.Description,
                    ForEveryone = option.Value.ForEveryone,
                    Done = taken.Any(fact => Named(fact.Fact.Action, option.Value)),
                    NeedsChoice = option.Value.Choices.Count > 0,
                    Choices = option.Value.Choices
                });
            }
        }

        return GenericResult<AlertSummary>.Success(new AlertSummary
        {
            Kind = source.Kind,
            Id = subject.Id,
            Shown = shown,
            ReturnsAt = returnsAt,
            Taken = [.. taken.Select(fact => fact.Fact)],
            Actions = offers
        });
    }

    // An action named by a stored row or by a source must still be an option of the collection: one removed while still in use is a failure, never skipped.
    private IGenericResult<IAlertAction> Declared(string name, string namedBy)
    {
        var option = AlertActions.ByName(name);
        return ReferenceEquals(option, AlertActions.NotFound)
            ? GenericResult<IAlertAction>.Failure(MessagingLog.AlertActionNotDeclared(_logger, name, namedBy))
            : GenericResult<IAlertAction>.Success(option);
    }

    private static bool Named(string name, IAlertAction option) => string.Equals(name, option.Name, StringComparison.OrdinalIgnoreCase);

    private string Kinds() => string.Join(", ", _sources.Values.Select(source => source.Kind));

    private IGenericResult<AlertCaller> Caller(string operation)
    {
        if (_authContext.Current is not { IsAuthenticated: true } context || !Guid.TryParse(context.UserId, out var userId))
            return GenericResult<AlertCaller>.Failure(MessagingLog.AlertCallerRequired(_logger, operation));

        return GenericResult<AlertCaller>.Success(new AlertCaller(userId, [.. context.Permissions]));
    }
}
