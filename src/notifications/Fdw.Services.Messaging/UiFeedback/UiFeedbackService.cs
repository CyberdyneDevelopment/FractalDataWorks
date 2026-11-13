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
using Fdw.Services.Settings;
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
    private readonly UiFeedbackAnswerers _answerers;
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
    /// <param name="settings">The host's settings, which say who answers the questions left on each screen
    /// (<see cref="SettingDefinitions.UiFeedbackQuestionAnswerers"/>); <see langword="null"/> in a host that registers none.</param>
    public UiFeedbackService(
        ILogger<UiFeedbackService> logger,
        IDataGatewayProvider dataGateways,
        IMessagingConfigurationProvider messaging,
        IAuthenticationContextAccessor authContext,
        IUserNames userNames,
        IPermissionHolders holders,
        IMessageService messages,
        Lazy<IEffectiveSettingsProvider>? settings = null)
    {
        _logger = logger ?? NullLogger<UiFeedbackService>.Instance;
        _store = new UiFeedbackStore(_logger, dataGateways, messaging);
        _alerts = new AlertStore(_logger, dataGateways, messaging);
        _answerers = new UiFeedbackAnswerers(_logger, settings);
        _authContext = authContext;
        _userNames = userNames;
        _holders = holders;
        _messages = messages;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<UiFeedbackPayload>> Create(CreateUiFeedbackRequest request, CancellationToken cancellationToken = default)
    {
        MessagingLog.TraceUiFeedbackEntry(_logger, "Create");

        try
        {
            var caller = Caller("save");
            if (!caller.IsSuccess)
                return caller.ToNewResult<UiFeedbackPayload>();

            if (UiFeedbackChecks.Problem(_logger, request) is { } problem)
                return GenericResult<UiFeedbackPayload>.Failure(problem);

            // Why the route asks only for a signed-in person: anyone may ask a question, so the right to leave any other comment is asked here, where the
            // tags are known. A question is a comment whose every tag asks for an answer, and there is at least one.
            var tags = UiFeedbackMapping.TagNames(request);
            var question = IsQuestion(tags);
            if (!caller.Value.Writes && !(question && tags.All(tag => UiFeedbackTags.ByName(tag).AsksForAnAnswer)))
                return GenericResult<UiFeedbackPayload>.Failure(MessagingLog.UiFeedbackOnlyQuestions(_logger, caller.Value.UserId.ToString("D"), WritePermission));

            var picture = request.Screenshot is null ? null : UiFeedbackPictures.Decode(_logger, request.Screenshot);
            if (picture is { IsSuccess: false })
                return picture.ToNewResult<UiFeedbackPayload>();

            var author = await _userNames.Get(caller.Value.UserId, cancellationToken).ConfigureAwait(false);
            if (!author.IsSuccess)
                return author.ToNewResult<UiFeedbackPayload>();

            var row = UiFeedbackMapping.NewRow(caller.Value, author.Value!, request, picture is not null);
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
            var untold = await Tell(row, question, caller.Value.OrganizationId, cancellationToken).ConfigureAwait(false);
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

        try
        {
            var caller = Caller("list");
            if (!caller.IsSuccess)
                return caller.ToNewResult<IReadOnlyList<UiFeedbackPayload>>();

            var request = ListRequestFor(caller.Value, query);
            if (!request.IsSuccess)
                return request.ToNewResult<IReadOnlyList<UiFeedbackPayload>>();

            var visible = await VisibleRows(caller.Value, query, request.Value, cancellationToken).ConfigureAwait(false);
            if (!visible.IsSuccess)
                return visible.ToNewResult<IReadOnlyList<UiFeedbackPayload>>();

            var (rows, tags, windowed) = visible.Value;
            var ids = rows.Select(row => row.Id).ToList();

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
                ? Page(caller.Value, request.Value, windowed, rows, tags, anchors.Value!, acknowledged.Value!, agreements.Value!, comments.Value!)
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

            // Why asked here and not by the route: a person without the right to leave comments answers a question put to them, and replies on one they asked.
            if (!caller.Value.Writes)
            {
                var engaged = await InQuestion(caller.Value, note.Value!, cancellationToken).ConfigureAwait(false);
                if (!engaged.IsSuccess)
                    return engaged.ToNewResult<UiFeedbackCommentPayload>();

                if (!engaged.Value)
                    return GenericResult<UiFeedbackCommentPayload>.Failure(
                        MessagingLog.UiFeedbackMayNotComment(_logger, id.ToString("D"), caller.Value.UserId.ToString("D"), WritePermission));
            }

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

            // Why no right is asked first: the alert routes ask only for a signed-in person, and a comment's alert is shown to whoever may see the comment, which
            // a person with neither right to see comments may only when it is a question put to them or one they asked.
            var found = await _store.FindRow(id, cancellationToken).ConfigureAwait(false);
            if (!found.IsSuccess)
                return found.ToNewResult<AlertSubject?>();

            if (found.Value is not { } row)
                return GenericResult<AlertSubject?>.Success(null);

            var seen = await Sees(caller.Value, row, cancellationToken).ConfigureAwait(false);
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

    // Tells the readers of comments and, for a question, the people who can answer for its screen. Returns the failures to report beside the saved comment.
    private async Task<IReadOnlyList<IGenericMessage>> Tell(UiFeedbackRow comment, bool question, Guid? organizationId, CancellationToken cancellationToken)
    {
        var untold = new List<IGenericMessage>();
        var readers = await TellReaders(comment, question, untold, cancellationToken).ConfigureAwait(false);
        if (question)
            await TellAnswerers(comment, organizationId, readers, untold, cancellationToken).ConfigureAwait(false);

        return untold;
    }

    // Tells each person who holds the permission that answers for the question's screen, in the question's tenant and, when it was asked in an organization,
    // through what that organization grants them; not the author, and not a reader, who was told already. One answerer who could not be told does not stop
    // the others, and nothing here fails the saved question.
    private async Task TellAnswerers(UiFeedbackRow comment, Guid? organizationId, HashSet<Guid> readers, List<IGenericMessage> untold, CancellationToken cancellationToken)
    {
        var feedbackId = comment.Id.ToString("D");
        var permission = _answerers.PermissionFor(comment.Route, comment.TenantId);
        if (!permission.IsSuccess)
        {
            untold.AddRange(permission.Messages);
            return;
        }

        if (permission.Value is not { } answering)
            return;

        var lookups = new List<IGenericResult<IReadOnlyList<Guid>>>
        {
            await _holders.Get(answering, comment.TenantId, cancellationToken).ConfigureAwait(false)
        };
        if (organizationId is { } organization)
            lookups.Add(await _holders.GetInOrganization(answering, comment.TenantId, organization, cancellationToken).ConfigureAwait(false));

        var answerers = new HashSet<Guid>();
        foreach (var lookup in lookups)
        {
            if (lookup.IsSuccess && lookup.Value is { } found)
                answerers.UnionWith(found);
            else
                untold.Add(MessagingLog.UiFeedbackAnswerersNotFound(_logger, feedbackId, answering, lookup.CurrentMessage));
        }

        answerers.Remove(comment.AuthorUserId);
        answerers.ExceptWith(readers);

        var told = 0;
        foreach (var answerer in answerers)
        {
            var sent = await _messages.CreateMessage(AlertFor(comment, answerer, true), cancellationToken).ConfigureAwait(false);
            if (sent.IsSuccess)
                told++;
            else
                untold.Add(MessagingLog.UiFeedbackAnswererNotTold(_logger, feedbackId, answerer.ToString("D"), sent.CurrentMessage));
        }

        MessagingLog.UiFeedbackAnswerersTold(_logger, feedbackId, told, answering);
    }

    // Tells each person who can read comments, in the comment's tenant and on the platform, except the author, and answers who they are. The failures go to
    // untold, to report beside the saved comment: one reader who could not be told does not stop the others from being told.
    private async Task<HashSet<Guid>> TellReaders(UiFeedbackRow comment, bool question, List<IGenericMessage> untold, CancellationToken cancellationToken)
    {
        var feedbackId = comment.Id.ToString("D");

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
            var sent = await _messages.CreateMessage(AlertFor(comment, reader, question), cancellationToken).ConfigureAwait(false);
            if (sent.IsSuccess)
                told++;
            else
                untold.Add(MessagingLog.UiFeedbackReaderNotTold(_logger, feedbackId, reader.ToString("D"), sent.CurrentMessage));
        }

        MessagingLog.UiFeedbackReadersTold(_logger, feedbackId, told);
        return readers;
    }

    private static CreateMessageRequest AlertFor(UiFeedbackRow comment, Guid reader, bool question)
    {
        var id = comment.Id.ToString("D");
        return new CreateMessageRequest
        {
            TenantId = comment.TenantId,
            RecipientUserId = reader,
            SenderUserId = comment.AuthorUserId,
            MessageType = AlertType,
            Severity = UiFeedbackUrgencies.ByName(comment.Urgency).Severity,
            Subject = question ? $"{comment.AuthorName} asked a question on {comment.Route}" : $"{comment.AuthorName} left a UI comment on {comment.Route}",
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

        var permissions = context.Permissions.ToList();
        return GenericResult<UiFeedbackCaller>.Success(new UiFeedbackCaller(
            userId,
            tenantId,
            ExecutionTenantScope.For(context),
            PermissionCoverage.Covers(permissions, ReadPermission),
            PermissionCoverage.Covers(permissions, WritePermission),
            context.ActiveOrgId,
            permissions));
    }

    // A comment the caller does not reach does not exist for them.
    private async Task<IGenericResult<UiFeedbackRow>> Reachable(UiFeedbackCaller caller, Guid id, CancellationToken cancellationToken)
    {
        var found = await _store.FindRow(id, cancellationToken).ConfigureAwait(false);
        if (!found.IsSuccess)
            return found.ToNewResult<UiFeedbackRow>();

        if (found.Value is not { } row)
            return GenericResult<UiFeedbackRow>.Failure(MessagingLog.UiFeedbackNotFound(_logger, id.ToString("D")));

        var seen = await Sees(caller, row, cancellationToken).ConfigureAwait(false);
        if (!seen.IsSuccess)
            return seen.ToNewResult<UiFeedbackRow>();

        return seen.Value
            ? GenericResult<UiFeedbackRow>.Success(row)
            : GenericResult<UiFeedbackRow>.Failure(MessagingLog.UiFeedbackNotFound(_logger, id.ToString("D")));
    }

    // MaySee for one comment, reading its tags only when the rights the caller holds do not decide.
    private async Task<IGenericResult<bool>> Sees(UiFeedbackCaller caller, UiFeedbackRow row, CancellationToken cancellationToken)
    {
        if (!caller.Scope.Reaches(row.TenantId) || caller.SeesAll || row.AuthorUserId == caller.UserId)
            return MaySee(caller, row, []);

        var tags = await _store.FindTags([row.Id], cancellationToken).ConfigureAwait(false);
        return tags.IsSuccess ? MaySee(caller, row, tags.Value![row.Id]) : tags.ToNewResult<bool>();
    }

    // Whether the caller may see a comment: it is in their company's reach, and they hold the read right, wrote it, hold the write right and it is not a
    // security issue, or it is a question put to them. A security issue is an urgency whose collection entry needs acknowledging, never one picked out by its
    // name (FDW-2246).
    private IGenericResult<bool> MaySee(UiFeedbackCaller caller, UiFeedbackRow row, IEnumerable<string> tags)
    {
        if (!caller.Scope.Reaches(row.TenantId))
            return GenericResult<bool>.Success(false);

        if (caller.SeesAll || row.AuthorUserId == caller.UserId)
            return GenericResult<bool>.Success(true);

        if (caller.Writes)
        {
            var urgency = UiFeedbackUrgencies.ByName(row.Urgency);
            if (ReferenceEquals(urgency, UiFeedbackUrgencies.NotFound))
                return GenericResult<bool>.Failure(MessagingLog.UiFeedbackUnexpected(
                    _logger, "decide who may see", $"comment {row.Id:D} carries the urgency '{row.Urgency}', which UiFeedbackUrgencies does not declare"));

            if (!urgency.NeedsAcknowledgement)
                return GenericResult<bool>.Success(true);
        }

        return AskedOf(caller, row, tags);
    }

    // Whether a comment is a question put to the caller: a tag of it asks for an answer, and the caller's session holds the permission the setting maps its
    // route to. The session's permissions are the roles the person holds in the tenant they act in and what the organization they act in grants them.
    private IGenericResult<bool> AskedOf(UiFeedbackCaller caller, UiFeedbackRow row, IEnumerable<string> tags)
    {
        if (!IsQuestion(tags))
            return GenericResult<bool>.Success(false);

        var permission = _answerers.PermissionFor(row.Route, row.TenantId);
        if (!permission.IsSuccess)
            return permission.ToNewResult<bool>();

        return GenericResult<bool>.Success(permission.Value is { } answering && PermissionCoverage.Covers(caller.Permissions, answering));
    }

    // Whether the caller takes part in a question: it is one, and they asked it or it was put to them.
    private async Task<IGenericResult<bool>> InQuestion(UiFeedbackCaller caller, UiFeedbackRow row, CancellationToken cancellationToken)
    {
        var tags = await _store.FindTags([row.Id], cancellationToken).ConfigureAwait(false);
        if (!tags.IsSuccess)
            return tags.ToNewResult<bool>();

        if (!IsQuestion(tags.Value![row.Id]))
            return GenericResult<bool>.Success(false);

        return row.AuthorUserId == caller.UserId ? GenericResult<bool>.Success(true) : AskedOf(caller, row, tags.Value![row.Id]);
    }

    // The comments of a list the caller may see, with their tags, and whether the list pages over a window rather than the store's own paging.
    private async Task<IGenericResult<(IReadOnlyList<UiFeedbackRow> Rows, ILookup<Guid, string> Tags, bool Windowed)>> VisibleRows(
        UiFeedbackCaller caller, UiFeedbackQuery query, UiFeedbackListRequest request, CancellationToken cancellationToken)
    {
        if (query.Mine)
            request.Filter.AuthorUserId = caller.UserId;

        // Why the questions are picked out after the read: which comments were put to the caller follows from their tags and from the setting that maps
        // routes to permissions, neither of which the store can filter by. A caller who holds neither right to see comments sees their own and those.
        var asked = query.AskedOfMe || (!caller.SeesAll && !caller.Writes);
        List<UiFeedbackFilter> reads = caller.SeesAll || asked ? [request.Filter] : NarrowedFilters(request.Filter, caller.UserId);
        var windowed = request.NeedsWindow || reads.Count > 1 || asked;
        var read = await ReadRows(reads, request, windowed, cancellationToken).ConfigureAwait(false);
        if (!read.IsSuccess)
            return read.ToNewResult<(IReadOnlyList<UiFeedbackRow>, ILookup<Guid, string>, bool)>();

        var tags = await _store.FindTags([.. read.Value!.Select(row => row.Id)], cancellationToken).ConfigureAwait(false);
        if (!tags.IsSuccess)
            return tags.ToNewResult<(IReadOnlyList<UiFeedbackRow>, ILookup<Guid, string>, bool)>();

        var rows = asked ? PutToCaller(caller, read.Value!, tags.Value!, query.AskedOfMe) : GenericResult<IReadOnlyList<UiFeedbackRow>>.Success(read.Value!);
        if (!rows.IsSuccess)
            return rows.ToNewResult<(IReadOnlyList<UiFeedbackRow>, ILookup<Guid, string>, bool)>();

        // Why checked again after the query narrowed by tenant and by who may see what: a query built with no tenant would return everyone's comments, so the
        // rule the single read applies (Reachable) is applied to every row of the page, and a row the caller may not see fails the list loudly.
        foreach (var row in rows.Value!)
        {
            var seen = MaySee(caller, row, tags.Value![row.Id]);
            if (!seen.IsSuccess)
                return seen.ToNewResult<(IReadOnlyList<UiFeedbackRow>, ILookup<Guid, string>, bool)>();

            if (!seen.Value)
                return GenericResult<(IReadOnlyList<UiFeedbackRow>, ILookup<Guid, string>, bool)>.Failure(MessagingLog.UiFeedbackUnexpected(
                    _logger, "list", $"the store returned comment {row.Id:D}, which the caller may not see"));
        }

        return GenericResult<(IReadOnlyList<UiFeedbackRow>, ILookup<Guid, string>, bool)>.Success((rows.Value!, tags.Value!, windowed));
    }

    // The comments of a read a caller is shown when only the questions put to them are listed, or when they hold neither right to see comments (then their
    // own as well).
    private IGenericResult<IReadOnlyList<UiFeedbackRow>> PutToCaller(UiFeedbackCaller caller, IReadOnlyList<UiFeedbackRow> rows, ILookup<Guid, string> tags, bool onlyAsked)
    {
        var kept = new List<UiFeedbackRow>();
        foreach (var row in rows)
        {
            // Why a row out of the caller's reach is not dropped here: the list fails loudly on it afterwards, as it does for any read the store got wrong.
            if (!onlyAsked && row.AuthorUserId == caller.UserId)
            {
                kept.Add(row);
                continue;
            }

            var asked = AskedOf(caller, row, tags[row.Id]);
            if (!asked.IsSuccess)
                return asked.ToNewResult<IReadOnlyList<UiFeedbackRow>>();

            if (asked.Value)
                kept.Add(row);
        }

        return GenericResult<IReadOnlyList<UiFeedbackRow>>.Success(kept);
    }

    private static bool IsQuestion(IEnumerable<string> tags) => tags.Any(tag => UiFeedbackTags.ByName(tag).AsksForAnAnswer);

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
