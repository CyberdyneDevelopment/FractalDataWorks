using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Operations.Execution;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Messaging.Abstractions;
using Fdw.Services.Messaging.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Messaging;

/// <summary>
/// Default implementation of <see cref="IUiFeedbackService"/>: UI comments kept in the msg tables of the Messaging store (FDW-2143).
/// </summary>
/// <remarks>
/// The caller is the authentication context's: the author and tenant of a comment are theirs, never stated by the request, and a read reaches the tenants
/// <see cref="ExecutionTenantScope"/> says (the platform and the system see every tenant's comments, anyone else sees their own tenant's). A comment out of
/// reach is answered as not found.
/// </remarks>
public sealed class UiFeedbackService : IUiFeedbackService
{
    // Why a window: a tag filter and an urgency order cannot be put to the store, so they look at the newest comments that match the other conditions.
    private const int WindowSize = 1000;

    // The permission that makes a person a reader of comments, and the type of the message that tells them.
    private const string ReadPermission = "ui-feedback:read";
    private const string AlertType = "UiFeedback";

    private readonly ILogger<UiFeedbackService> _logger;
    private readonly UiFeedbackStore _store;
    private readonly IAuthenticationContextAccessor _authContext;
    private readonly IPermissionHolders _holders;
    private readonly IMessageService _messages;

    /// <summary>
    /// Initializes a new instance of the <see cref="UiFeedbackService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="dataGateways">Resolves the data gateway when a call is made.</param>
    /// <param name="messaging">Resolves where this deployment keeps its messaging data.</param>
    /// <param name="authContext">Supplies the caller, whose identity and tenant every comment carries and whose tenant scopes every read.</param>
    /// <param name="holders">Says who holds the permission to read comments, the people a new comment is told to.</param>
    /// <param name="messages">Tells each of them, and marks an alert read when it is acknowledged.</param>
    public UiFeedbackService(
        ILogger<UiFeedbackService> logger,
        IDataGatewayProvider dataGateways,
        IMessagingConfigurationProvider messaging,
        IAuthenticationContextAccessor authContext,
        IPermissionHolders holders,
        IMessageService messages)
    {
        _logger = logger ?? NullLogger<UiFeedbackService>.Instance;
        _store = new UiFeedbackStore(_logger, dataGateways, messaging);
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        _holders = holders ?? throw new ArgumentNullException(nameof(holders));
        _messages = messages ?? throw new ArgumentNullException(nameof(messages));
    }

    /// <inheritdoc />
    public async Task<IGenericResult<UiFeedbackPayload>> Create(CreateUiFeedbackRequest request, CancellationToken cancellationToken = default)
    {
        MessagingLog.TraceUiFeedbackEntry(_logger, "Create");
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            var caller = Caller("save");
            if (!caller.IsSuccess)
                return caller.ToNewResult<UiFeedbackPayload>();

            if (UiFeedbackChecks.Problem(_logger, request) is { } problem)
                return GenericResult<UiFeedbackPayload>.Failure(problem);

            var picture = request.Screenshot is null ? null : UiFeedbackPictures.Decode(_logger, request.Screenshot);
            if (picture is { IsSuccess: false })
                return picture.ToNewResult<UiFeedbackPayload>();

            var row = UiFeedbackMapping.NewRow(caller.Value, request, picture is not null);
            var tags = UiFeedbackMapping.TagNames(request);
            var saved = await _store.Add(
                row,
                tags.Select(tag => new UiFeedbackTagRow { FeedbackId = row.Id, Tag = tag }).ToList(),
                picture is null ? null : new UiFeedbackScreenshotRow { FeedbackId = row.Id, ContentType = picture.Value!.ContentType, Picture = picture.Value!.Content },
                cancellationToken).ConfigureAwait(false);
            if (!saved.IsSuccess)
                return saved.ToNewResult<UiFeedbackPayload>();

            MessagingLog.UiFeedbackSaved(_logger, row.Id.ToString("D"), row.AuthorName, row.Route, row.Urgency);

            // Why the comment is answered as saved whatever happens next: it is kept, and a reader who could not be told is a failure to report beside it, not a
            // reason to lose it.
            var untold = await TellReaders(row, cancellationToken).ConfigureAwait(false);
            return GenericResult<UiFeedbackPayload>.Success(UiFeedbackMapping.ToPayload(row, tags, caller.Value.UserId, false), untold);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return GenericResult<UiFeedbackPayload>.Failure(MessagingLog.UiFeedbackUnexpected(_logger, "save", ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<UiFeedbackPayload>>> List(UiFeedbackQuery query, CancellationToken cancellationToken = default)
    {
        MessagingLog.TraceUiFeedbackEntry(_logger, "List");
        ArgumentNullException.ThrowIfNull(query);

        try
        {
            var caller = Caller("list");
            if (!caller.IsSuccess)
                return caller.ToNewResult<IReadOnlyList<UiFeedbackPayload>>();

            var request = ListRequestFor(caller.Value, query);
            if (!request.IsSuccess)
                return request.ToNewResult<IReadOnlyList<UiFeedbackPayload>>();

            var rows = request.Value.NeedsWindow
                ? await _store.FindRows(request.Value.Filter, 0, WindowSize, cancellationToken).ConfigureAwait(false)
                : await _store.FindRows(request.Value.Filter, request.Value.Skip, request.Value.Take, cancellationToken).ConfigureAwait(false);
            if (!rows.IsSuccess)
                return rows.ToNewResult<IReadOnlyList<UiFeedbackPayload>>();

            var ids = rows.Value!.Select(row => row.Id).ToList();
            var tags = await _store.FindTags(ids, cancellationToken).ConfigureAwait(false);
            if (!tags.IsSuccess)
                return tags.ToNewResult<IReadOnlyList<UiFeedbackPayload>>();

            var acknowledged = await _store.FindAcknowledged(ids, caller.Value.UserId, cancellationToken).ConfigureAwait(false);
            return acknowledged.IsSuccess
                ? Page(caller.Value, request.Value, rows.Value!, tags.Value!, acknowledged.Value!)
                : acknowledged.ToNewResult<IReadOnlyList<UiFeedbackPayload>>();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return GenericResult<IReadOnlyList<UiFeedbackPayload>>.Failure(MessagingLog.UiFeedbackUnexpected(_logger, "list", ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<IGenericResult<UiFeedbackPayload>> Get(Guid id, CancellationToken cancellationToken = default)
    {
        MessagingLog.TraceUiFeedbackEntry(_logger, "Get");

        try
        {
            var caller = Caller("read");
            if (!caller.IsSuccess)
                return caller.ToNewResult<UiFeedbackPayload>();

            var row = await Reachable(caller.Value, id, cancellationToken).ConfigureAwait(false);
            return row.IsSuccess
                ? await Describe(caller.Value, row.Value!, cancellationToken).ConfigureAwait(false)
                : row.ToNewResult<UiFeedbackPayload>();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return GenericResult<UiFeedbackPayload>.Failure(MessagingLog.UiFeedbackUnexpected(_logger, "read", ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<IGenericResult<UiFeedbackPicture>> GetPicture(Guid id, CancellationToken cancellationToken = default)
    {
        MessagingLog.TraceUiFeedbackEntry(_logger, "GetPicture");

        try
        {
            var caller = Caller("read the picture of");
            if (!caller.IsSuccess)
                return caller.ToNewResult<UiFeedbackPicture>();

            var row = await Reachable(caller.Value, id, cancellationToken).ConfigureAwait(false);
            if (!row.IsSuccess)
                return row.ToNewResult<UiFeedbackPicture>();

            if (!row.Value!.HasScreenshot)
                return GenericResult<UiFeedbackPicture>.Failure(MessagingLog.UiFeedbackNotFound(_logger, id.ToString("D")));

            var picture = await _store.FindPicture(id, cancellationToken).ConfigureAwait(false);
            if (!picture.IsSuccess)
                return picture.ToNewResult<UiFeedbackPicture>();

            // Why a failure and not "not found": the comment says it has a picture and none is stored, which is damaged data and not a missing comment.
            return picture.Value is { } kept
                ? GenericResult<UiFeedbackPicture>.Success(new UiFeedbackPicture { ContentType = kept.ContentType, Content = kept.Picture })
                : GenericResult<UiFeedbackPicture>.Failure(MessagingLog.UiFeedbackUnexpected(_logger, "read the picture of", $"comment {id:D} says it has a picture and none is stored"));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return GenericResult<UiFeedbackPicture>.Failure(MessagingLog.UiFeedbackUnexpected(_logger, "read the picture of", ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<IGenericResult<UiFeedbackPayload>> Edit(Guid id, string? body, CancellationToken cancellationToken = default)
    {
        MessagingLog.TraceUiFeedbackEntry(_logger, "Edit");

        try
        {
            var caller = Caller("edit");
            if (!caller.IsSuccess)
                return caller.ToNewResult<UiFeedbackPayload>();

            if (UiFeedbackChecks.BodyProblem(_logger, body) is { } problem)
                return GenericResult<UiFeedbackPayload>.Failure(problem);

            var row = await Own(caller.Value, id, "edit", cancellationToken).ConfigureAwait(false);
            if (!row.IsSuccess)
                return row.ToNewResult<UiFeedbackPayload>();

            var update = new UiFeedbackBodyUpdate { Body = body!, UpdatedAt = DateTimeOffset.UtcNow };
            var changed = await _store.UpdateBody(id, update, cancellationToken).ConfigureAwait(false);
            if (!changed.IsSuccess)
                return changed.ToNewResult<UiFeedbackPayload>();

            if (changed.Value == 0)
                return GenericResult<UiFeedbackPayload>.Failure(MessagingLog.UiFeedbackNotFound(_logger, id.ToString("D")));

            row.Value!.Body = update.Body;
            row.Value!.UpdatedAt = update.UpdatedAt;
            MessagingLog.UiFeedbackChanged(_logger, id.ToString("D"), "edited", caller.Value.Name);
            return await Describe(caller.Value, row.Value!, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return GenericResult<UiFeedbackPayload>.Failure(MessagingLog.UiFeedbackUnexpected(_logger, "edit", ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        MessagingLog.TraceUiFeedbackEntry(_logger, "Delete");

        try
        {
            var caller = Caller("delete");
            if (!caller.IsSuccess)
                return (IGenericResult)caller;

            var row = await Own(caller.Value, id, "delete", cancellationToken).ConfigureAwait(false);
            if (!row.IsSuccess)
                return (IGenericResult)row;

            var removed = await _store.Remove(id, cancellationToken).ConfigureAwait(false);
            if (removed.IsSuccess)
                MessagingLog.UiFeedbackChanged(_logger, id.ToString("D"), "deleted", caller.Value.Name);

            return removed;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return GenericResult.Failure(MessagingLog.UiFeedbackUnexpected(_logger, "delete", ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<IGenericResult<UiFeedbackPayload>> Acknowledge(Guid id, CancellationToken cancellationToken = default)
    {
        MessagingLog.TraceUiFeedbackEntry(_logger, "Acknowledge");

        try
        {
            var caller = Caller("acknowledge");
            if (!caller.IsSuccess)
                return caller.ToNewResult<UiFeedbackPayload>();

            var row = await Reachable(caller.Value, id, cancellationToken).ConfigureAwait(false);
            if (!row.IsSuccess)
                return row.ToNewResult<UiFeedbackPayload>();

            var held = await _store.FindAcknowledged([id], caller.Value.UserId, cancellationToken).ConfigureAwait(false);
            if (!held.IsSuccess)
                return held.ToNewResult<UiFeedbackPayload>();

            if (held.Value!.Contains(id))
            {
                MessagingLog.UiFeedbackAlreadyAcknowledged(_logger, id.ToString("D"), caller.Value.Name);
            }
            else
            {
                var written = await _store.AddAcknowledgement(
                    new UiFeedbackAcknowledgementRow { FeedbackId = id, UserId = caller.Value.UserId, UserName = caller.Value.Name, AcknowledgedAt = DateTimeOffset.UtcNow },
                    cancellationToken).ConfigureAwait(false);
                if (!written.IsSuccess)
                    return written.ToNewResult<UiFeedbackPayload>();

                MessagingLog.UiFeedbackAcknowledged(_logger, id.ToString("D"), caller.Value.Name);
            }

            // Why marked read after the acknowledgement is kept, and also when it was already: an alert left unread by an earlier failure is put right by asking again.
            var read = await MarkAlertsRead(id, caller.Value, cancellationToken).ConfigureAwait(false);
            return read.IsSuccess
                ? await Describe(caller.Value, row.Value!, cancellationToken).ConfigureAwait(false)
                : read.ToNewResult<UiFeedbackPayload>();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return GenericResult<UiFeedbackPayload>.Failure(MessagingLog.UiFeedbackUnexpected(_logger, "acknowledge", ex.Message));
        }
    }

    /// <inheritdoc />
    public Task<IGenericResult<UiFeedbackPayload>> Resolve(Guid id, CancellationToken cancellationToken = default)
        => Move(id, "Resolve", "Open", "Resolved", "resolved", cancellationToken);

    /// <inheritdoc />
    public Task<IGenericResult<UiFeedbackPayload>> Reopen(Guid id, CancellationToken cancellationToken = default)
        => Move(id, "Reopen", "Resolved", "Open", "reopened", cancellationToken);

    private async Task<IGenericResult<UiFeedbackPayload>> Move(Guid id, string operation, string stateNow, string stateNext, string change, CancellationToken cancellationToken)
    {
        MessagingLog.TraceUiFeedbackEntry(_logger, operation);

        try
        {
            var caller = Caller(operation.ToLowerInvariant());
            if (!caller.IsSuccess)
                return caller.ToNewResult<UiFeedbackPayload>();

            var row = await Reachable(caller.Value, id, cancellationToken).ConfigureAwait(false);
            if (!row.IsSuccess)
                return row.ToNewResult<UiFeedbackPayload>();

            if (!string.Equals(row.Value!.State, stateNow, StringComparison.Ordinal))
                return GenericResult<UiFeedbackPayload>.Failure(MessagingLog.UiFeedbackWrongState(_logger, id.ToString("D"), row.Value!.State));

            var resolving = string.Equals(stateNext, "Resolved", StringComparison.Ordinal);
            var update = new UiFeedbackStateUpdate
            {
                State = stateNext,
                ResolvedByUserId = resolving ? caller.Value.UserId : null,
                ResolvedByName = resolving ? caller.Value.Name : null,
                ResolvedAt = resolving ? DateTimeOffset.UtcNow : null
            };

            var changed = await _store.UpdateState(id, stateNow, update, cancellationToken).ConfigureAwait(false);
            if (!changed.IsSuccess)
                return changed.ToNewResult<UiFeedbackPayload>();

            // Why 0 is a conflict: the update is made only while the comment is still in the state it was read in, so nothing changed means someone moved it first.
            if (changed.Value == 0)
                return GenericResult<UiFeedbackPayload>.Failure(MessagingLog.UiFeedbackWrongState(_logger, id.ToString("D"), stateNext));

            row.Value!.State = update.State;
            row.Value!.ResolvedByUserId = update.ResolvedByUserId;
            row.Value!.ResolvedByName = update.ResolvedByName;
            row.Value!.ResolvedAt = update.ResolvedAt;
            MessagingLog.UiFeedbackChanged(_logger, id.ToString("D"), change, caller.Value.Name);
            return await Describe(caller.Value, row.Value!, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return GenericResult<UiFeedbackPayload>.Failure(MessagingLog.UiFeedbackUnexpected(_logger, operation.ToLowerInvariant(), ex.Message));
        }
    }

    // Tells each person who can read comments, in the comment's tenant and on the platform, except the author. Returns the failures to report beside the saved
    // comment: one reader who could not be told does not stop the others from being told.
    private async Task<IReadOnlyList<IGenericMessage>> TellReaders(UiFeedbackRow comment, CancellationToken cancellationToken)
    {
        var feedbackId = comment.Id.ToString("D");
        var untold = new List<IGenericMessage>();

        var inTenant = await _holders.Get(ReadPermission, comment.TenantId, cancellationToken).ConfigureAwait(false);
        var onPlatform = await _holders.GetOnPlatform(ReadPermission, cancellationToken).ConfigureAwait(false);
        foreach (var lookup in new[] { inTenant, onPlatform }.Where(found => !found.IsSuccess))
            untold.Add(MessagingLog.UiFeedbackReadersNotFound(_logger, feedbackId, lookup.CurrentMessage));

        // Why an empty list when a lookup failed: the failure is already among the ones returned, and the readers the other lookup found are still told.
        IReadOnlyList<Guid> readers = inTenant.IsSuccess && inTenant.Value is { } tenantReaders ? tenantReaders : [];
        readers = onPlatform.IsSuccess && onPlatform.Value is { } platformReaders ? readers.Union(platformReaders).ToList() : readers;

        var told = 0;
        foreach (var reader in readers.Where(reader => reader != comment.AuthorUserId))
        {
            var sent = await _messages.CreateMessage(AlertFor(comment, reader), cancellationToken).ConfigureAwait(false);
            if (sent.IsSuccess)
                told++;
            else
                untold.Add(MessagingLog.UiFeedbackReaderNotTold(_logger, feedbackId, reader.ToString("D"), sent.CurrentMessage));
        }

        MessagingLog.UiFeedbackReadersTold(_logger, feedbackId, told);
        return untold;
    }

    private static CreateMessageRequest AlertFor(UiFeedbackRow comment, Guid reader)
    {
        var id = comment.Id.ToString("D");
        return new CreateMessageRequest
        {
            TenantId = comment.TenantId,
            RecipientUserId = reader,
            SenderUserId = comment.AuthorUserId,
            MessageType = AlertType,
            Severity = UiFeedbackUrgencies.ByName(comment.Urgency).Severity,
            Subject = $"{comment.AuthorName} left a UI comment on {comment.Route}",
            Body = comment.Body,
            ResourceType = AlertType,
            ResourceId = id,
            ReferenceId = id,
            ActionType = "Open",
            ActionUrl = $"/feedback/{id}"
        };
    }

    // The person's alerts about the comment are the messages of type UiFeedback to them that carry its identifier as their reference.
    private async Task<IGenericResult> MarkAlertsRead(Guid feedbackId, UiFeedbackCaller caller, CancellationToken cancellationToken)
    {
        var alerts = await _messages.GetMessages(
            new MessageQuery { UserId = caller.UserId, MessageType = AlertType, ReferenceId = feedbackId.ToString("D") }, cancellationToken).ConfigureAwait(false);
        if (!alerts.IsSuccess || alerts.Value is null)
            return alerts;

        foreach (var alert in alerts.Value.Where(alert => alert.ReadAt is null))
        {
            var read = await _messages.MarkRead(alert.Id, cancellationToken).ConfigureAwait(false);
            if (!read.IsSuccess)
                return read;
        }

        return GenericResult.Success();
    }

    private IGenericResult<UiFeedbackCaller> Caller(string operation)
    {
        if (_authContext.Current is not { IsAuthenticated: true } context
            || !Guid.TryParse(context.UserId, out var userId)
            || string.IsNullOrWhiteSpace(context.Username)
            || context.ActiveTenantId is not { } tenantId)
            return GenericResult<UiFeedbackCaller>.Failure(MessagingLog.UiFeedbackCallerRequired(_logger, operation));

        return GenericResult<UiFeedbackCaller>.Success(new UiFeedbackCaller(userId, context.Username, tenantId, ExecutionTenantScope.For(context)));
    }

    // A comment the caller does not reach does not exist for them.
    private async Task<IGenericResult<UiFeedbackRow>> Reachable(UiFeedbackCaller caller, Guid id, CancellationToken cancellationToken)
    {
        var found = await _store.FindRow(id, cancellationToken).ConfigureAwait(false);
        if (!found.IsSuccess)
            return found.ToNewResult<UiFeedbackRow>();

        return found.Value is { } row && caller.Scope.Reaches(row.TenantId)
            ? GenericResult<UiFeedbackRow>.Success(row)
            : GenericResult<UiFeedbackRow>.Failure(MessagingLog.UiFeedbackNotFound(_logger, id.ToString("D")));
    }

    private async Task<IGenericResult<UiFeedbackRow>> Own(UiFeedbackCaller caller, Guid id, string action, CancellationToken cancellationToken)
    {
        var row = await Reachable(caller, id, cancellationToken).ConfigureAwait(false);
        return row.IsSuccess && row.Value!.AuthorUserId != caller.UserId
            ? GenericResult<UiFeedbackRow>.Failure(MessagingLog.UiFeedbackNotYours(_logger, id.ToString("D"), caller.Name, action))
            : row;
    }

    private async Task<IGenericResult<UiFeedbackPayload>> Describe(UiFeedbackCaller caller, UiFeedbackRow row, CancellationToken cancellationToken)
    {
        var tags = await _store.FindTags([row.Id], cancellationToken).ConfigureAwait(false);
        if (!tags.IsSuccess)
            return tags.ToNewResult<UiFeedbackPayload>();

        var acknowledged = await _store.FindAcknowledged([row.Id], caller.UserId, cancellationToken).ConfigureAwait(false);
        return acknowledged.IsSuccess
            ? GenericResult<UiFeedbackPayload>.Success(UiFeedbackMapping.ToPayload(row, tags.Value![row.Id], caller.UserId, acknowledged.Value!.Contains(row.Id)))
            : acknowledged.ToNewResult<UiFeedbackPayload>();
    }

    private IGenericResult<UiFeedbackListRequest> ListRequestFor(UiFeedbackCaller caller, UiFeedbackQuery query)
    {
        var canonicalState = string.Empty;
        var canonicalUrgency = string.Empty;
        var canonicalTag = string.Empty;

        var problem = ListWindowProblem(query)
            ?? (query.State is null ? null : UiFeedbackChecks.StateProblem(_logger, query.State, out canonicalState))
            ?? (query.Route is not null && string.IsNullOrWhiteSpace(query.Route) ? MessagingLog.UiFeedbackFieldInvalid(_logger, "route", "is blank; leave it out to list every screen") : null)
            ?? (query.Urgency is null ? null : UiFeedbackChecks.UrgencyProblem(_logger, query.Urgency, out canonicalUrgency))
            ?? (query.Tag is null ? null : UiFeedbackChecks.TagProblem(_logger, query.Tag, out canonicalTag));
        if (problem is not null)
            return GenericResult<UiFeedbackListRequest>.Failure(problem);

        var filter = new UiFeedbackFilter
        {
            TenantId = caller.Scope.ReachesAll ? null : caller.Scope.TenantId,
            State = query.State is null ? null : canonicalState,
            Route = query.Route,
            Urgency = query.Urgency is null ? null : canonicalUrgency
        };
        return GenericResult<UiFeedbackListRequest>.Success(
            new UiFeedbackListRequest(filter, query.Tag is null ? null : canonicalTag, query.SortByUrgency, query.Skip, query.Take));
    }

    private IGenericMessage? ListWindowProblem(UiFeedbackQuery query)
    {
        if (query.Skip < 0)
            return MessagingLog.UiFeedbackFieldInvalid(_logger, "skip", "must not be negative");

        return query.Take is < 1 or > UiFeedbackLimits.MaxPageSize
            ? MessagingLog.UiFeedbackFieldInvalid(_logger, "take", $"must be from 1 to {UiFeedbackLimits.MaxPageSize}")
            : null;
    }

    private IGenericResult<IReadOnlyList<UiFeedbackPayload>> Page(
        UiFeedbackCaller caller,
        UiFeedbackListRequest request,
        IReadOnlyList<UiFeedbackRow> rows,
        ILookup<Guid, string> tags,
        HashSet<Guid> acknowledged)
    {
        var payloads = rows.Select(row => UiFeedbackMapping.ToPayload(row, tags[row.Id], caller.UserId, acknowledged.Contains(row.Id)));
        if (request.Tag is not null)
            payloads = payloads.Where(payload => payload.Tags.Contains(request.Tag, StringComparer.Ordinal));

        if (request.ByUrgency)
        {
            // Why checked here: a comment saved with an urgency the collection no longer declares has no rank, and ordering it anywhere would be a guess.
            if (rows.FirstOrDefault(row => ReferenceEquals(UiFeedbackUrgencies.ByName(row.Urgency), UiFeedbackUrgencies.NotFound)) is { } undeclared)
                return GenericResult<IReadOnlyList<UiFeedbackPayload>>.Failure(MessagingLog.UiFeedbackUnexpected(
                    _logger, "list", $"comment {undeclared.Id:D} carries the urgency '{undeclared.Urgency}', which UiFeedbackUrgencies does not declare"));

            payloads = payloads
                .OrderByDescending(payload => UiFeedbackUrgencies.ByName(payload.Urgency).Rank)
                .ThenByDescending(payload => payload.CreatedAt);
        }

        if (request.NeedsWindow)
            payloads = payloads.Skip(request.Skip).Take(request.Take);

        return GenericResult<IReadOnlyList<UiFeedbackPayload>>.Success(payloads.ToList());
    }
}
