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
public sealed class UiFeedbackService : IUiFeedbackService, IAlertSource
{
    // Why a window: a tag filter and an urgency order cannot be put to the store, so they look at the newest comments that match the other conditions.
    private const int WindowSize = 1000;

    // The permission that makes a person a reader of comments, and the type of the message that tells them.
    private const string ReadPermission = "ui-feedback:read";
    private const string WritePermission = "ui-feedback:write";
    private const string AlertType = "UiFeedback";

    private readonly ILogger<UiFeedbackService> _logger;
    private readonly UiFeedbackStore _store;
    private readonly AlertStore _alerts;
    private readonly IAuthenticationContextAccessor _authContext;
    private readonly IPermissionHolders _holders;
    private readonly IMessageService _messages;
    private readonly IUserNames _userNames;

    /// <summary>
    /// Initializes a new instance of the <see cref="UiFeedbackService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="dataGateways">Resolves the data gateway when a call is made.</param>
    /// <param name="messaging">Resolves where this deployment keeps its messaging data.</param>
    /// <param name="authContext">Supplies the caller, whose identity and tenant every comment carries and whose tenant scopes every read.</param>
    /// <param name="userNames">Reads the caller's user name from their account, for the rows that record who acted (FDW-2199).</param>
    /// <param name="holders">Says who holds the permission to read comments, the people a new comment is told to.</param>
    /// <param name="messages">Tells each of them, and marks an alert read when it is acknowledged.</param>
    public UiFeedbackService(
        ILogger<UiFeedbackService> logger,
        IDataGatewayProvider dataGateways,
        IMessagingConfigurationProvider messaging,
        IAuthenticationContextAccessor authContext,
        IUserNames userNames,
        IPermissionHolders holders,
        IMessageService messages)
    {
        _logger = logger ?? NullLogger<UiFeedbackService>.Instance;
        _store = new UiFeedbackStore(_logger, dataGateways, messaging);
        _alerts = new AlertStore(_logger, dataGateways, messaging);
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        _userNames = userNames ?? throw new ArgumentNullException(nameof(userNames));
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

            var author = await _userNames.Get(caller.Value.UserId, cancellationToken).ConfigureAwait(false);
            if (!author.IsSuccess)
                return author.ToNewResult<UiFeedbackPayload>();

            var row = UiFeedbackMapping.NewRow(caller.Value, author.Value!, request, picture is not null);
            var tags = UiFeedbackMapping.TagNames(request);
            var saved = await _store.Add(
                row,
                tags.Select(tag => new UiFeedbackTagRow { FeedbackId = row.Id, Tag = tag }).ToList(),
                UiFeedbackMapping.AnchorRows(row.Id, request),
                picture is null ? null : new UiFeedbackScreenshotRow { FeedbackId = row.Id, ContentType = picture.Value!.ContentType, Picture = picture.Value!.Content },
                cancellationToken).ConfigureAwait(false);
            if (!saved.IsSuccess)
                return saved.ToNewResult<UiFeedbackPayload>();

            MessagingLog.UiFeedbackSaved(_logger, row.Id.ToString("D"), row.AuthorName, row.Route, row.Urgency);

            // Why the comment is answered as saved whatever happens next: it is kept, and a reader who could not be told is a failure to report beside it, not a
            // reason to lose it.
            var untold = await TellReaders(row, cancellationToken).ConfigureAwait(false);
            return GenericResult<UiFeedbackPayload>.Success(UiFeedbackMapping.ToPayload(row, tags, request.AnchorsJson!, caller.Value.UserId, false, new UiFeedbackAgreements(Enumerable.Empty<UiFeedbackAgreementRow>().ToLookup(agreement => agreement.FeedbackId)), UiFeedbackCommentCounts.None), untold);
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

            List<UiFeedbackFilter> reads = caller.Value.SeesAll ? [request.Value.Filter] : NarrowedFilters(request.Value.Filter, caller.Value.UserId);
            var windowed = request.Value.NeedsWindow || reads.Count > 1;
            var rows = await ReadRows(reads, request.Value, windowed, cancellationToken).ConfigureAwait(false);
            if (!rows.IsSuccess)
                return rows.ToNewResult<IReadOnlyList<UiFeedbackPayload>>();

            // Why checked again after the query narrowed by tenant and by who may see what: a query built with no tenant would return everyone's comments, so the
            // rule the single read applies (Reachable) is applied to every row of the page, and a row the caller may not see fails the list loudly.
            foreach (var row in rows.Value!)
            {
                var seen = MaySee(caller.Value, row);
                if (!seen.IsSuccess)
                    return seen.ToNewResult<IReadOnlyList<UiFeedbackPayload>>();

                if (!seen.Value)
                    return GenericResult<IReadOnlyList<UiFeedbackPayload>>.Failure(MessagingLog.UiFeedbackUnexpected(
                        _logger, "list", $"the store returned comment {row.Id:D}, which the caller may not see"));
            }

            var ids = rows.Value!.Select(row => row.Id).ToList();
            var tags = await _store.FindTags(ids, cancellationToken).ConfigureAwait(false);
            if (!tags.IsSuccess)
                return tags.ToNewResult<IReadOnlyList<UiFeedbackPayload>>();

            var anchors = await _store.FindAnchors(ids, cancellationToken).ConfigureAwait(false);
            if (!anchors.IsSuccess)
                return anchors.ToNewResult<IReadOnlyList<UiFeedbackPayload>>();

            var acknowledged = await _alerts.FindAlertsActedOn(UiFeedbackAlert.Kind, ids, caller.Value.UserId, UiFeedbackAlert.AcknowledgeAction, cancellationToken).ConfigureAwait(false);
            if (!acknowledged.IsSuccess)
                return acknowledged.ToNewResult<IReadOnlyList<UiFeedbackPayload>>();

            var agreements = await _store.FindAgreements(ids, cancellationToken).ConfigureAwait(false);
            if (!agreements.IsSuccess)
                return agreements.ToNewResult<IReadOnlyList<UiFeedbackPayload>>();

            var comments = await _store.FindCommentCounts(ids, cancellationToken).ConfigureAwait(false);
            return comments.IsSuccess
                ? Page(caller.Value, request.Value, windowed, rows.Value!, tags.Value!, anchors.Value!, acknowledged.Value!, agreements.Value!, comments.Value!)
                : comments.ToNewResult<IReadOnlyList<UiFeedbackPayload>>();
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
            MessagingLog.UiFeedbackChanged(_logger, id.ToString("D"), "edited", caller.Value.UserId.ToString("D"));
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
                MessagingLog.UiFeedbackChanged(_logger, id.ToString("D"), "deleted", caller.Value.UserId.ToString("D"));

            return removed;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return GenericResult.Failure(MessagingLog.UiFeedbackUnexpected(_logger, "delete", ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<IGenericResult<UiFeedbackCommentPayload>> AddComment(Guid id, string? body, CancellationToken cancellationToken = default)
    {
        MessagingLog.TraceUiFeedbackEntry(_logger, "AddComment");

        try
        {
            var caller = Caller("comment on");
            if (!caller.IsSuccess)
                return caller.ToNewResult<UiFeedbackCommentPayload>();

            var note = await Reachable(caller.Value, id, cancellationToken).ConfigureAwait(false);
            if (!note.IsSuccess)
                return note.ToNewResult<UiFeedbackCommentPayload>();

            // Why the text is checked after the note is found: a person who may not see the note learns nothing from the refusal, not even that it has a rule for text.
            if (UiFeedbackChecks.BodyProblem(_logger, body) is { } problem)
                return GenericResult<UiFeedbackCommentPayload>.Failure(problem);

            var author = await _userNames.Get(caller.Value.UserId, cancellationToken).ConfigureAwait(false);
            if (!author.IsSuccess)
                return author.ToNewResult<UiFeedbackCommentPayload>();

            var comment = UiFeedbackMapping.NewComment(caller.Value, author.Value!, note.Value!, body!);
            var written = await _store.AddComment(comment, cancellationToken).ConfigureAwait(false);
            if (!written.IsSuccess)
                return written.ToNewResult<UiFeedbackCommentPayload>();

            MessagingLog.UiFeedbackCommented(_logger, id.ToString("D"), author.Value!);
            return GenericResult<UiFeedbackCommentPayload>.Success(UiFeedbackMapping.ToCommentPayload(comment));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return GenericResult<UiFeedbackCommentPayload>.Failure(MessagingLog.UiFeedbackUnexpected(_logger, "comment on", ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<UiFeedbackCommentPayload>>> ListComments(Guid id, int skip, int take, CancellationToken cancellationToken = default)
    {
        MessagingLog.TraceUiFeedbackEntry(_logger, "ListComments");

        try
        {
            var caller = Caller("list the comments on");
            if (!caller.IsSuccess)
                return caller.ToNewResult<IReadOnlyList<UiFeedbackCommentPayload>>();

            var note = await Reachable(caller.Value, id, cancellationToken).ConfigureAwait(false);
            if (!note.IsSuccess)
                return note.ToNewResult<IReadOnlyList<UiFeedbackCommentPayload>>();

            if (WindowProblem(skip, take) is { } problem)
                return GenericResult<IReadOnlyList<UiFeedbackCommentPayload>>.Failure(problem);

            var rows = await _store.FindComments(id, skip, take, cancellationToken).ConfigureAwait(false);
            return rows.IsSuccess
                ? GenericResult<IReadOnlyList<UiFeedbackCommentPayload>>.Success([.. rows.Value!.Select(UiFeedbackMapping.ToCommentPayload)])
                : rows.ToNewResult<IReadOnlyList<UiFeedbackCommentPayload>>();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return GenericResult<IReadOnlyList<UiFeedbackCommentPayload>>.Failure(MessagingLog.UiFeedbackUnexpected(_logger, "list the comments on", ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<IGenericResult<UiFeedbackPayload>> Agree(Guid id, CancellationToken cancellationToken = default)
    {
        MessagingLog.TraceUiFeedbackEntry(_logger, "Agree");

        try
        {
            var caller = Caller("agree with");
            if (!caller.IsSuccess)
                return caller.ToNewResult<UiFeedbackPayload>();

            var row = await Reachable(caller.Value, id, cancellationToken).ConfigureAwait(false);
            if (!row.IsSuccess)
                return row.ToNewResult<UiFeedbackPayload>();

            // Why refused: the person who left the comment is already counted by having written it, so their agreement would count them twice.
            if (row.Value!.AuthorUserId == caller.Value.UserId)
                return GenericResult<UiFeedbackPayload>.Failure(MessagingLog.UiFeedbackOwnComment(_logger, id.ToString("D"), caller.Value.UserId.ToString("D")));

            var held = await _store.FindAgreements([id], cancellationToken).ConfigureAwait(false);
            if (!held.IsSuccess)
                return held.ToNewResult<UiFeedbackPayload>();

            if (held.Value!.AgreedBy(id, caller.Value.UserId))
            {
                MessagingLog.UiFeedbackAlreadyAgreed(_logger, id.ToString("D"), caller.Value.UserId.ToString("D"));
            }
            else
            {
                var agreeing = await _userNames.Get(caller.Value.UserId, cancellationToken).ConfigureAwait(false);
                if (!agreeing.IsSuccess)
                    return agreeing.ToNewResult<UiFeedbackPayload>();

                var written = await _store.AddAgreement(
                    new UiFeedbackAgreementRow { FeedbackId = id, UserId = caller.Value.UserId, UserName = agreeing.Value!, AgreedAt = DateTimeOffset.UtcNow },
                    cancellationToken).ConfigureAwait(false);
                if (!written.IsSuccess)
                {
                    // Why asked again: two presses of Agree at once both find no agreement and both write, and the second is refused by the table's unique key. If the
                    // agreement is there now, the person did agree and the answer is the same as for a repeat; if it is not, the write really failed.
                    var now = await _store.FindAgreements([id], cancellationToken).ConfigureAwait(false);
                    if (!now.IsSuccess || !now.Value!.AgreedBy(id, caller.Value.UserId))
                        return written.ToNewResult<UiFeedbackPayload>();
                }

                MessagingLog.UiFeedbackAgreed(_logger, id.ToString("D"), agreeing.Value!);
            }

            return await Describe(caller.Value, row.Value, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return GenericResult<UiFeedbackPayload>.Failure(MessagingLog.UiFeedbackUnexpected(_logger, "agree with", ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<IGenericResult<UiFeedbackPayload>> TakeBackAgreement(Guid id, CancellationToken cancellationToken = default)
    {
        MessagingLog.TraceUiFeedbackEntry(_logger, "TakeBackAgreement");

        try
        {
            var caller = Caller("take back the agreement with");
            if (!caller.IsSuccess)
                return caller.ToNewResult<UiFeedbackPayload>();

            var row = await Reachable(caller.Value, id, cancellationToken).ConfigureAwait(false);
            if (!row.IsSuccess)
                return row.ToNewResult<UiFeedbackPayload>();

            var removed = await _store.RemoveAgreement(id, caller.Value.UserId, cancellationToken).ConfigureAwait(false);
            if (!removed.IsSuccess)
                return removed.ToNewResult<UiFeedbackPayload>();

            MessagingLog.UiFeedbackAgreementTakenBack(_logger, id.ToString("D"), caller.Value.UserId.ToString("D"));
            return await Describe(caller.Value, row.Value!, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return GenericResult<UiFeedbackPayload>.Failure(MessagingLog.UiFeedbackUnexpected(_logger, "take back the agreement with", ex.Message));
        }
    }

    /// <inheritdoc />
    public string Kind => UiFeedbackAlert.Kind;

    /// <inheritdoc />
    // Why every action for every comment: the reference default; a company's own choice of the set is read elsewhere (FDW-2430).
    public IReadOnlyList<string> OfferedActions { get; } = ["Acknowledge", "Snooze", "Silence", "Dismiss", "Discard"];

    /// <inheritdoc />
    public async Task<IGenericResult<AlertSubject?>> Find(Guid id, CancellationToken cancellationToken = default)
    {
        MessagingLog.TraceUiFeedbackEntry(_logger, "Find an alert of");

        try
        {
            var caller = Caller("find an alert of");
            if (!caller.IsSuccess)
                return caller.ToNewResult<AlertSubject?>();

            // Why a right: the alert routes ask only for a signed-in person, so a comment's alert is shown to someone who holds one of the rights to see comments.
            if (_authContext.Current is not { } context
                || (!PermissionCoverage.Covers(context.Permissions, ReadPermission) && !PermissionCoverage.Covers(context.Permissions, WritePermission)))
                return GenericResult<AlertSubject?>.Success(null);

            var found = await _store.FindRow(id, cancellationToken).ConfigureAwait(false);
            if (!found.IsSuccess)
                return found.ToNewResult<AlertSubject?>();

            if (found.Value is not { } row)
                return GenericResult<AlertSubject?>.Success(null);

            var seen = MaySee(caller.Value, row);
            if (!seen.IsSuccess)
                return seen.ToNewResult<AlertSubject?>();

            return GenericResult<AlertSubject?>.Success(seen.Value ? new AlertSubject(row.Id, row.TenantId) : null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return GenericResult<AlertSubject?>.Failure(MessagingLog.UiFeedbackUnexpected(_logger, "find an alert of", ex.Message));
        }
    }

    /// <inheritdoc />
    // Why read after any action: a person who acknowledges, silences, snoozes or dismisses an alert has dealt with the notice that told them of it, so it is
    // marked read; also when it was already, so a notice left unread by an earlier failure is put right by acting again.
    public async Task<IGenericResult> Acted(Guid id, IAlertAction action, CancellationToken cancellationToken = default)
    {
        MessagingLog.TraceUiFeedbackEntry(_logger, "Acted on an alert of");

        try
        {
            var caller = Caller("act on an alert of");
            if (!caller.IsSuccess)
                return (IGenericResult)caller;

            return action.ForEveryone
                ? GenericResult.Success()
                : await MarkAlertsRead(id, caller.Value, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return GenericResult.Failure(MessagingLog.UiFeedbackUnexpected(_logger, "act on an alert of", ex.Message));
        }
    }

    /// <inheritdoc />
    public Task<IGenericResult<UiFeedbackPayload>> Resolve(Guid id, CancellationToken cancellationToken = default)
        => Move(id, "Resolve", "Open", "Resolved", "resolved", true, cancellationToken);

    /// <inheritdoc />
    public Task<IGenericResult<UiFeedbackPayload>> Reopen(Guid id, CancellationToken cancellationToken = default)
        => Move(id, "Reopen", "Resolved", "Open", "reopened", false, cancellationToken);

    private async Task<IGenericResult<UiFeedbackPayload>> Move(Guid id, string operation, string stateNow, string stateNext, string change, bool resolving, CancellationToken cancellationToken)
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

            // Why the name is read only when resolving: reopening clears who resolved the comment and records no name.
            string? resolverName = null;
            if (resolving)
            {
                var resolver = await _userNames.Get(caller.Value.UserId, cancellationToken).ConfigureAwait(false);
                if (!resolver.IsSuccess)
                    return resolver.ToNewResult<UiFeedbackPayload>();

                resolverName = resolver.Value;
            }

            var update = new UiFeedbackStateUpdate
            {
                State = stateNext,
                ResolvedByUserId = resolving ? caller.Value.UserId : null,
                ResolvedByName = resolverName,
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
            MessagingLog.UiFeedbackChanged(_logger, id.ToString("D"), change, caller.Value.UserId.ToString("D"));
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

        // Each lookup stands on its own: a failed one is reported, and the readers the other found are still told.
        var readers = new HashSet<Guid>();
        var inTenant = await _holders.Get(ReadPermission, comment.TenantId, cancellationToken).ConfigureAwait(false);
        var onPlatform = await _holders.GetOnPlatform(ReadPermission, cancellationToken).ConfigureAwait(false);
        foreach (var lookup in new[] { inTenant, onPlatform })
        {
            if (lookup.IsSuccess && lookup.Value is { } found)
                readers.UnionWith(found);
            else
                untold.Add(MessagingLog.UiFeedbackReadersNotFound(_logger, feedbackId, lookup.CurrentMessage));
        }

        readers.Remove(comment.AuthorUserId);

        var told = 0;
        foreach (var reader in readers)
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
            || context.ActiveTenantId is not { } tenantId)
            return GenericResult<UiFeedbackCaller>.Failure(MessagingLog.UiFeedbackCallerRequired(_logger, operation));

        return GenericResult<UiFeedbackCaller>.Success(new UiFeedbackCaller(userId, tenantId, ExecutionTenantScope.For(context), PermissionCoverage.Covers(context.Permissions, ReadPermission)));
    }

    // A comment the caller does not reach does not exist for them.
    private async Task<IGenericResult<UiFeedbackRow>> Reachable(UiFeedbackCaller caller, Guid id, CancellationToken cancellationToken)
    {
        var found = await _store.FindRow(id, cancellationToken).ConfigureAwait(false);
        if (!found.IsSuccess)
            return found.ToNewResult<UiFeedbackRow>();

        if (found.Value is not { } row)
            return GenericResult<UiFeedbackRow>.Failure(MessagingLog.UiFeedbackNotFound(_logger, id.ToString("D")));

        var seen = MaySee(caller, row);
        if (!seen.IsSuccess)
            return seen.ToNewResult<UiFeedbackRow>();

        return seen.Value
            ? GenericResult<UiFeedbackRow>.Success(row)
            : GenericResult<UiFeedbackRow>.Failure(MessagingLog.UiFeedbackNotFound(_logger, id.ToString("D")));
    }

    // Whether the caller may see a comment: it is in their company's reach, and they either hold the read right, wrote it, or it is not a security issue.
    // A security issue is an urgency whose collection entry needs acknowledging, never one picked out by its name (FDW-2246).
    private IGenericResult<bool> MaySee(UiFeedbackCaller caller, UiFeedbackRow row)
    {
        if (!caller.Scope.Reaches(row.TenantId))
            return GenericResult<bool>.Success(false);

        if (caller.SeesAll || row.AuthorUserId == caller.UserId)
            return GenericResult<bool>.Success(true);

        var urgency = UiFeedbackUrgencies.ByName(row.Urgency);
        return ReferenceEquals(urgency, UiFeedbackUrgencies.NotFound)
            ? GenericResult<bool>.Failure(MessagingLog.UiFeedbackUnexpected(
                _logger, "decide who may see", $"comment {row.Id:D} carries the urgency '{row.Urgency}', which UiFeedbackUrgencies does not declare"))
            : GenericResult<bool>.Success(!urgency.NeedsAcknowledgement);
    }

    // The reads that together give a caller without the read right what they may see: the comments of every urgency that is not a security issue, and their own
    // comments of the urgencies that are. The two sets do not overlap, so their union has no duplicate.
    private static List<UiFeedbackFilter> NarrowedFilters(UiFeedbackFilter filter, Guid userId)
    {
        if (filter.Urgency is not null)
        {
            var stated = filter.Copy();
            if (UiFeedbackUrgencies.ByName(filter.Urgency).NeedsAcknowledgement)
                stated.AuthorUserId = userId;

            return [stated];
        }

        var safe = filter.Copy();
        safe.Urgencies = [.. UiFeedbackUrgencies.All().Where(urgency => !urgency.NeedsAcknowledgement).Select(urgency => urgency.Name)];
        var reads = new List<UiFeedbackFilter> { safe };

        var security = UiFeedbackUrgencies.All().Where(urgency => urgency.NeedsAcknowledgement).Select(urgency => urgency.Name).ToList();
        if (security.Count > 0)
        {
            var own = filter.Copy();
            own.Urgencies = security;
            own.AuthorUserId = userId;
            reads.Add(own);
        }

        return reads;
    }

    private async Task<IGenericResult<IReadOnlyList<UiFeedbackRow>>> ReadRows(
        List<UiFeedbackFilter> reads, UiFeedbackListRequest request, bool windowed, CancellationToken cancellationToken)
    {
        if (reads.Count == 1)
            return windowed
                ? await _store.FindRows(reads[0], 0, WindowSize, cancellationToken).ConfigureAwait(false)
                : await _store.FindRows(reads[0], request.Skip, request.Take, cancellationToken).ConfigureAwait(false);

        var merged = new List<UiFeedbackRow>();
        foreach (var read in reads)
        {
            var rows = await _store.FindRows(read, 0, WindowSize, cancellationToken).ConfigureAwait(false);
            if (!rows.IsSuccess)
                return rows;

            merged.AddRange(rows.Value!);
        }

        return GenericResult<IReadOnlyList<UiFeedbackRow>>.Success([.. merged.DistinctBy(row => row.Id).OrderByDescending(row => row.CreatedAt).ThenBy(row => row.Id)]);
    }

    private async Task<IGenericResult<UiFeedbackRow>> Own(UiFeedbackCaller caller, Guid id, string action, CancellationToken cancellationToken)
    {
        var row = await Reachable(caller, id, cancellationToken).ConfigureAwait(false);
        return row.IsSuccess && row.Value!.AuthorUserId != caller.UserId
            ? GenericResult<UiFeedbackRow>.Failure(MessagingLog.UiFeedbackNotYours(_logger, id.ToString("D"), caller.UserId.ToString("D"), action))
            : row;
    }

    private async Task<IGenericResult<UiFeedbackPayload>> Describe(UiFeedbackCaller caller, UiFeedbackRow row, CancellationToken cancellationToken)
    {
        var tags = await _store.FindTags([row.Id], cancellationToken).ConfigureAwait(false);
        if (!tags.IsSuccess)
            return tags.ToNewResult<UiFeedbackPayload>();

        var anchors = await _store.FindAnchors([row.Id], cancellationToken).ConfigureAwait(false);
        if (!anchors.IsSuccess)
            return anchors.ToNewResult<UiFeedbackPayload>();

        var acknowledged = await _alerts.FindAlertsActedOn(UiFeedbackAlert.Kind, [row.Id], caller.UserId, UiFeedbackAlert.AcknowledgeAction, cancellationToken).ConfigureAwait(false);
        if (!acknowledged.IsSuccess)
            return acknowledged.ToNewResult<UiFeedbackPayload>();

        var agreements = await _store.FindAgreements([row.Id], cancellationToken).ConfigureAwait(false);
        if (!agreements.IsSuccess)
            return agreements.ToNewResult<UiFeedbackPayload>();

        var comments = await _store.FindCommentCounts([row.Id], cancellationToken).ConfigureAwait(false);
        return comments.IsSuccess
            ? GenericResult<UiFeedbackPayload>.Success(
                UiFeedbackMapping.ToPayload(row, tags.Value![row.Id], anchors.Value![row.Id], caller.UserId, acknowledged.Value!.Contains(row.Id), agreements.Value!, comments.Value!))
            : comments.ToNewResult<UiFeedbackPayload>();
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

    private IGenericMessage? ListWindowProblem(UiFeedbackQuery query) => WindowProblem(query.Skip, query.Take);

    private IGenericMessage? WindowProblem(int skip, int take)
    {
        if (skip < 0)
            return MessagingLog.UiFeedbackFieldInvalid(_logger, "skip", "must not be negative");

        return take is < 1 or > UiFeedbackLimits.MaxPageSize
            ? MessagingLog.UiFeedbackFieldInvalid(_logger, "take", $"must be from 1 to {UiFeedbackLimits.MaxPageSize}")
            : null;
    }

    private IGenericResult<IReadOnlyList<UiFeedbackPayload>> Page(
        UiFeedbackCaller caller,
        UiFeedbackListRequest request,
        bool windowed,
        IReadOnlyList<UiFeedbackRow> rows,
        ILookup<Guid, string> tags,
        ILookup<Guid, string> anchors,
        HashSet<Guid> acknowledged,
        UiFeedbackAgreements agreements,
        UiFeedbackCommentCounts comments)
    {
        var payloads = rows.Select(row => UiFeedbackMapping.ToPayload(row, tags[row.Id], anchors[row.Id], caller.UserId, acknowledged.Contains(row.Id), agreements, comments));
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

        if (windowed)
            payloads = payloads.Skip(request.Skip).Take(request.Take);

        return GenericResult<IReadOnlyList<UiFeedbackPayload>>.Success(payloads.ToList());
    }
}
