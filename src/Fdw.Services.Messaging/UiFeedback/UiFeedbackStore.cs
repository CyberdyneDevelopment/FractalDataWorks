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
/// Reads and writes the three msg tables of UI comments through the data gateway (FDW-2143). It decides nothing: who may do what, what a field may hold and
/// what a missing comment means are the service's.
/// </summary>
internal sealed class UiFeedbackStore
{
    // The row's own name, not a store choice: see MessageService's identical constant.
    private const string MessagingServiceName = "Messaging";
    private const string FeedbackContainer = "UiFeedback";
    private const string TagContainer = "UiFeedbackTag";
    private const string AnchorContainer = "UiFeedbackAnchor";
    private const string ScreenshotContainer = "UiFeedbackScreenshot";
    private const string AgreementContainer = "UiFeedbackAgreement";
    private const string CommentContainer = "UiFeedbackComment";

    private readonly ILogger _logger;
    private readonly IDataGatewayProvider _dataGateways;
    private readonly IMessagingConfigurationProvider _messaging;
    private readonly AlertStore _alerts;

    /// <summary>
    /// Initializes a new instance of the <see cref="UiFeedbackStore"/> class.
    /// </summary>
    /// <param name="logger">The logger a failure is written to.</param>
    /// <param name="dataGateways">Resolves the data gateway when a call is made.</param>
    /// <param name="messaging">Resolves where this deployment keeps its messaging data.</param>
    public UiFeedbackStore(ILogger logger, IDataGatewayProvider dataGateways, IMessagingConfigurationProvider messaging)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dataGateways = dataGateways ?? throw new ArgumentNullException(nameof(dataGateways));
        _messaging = messaging ?? throw new ArgumentNullException(nameof(messaging));
        _alerts = new AlertStore(logger, dataGateways, messaging);
    }

    // Why resolved here rather than injected: the gateway is scoped and the service is not, so holding one would be a captive dependency.
    private IDataGateway Gateway => _dataGateways.ByName("Main");

    /// <summary>Reads one comment's row.</summary>
    /// <param name="id">The comment's identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The row, or success with no value when there is none.</returns>
    public async Task<IGenericResult<UiFeedbackRow?>> FindRow(Guid id, CancellationToken cancellationToken)
    {
        var location = await Location(cancellationToken).ConfigureAwait(false);
        if (!location.IsSuccess)
            return location.ToNewResult<UiFeedbackRow?>();

        var command = Query.From<UiFeedbackRow>(location.Value.DataStoreName, location.Value.PathName, FeedbackContainer)
            .Where(row => row.Id).Equal(id)
            .Build();

        var rows = await Gateway.Execute<IEnumerable<UiFeedbackRow>>(command, cancellationToken).ConfigureAwait(false);
        return rows.IsSuccess
            ? GenericResult<UiFeedbackRow?>.Success(rows.Value?.FirstOrDefault())
            : Failed<UiFeedbackRow?>(rows, "read");
    }

    /// <summary>Reads the comments that match a filter, newest first.</summary>
    /// <param name="filter">The conditions to narrow by.</param>
    /// <param name="skip">The number of rows to skip.</param>
    /// <param name="take">The number of rows to take.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The rows.</returns>
    public async Task<IGenericResult<IReadOnlyList<UiFeedbackRow>>> FindRows(UiFeedbackFilter filter, int skip, int take, CancellationToken cancellationToken)
    {
        var location = await Location(cancellationToken).ConfigureAwait(false);
        if (!location.IsSuccess)
            return location.ToNewResult<IReadOnlyList<UiFeedbackRow>>();

        var command = Narrow(Query.From<UiFeedbackRow>(location.Value.DataStoreName, location.Value.PathName, FeedbackContainer), filter)
            .OrderByDescending(row => row.CreatedAt)
            .OrderBy(row => row.Id)
            .Paging(skip, take)
            .Build();

        var rows = await Gateway.Execute<IEnumerable<UiFeedbackRow>>(command, cancellationToken).ConfigureAwait(false);
        return rows.IsSuccess && rows.Value is { } found
            ? GenericResult<IReadOnlyList<UiFeedbackRow>>.Success(found.ToList())
            : Failed<IReadOnlyList<UiFeedbackRow>>(rows, "list");
    }

    /// <summary>Reads the tags of some comments.</summary>
    /// <param name="ids">The comments' identifiers; not more than the store takes in one condition.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The tags of each comment; a comment with none has no entry.</returns>
    public async Task<IGenericResult<ILookup<Guid, string>>> FindTags(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
            return GenericResult<ILookup<Guid, string>>.Success(Enumerable.Empty<UiFeedbackTagRow>().ToLookup(row => row.FeedbackId, row => row.Tag));

        var location = await Location(cancellationToken).ConfigureAwait(false);
        if (!location.IsSuccess)
            return location.ToNewResult<ILookup<Guid, string>>();

        var command = Query.From<UiFeedbackTagRow>(location.Value.DataStoreName, location.Value.PathName, TagContainer)
            .Where(row => row.FeedbackId).In(ids)
            .Build();

        var rows = await Gateway.Execute<IEnumerable<UiFeedbackTagRow>>(command, cancellationToken).ConfigureAwait(false);
        return rows.IsSuccess && rows.Value is { } found
            ? GenericResult<ILookup<Guid, string>>.Success(found.ToLookup(row => row.FeedbackId, row => row.Tag))
            : Failed<ILookup<Guid, string>>(rows, "read the tags of");
    }

    /// <summary>Reads the components of some comments, each comment's in the order they were given.</summary>
    /// <param name="ids">The comments' identifiers; not more than the store takes in one condition.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The components' JSON texts of each comment, ordered by their position; a comment with none has no entry.</returns>
    public async Task<IGenericResult<ILookup<Guid, string>>> FindAnchors(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
            return GenericResult<ILookup<Guid, string>>.Success(Enumerable.Empty<UiFeedbackAnchorRow>().ToLookup(row => row.FeedbackId, row => row.Anchor));

        var location = await Location(cancellationToken).ConfigureAwait(false);
        if (!location.IsSuccess)
            return location.ToNewResult<ILookup<Guid, string>>();

        var command = Query.From<UiFeedbackAnchorRow>(location.Value.DataStoreName, location.Value.PathName, AnchorContainer)
            .Where(row => row.FeedbackId).In(ids)
            .Build();

        var rows = await Gateway.Execute<IEnumerable<UiFeedbackAnchorRow>>(command, cancellationToken).ConfigureAwait(false);

        // Why ordered here and not trusted to the query: the order is the reviewer's, and a lookup keeps the order its rows arrive in.
        return rows.IsSuccess && rows.Value is { } found
            ? GenericResult<ILookup<Guid, string>>.Success(found.OrderBy(row => row.Ordinal).ToLookup(row => row.FeedbackId, row => row.Anchor))
            : Failed<ILookup<Guid, string>>(rows, "read the components of");
    }

    /// <summary>Reads the agreements with some comments.</summary>
    /// <param name="ids">The comments' identifiers; not more than the store takes in one condition.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The agreements, so that each comment can say how many people agree and whether a given person does.</returns>
    public async Task<IGenericResult<UiFeedbackAgreements>> FindAgreements(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
            return GenericResult<UiFeedbackAgreements>.Success(new UiFeedbackAgreements(Enumerable.Empty<UiFeedbackAgreementRow>().ToLookup(row => row.FeedbackId)));

        var location = await Location(cancellationToken).ConfigureAwait(false);
        if (!location.IsSuccess)
            return location.ToNewResult<UiFeedbackAgreements>();

        var command = Query.From<UiFeedbackAgreementRow>(location.Value.DataStoreName, location.Value.PathName, AgreementContainer)
            .Where(row => row.FeedbackId).In(ids)
            .Build();

        var rows = await Gateway.Execute<IEnumerable<UiFeedbackAgreementRow>>(command, cancellationToken).ConfigureAwait(false);
        return rows.IsSuccess && rows.Value is { } found
            ? GenericResult<UiFeedbackAgreements>.Success(new UiFeedbackAgreements(found.ToLookup(row => row.FeedbackId)))
            : Failed<UiFeedbackAgreements>(rows, "read the agreements with");
    }

    /// <summary>Counts the comments on some notes.</summary>
    /// <param name="ids">The notes' identifiers; not more than the store takes in one condition.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The comments' marks, one for each comment, so each note can say how many it has.</returns>
    public async Task<IGenericResult<UiFeedbackCommentCounts>> FindCommentCounts(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
            return GenericResult<UiFeedbackCommentCounts>.Success(UiFeedbackCommentCounts.None);

        var location = await Location(cancellationToken).ConfigureAwait(false);
        if (!location.IsSuccess)
            return location.ToNewResult<UiFeedbackCommentCounts>();

        var command = Query.From<UiFeedbackCommentMarkRow>(location.Value.DataStoreName, location.Value.PathName, CommentContainer)
            .Where(row => row.FeedbackId).In(ids)
            .Build();

        var rows = await Gateway.Execute<IEnumerable<UiFeedbackCommentMarkRow>>(command, cancellationToken).ConfigureAwait(false);
        return rows.IsSuccess && rows.Value is { } found
            ? GenericResult<UiFeedbackCommentCounts>.Success(new UiFeedbackCommentCounts(found.ToLookup(row => row.FeedbackId)))
            : Failed<UiFeedbackCommentCounts>(rows, "count the comments on");
    }

    /// <summary>Reads the comments on a note, oldest first.</summary>
    /// <param name="feedbackId">The note.</param>
    /// <param name="skip">The number of comments to skip.</param>
    /// <param name="take">The number of comments to take.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The comments.</returns>
    public async Task<IGenericResult<IReadOnlyList<UiFeedbackCommentRow>>> FindComments(Guid feedbackId, int skip, int take, CancellationToken cancellationToken)
    {
        var location = await Location(cancellationToken).ConfigureAwait(false);
        if (!location.IsSuccess)
            return location.ToNewResult<IReadOnlyList<UiFeedbackCommentRow>>();

        var command = Query.From<UiFeedbackCommentRow>(location.Value.DataStoreName, location.Value.PathName, CommentContainer)
            .Where(row => row.FeedbackId).Equal(feedbackId)
            .OrderBy(row => row.CreatedAt)
            .OrderBy(row => row.Id)
            .Paging(skip, take)
            .Build();

        var rows = await Gateway.Execute<IEnumerable<UiFeedbackCommentRow>>(command, cancellationToken).ConfigureAwait(false);
        return rows.IsSuccess && rows.Value is { } found
            ? GenericResult<IReadOnlyList<UiFeedbackCommentRow>>.Success(found.ToList())
            : Failed<IReadOnlyList<UiFeedbackCommentRow>>(rows, "list the comments on");
    }

    /// <summary>Keeps a comment on a note.</summary>
    /// <param name="row">The comment.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Success when the row was written.</returns>
    public async Task<IGenericResult> AddComment(UiFeedbackCommentRow row, CancellationToken cancellationToken)
    {
        var location = await Location(cancellationToken).ConfigureAwait(false);
        if (!location.IsSuccess)
            return (IGenericResult)location;

        return await Execute(
            CmdBuilders.Insert.Into<UiFeedbackCommentRow>(CommentContainer).DataStore(location.Value.DataStoreName).Path(location.Value.PathName).Value(row),
            "comment on", row.FeedbackId, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Keeps a person's agreement with a comment.</summary>
    /// <param name="row">The agreement.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Success when the row was written.</returns>
    public async Task<IGenericResult> AddAgreement(UiFeedbackAgreementRow row, CancellationToken cancellationToken)
    {
        var location = await Location(cancellationToken).ConfigureAwait(false);
        if (!location.IsSuccess)
            return (IGenericResult)location;

        return await Execute(
            CmdBuilders.Insert.Into<UiFeedbackAgreementRow>(AgreementContainer).DataStore(location.Value.DataStoreName).Path(location.Value.PathName).Value(row),
            "agree with", row.FeedbackId, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Removes a person's agreement with a comment.</summary>
    /// <param name="feedbackId">The comment.</param>
    /// <param name="userId">The person.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Success when no agreement of theirs remains, whether or not there was one.</returns>
    public async Task<IGenericResult> RemoveAgreement(Guid feedbackId, Guid userId, CancellationToken cancellationToken)
    {
        var location = await Location(cancellationToken).ConfigureAwait(false);
        if (!location.IsSuccess)
            return (IGenericResult)location;

        return await Execute(
            CmdBuilders.Delete.From(AgreementContainer).DataStore(location.Value.DataStoreName).Path(location.Value.PathName)
                .Where(nameof(UiFeedbackAgreementRow.FeedbackId), feedbackId)
                .Where(nameof(UiFeedbackAgreementRow.UserId), userId)
                .Build(),
            "take back the agreement with", feedbackId, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Reads the picture kept with a comment.</summary>
    /// <param name="id">The comment's identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The picture row, or success with no value when none is kept.</returns>
    public async Task<IGenericResult<UiFeedbackScreenshotRow?>> FindPicture(Guid id, CancellationToken cancellationToken)
    {
        var location = await Location(cancellationToken).ConfigureAwait(false);
        if (!location.IsSuccess)
            return location.ToNewResult<UiFeedbackScreenshotRow?>();

        var command = Query.From<UiFeedbackScreenshotRow>(location.Value.DataStoreName, location.Value.PathName, ScreenshotContainer)
            .Where(row => row.FeedbackId).Equal(id)
            .Build();

        var rows = await Gateway.Execute<IEnumerable<UiFeedbackScreenshotRow>>(command, cancellationToken).ConfigureAwait(false);
        return rows.IsSuccess
            ? GenericResult<UiFeedbackScreenshotRow?>.Success(rows.Value?.FirstOrDefault())
            : Failed<UiFeedbackScreenshotRow?>(rows, "read the picture of");
    }

    /// <summary>
    /// Keeps a comment with its tags and picture. When a later row cannot be written the earlier ones are removed again, so a comment is never kept half made.
    /// </summary>
    /// <param name="row">The comment.</param>
    /// <param name="tags">Its tags.</param>
    /// <param name="anchors">The components it is about, in order.</param>
    /// <param name="picture">Its picture, or <see langword="null"/> when it has none.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Success when every row was written.</returns>
    public async Task<IGenericResult> Add(
        UiFeedbackRow row, IReadOnlyList<UiFeedbackTagRow> tags, IReadOnlyList<UiFeedbackAnchorRow> anchors, UiFeedbackScreenshotRow? picture, CancellationToken cancellationToken)
    {
        var location = await Location(cancellationToken).ConfigureAwait(false);
        if (!location.IsSuccess)
            return (IGenericResult)location;

        var saved = await Execute(CmdBuilders.Insert.Into<UiFeedbackRow>(FeedbackContainer).DataStore(location.Value.DataStoreName).Path(location.Value.PathName).Value(row), "save", row.Id, cancellationToken)
            .ConfigureAwait(false);
        if (!saved.IsSuccess)
            return saved;

        foreach (var tag in tags)
        {
            var tagged = await Execute(CmdBuilders.Insert.Into<UiFeedbackTagRow>(TagContainer).DataStore(location.Value.DataStoreName).Path(location.Value.PathName).Value(tag), "save a tag of", row.Id, cancellationToken)
                .ConfigureAwait(false);
            if (!tagged.IsSuccess)
                return await Undo(row.Id, tagged, cancellationToken).ConfigureAwait(false);
        }

        foreach (var anchor in anchors)
        {
            var anchored = await Execute(CmdBuilders.Insert.Into<UiFeedbackAnchorRow>(AnchorContainer).DataStore(location.Value.DataStoreName).Path(location.Value.PathName).Value(anchor), "save a component of", row.Id, cancellationToken)
                .ConfigureAwait(false);
            if (!anchored.IsSuccess)
                return await Undo(row.Id, anchored, cancellationToken).ConfigureAwait(false);
        }

        if (picture is null)
            return GenericResult.Success();

        var pictured = await Execute(CmdBuilders.Insert.Into<UiFeedbackScreenshotRow>(ScreenshotContainer).DataStore(location.Value.DataStoreName).Path(location.Value.PathName).Value(picture), "save the picture of", row.Id, cancellationToken)
            .ConfigureAwait(false);
        return pictured.IsSuccess ? pictured : await Undo(row.Id, pictured, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Writes a new note.</summary>
    /// <param name="id">The comment's identifier.</param>
    /// <param name="update">The new note and when it changed.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Success with the number of rows changed.</returns>
    public async Task<IGenericResult<int>> UpdateBody(Guid id, UiFeedbackBodyUpdate update, CancellationToken cancellationToken)
    {
        var location = await Location(cancellationToken).ConfigureAwait(false);
        if (!location.IsSuccess)
            return location.ToNewResult<int>();

        var changed = await Gateway.Execute<int>(
            CmdBuilders.Update.In<UiFeedbackBodyUpdate>(FeedbackContainer).DataStore(location.Value.DataStoreName).Path(location.Value.PathName)
                .Where(nameof(UiFeedbackRow.Id), id)
                .Value(update),
            cancellationToken).ConfigureAwait(false);
        return changed.IsSuccess ? changed : Failed<int>(changed, "change the note of");
    }

    /// <summary>Moves a comment from one state to the other.</summary>
    /// <param name="id">The comment's identifier.</param>
    /// <param name="stateNow">The state the comment must still be in; a comment already moved by someone else is not changed.</param>
    /// <param name="update">The new state and who resolved the comment.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Success with the number of rows changed: 0 when the comment is no longer in the state it was read in.</returns>
    public async Task<IGenericResult<int>> UpdateState(Guid id, string stateNow, UiFeedbackStateUpdate update, CancellationToken cancellationToken)
    {
        var location = await Location(cancellationToken).ConfigureAwait(false);
        if (!location.IsSuccess)
            return location.ToNewResult<int>();

        var changed = await Gateway.Execute<int>(
            CmdBuilders.Update.In<UiFeedbackStateUpdate>(FeedbackContainer).DataStore(location.Value.DataStoreName).Path(location.Value.PathName)
                .Where(nameof(UiFeedbackRow.Id), id)
                .Where(nameof(UiFeedbackRow.State), stateNow)
                .Value(update),
            cancellationToken).ConfigureAwait(false);
        return changed.IsSuccess ? changed : Failed<int>(changed, "change the state of");
    }

    /// <summary>Deletes a comment with its tags and picture.</summary>
    /// <param name="id">The comment's identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Success when every row is gone.</returns>
    public async Task<IGenericResult> Remove(Guid id, CancellationToken cancellationToken)
    {
        var location = await Location(cancellationToken).ConfigureAwait(false);
        if (!location.IsSuccess)
            return (IGenericResult)location;

        // Why the children first: what was done to the comment as an alert (msg.AlertAction, msg.AlertEveryoneAction), msg.UiFeedbackAnchor, msg.UiFeedbackComment, msg.UiFeedbackAgreement, msg.UiFeedbackTag and msg.UiFeedbackScreenshot reference the comment and carry no cascade.
        var anchors = await Execute(DeleteCall(AnchorContainer, nameof(UiFeedbackAnchorRow.FeedbackId), id, location.Value), "delete the components of", id, cancellationToken).ConfigureAwait(false);
        if (!anchors.IsSuccess)
            return anchors;

        var comments = await Execute(DeleteCall(CommentContainer, nameof(UiFeedbackCommentRow.FeedbackId), id, location.Value), "delete the comments on", id, cancellationToken).ConfigureAwait(false);
        if (!comments.IsSuccess)
            return comments;

        var agreements = await Execute(DeleteCall(AgreementContainer, nameof(UiFeedbackAgreementRow.FeedbackId), id, location.Value), "delete the agreements with", id, cancellationToken).ConfigureAwait(false);
        if (!agreements.IsSuccess)
            return agreements;

        var alerts = await _alerts.RemoveAllFor(UiFeedbackAlert.Kind, id, cancellationToken).ConfigureAwait(false);
        if (!alerts.IsSuccess)
            return alerts;

        var tags = await Execute(DeleteCall(TagContainer, nameof(UiFeedbackTagRow.FeedbackId), id, location.Value), "delete the tags of", id, cancellationToken).ConfigureAwait(false);
        if (!tags.IsSuccess)
            return tags;

        var picture = await Execute(DeleteCall(ScreenshotContainer, nameof(UiFeedbackScreenshotRow.FeedbackId), id, location.Value), "delete the picture of", id, cancellationToken).ConfigureAwait(false);
        return picture.IsSuccess
            ? await Execute(DeleteCall(FeedbackContainer, nameof(UiFeedbackRow.Id), id, location.Value), "delete", id, cancellationToken).ConfigureAwait(false)
            : picture;
    }

    private static DataGatewayCall DeleteCall(string container, string column, Guid id, (string DataStoreName, string PathName) location)
        => CmdBuilders.Delete.From(container).DataStore(location.DataStoreName).Path(location.PathName).Where(column, id).Build();

    private static QueryCommandBuilder<UiFeedbackRow> Narrow(QueryCommandBuilder<UiFeedbackRow> builder, UiFeedbackFilter filter)
    {
        if (filter.TenantId is { } tenantId)
            builder = builder.Where(row => row.TenantId).Equal(tenantId);

        if (filter.State is not null)
            builder = builder.Where(row => row.State).Equal(filter.State);

        if (filter.Route is not null)
            builder = builder.Where(row => row.Route).Equal(filter.Route);

        if (filter.Urgency is not null)
            builder = builder.Where(row => row.Urgency).Equal(filter.Urgency);

        if (filter.Urgencies is { } urgencies)
            builder = builder.Where(row => row.Urgency).In(urgencies);

        return filter.AuthorUserId is { } authorUserId ? builder.Where(row => row.AuthorUserId).Equal(authorUserId) : builder;
    }

    private async Task<IGenericResult> Undo(Guid id, IGenericResult cause, CancellationToken cancellationToken)
    {
        var removed = await Remove(id, cancellationToken).ConfigureAwait(false);
        if (!removed.IsSuccess)
            MessagingLog.UiFeedbackStoreFailed(_logger, "undo a half-saved comment", removed.CurrentMessage);

        return cause;
    }

    private async Task<IGenericResult> Execute(DataGatewayCall command, string action, Guid id, CancellationToken cancellationToken)
    {
        var result = await Gateway.Execute<int>(command, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? GenericResult.Success() : FailedWrite(result, action, id);
    }

    private IGenericResult FailedWrite(IGenericResult result, string action, Guid id)
        => result.Messages.Any()
            ? result
            : GenericResult.Failure(MessagingLog.UiFeedbackStoreFailed(_logger, $"{action} comment {id:D}", "the command failed"));

    private IGenericResult<T> Failed<T>(IGenericResult result, string action)
        => result.Messages.Any()
            ? result.ToNewResult<T>()
            : GenericResult<T>.Failure(MessagingLog.UiFeedbackStoreFailed(_logger, $"{action} comments", "the command failed"));

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
