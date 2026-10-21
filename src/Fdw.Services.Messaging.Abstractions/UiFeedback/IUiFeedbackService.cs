using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Messaging;

/// <summary>
/// Keeps UI comments, the post-it notes a tester pins to an area of a screen, and reads them back (FDW-2143).
/// </summary>
/// <remarks>
/// The caller is taken from the authentication context: a comment belongs to its author's company, and a reader reaches the comments of the company they act in,
/// or every company's when they act in the global tenant, the rule executions and the audit trail follow. A comment out of a caller's reach does not exist for
/// them: it is not found. Every refusal is loud and names the field; nothing is cut or defaulted.
/// </remarks>
public interface IUiFeedbackService
{
    /// <summary>Leaves a comment.</summary>
    /// <param name="request">What the tester sent.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The comment as saved, or the failure naming the field that was refused.</returns>
    Task<IGenericResult<UiFeedbackPayload>> Create(CreateUiFeedbackRequest request, CancellationToken cancellationToken = default);

    /// <summary>Lists the comments the caller reaches.</summary>
    /// <param name="query">What to narrow and order by, and which page.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The page, without pictures.</returns>
    Task<IGenericResult<IReadOnlyList<UiFeedbackPayload>>> List(UiFeedbackQuery query, CancellationToken cancellationToken = default);

    /// <summary>Reads one comment.</summary>
    /// <param name="id">The comment's identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The comment, or not found when there is none in the caller's reach.</returns>
    Task<IGenericResult<UiFeedbackPayload>> Get(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Reads the picture of the screen a comment was left on.</summary>
    /// <param name="id">The comment's identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The picture, or not found when the comment is out of reach or was saved without one.</returns>
    Task<IGenericResult<UiFeedbackPicture>> GetPicture(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Changes the note of the caller's own comment.</summary>
    /// <param name="id">The comment's identifier.</param>
    /// <param name="body">The new note.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The comment as changed; not found out of reach, refused when it is another person's.</returns>
    Task<IGenericResult<UiFeedbackPayload>> Edit(Guid id, string? body, CancellationToken cancellationToken = default);

    /// <summary>Deletes the caller's own comment, with its tags and picture.</summary>
    /// <param name="id">The comment's identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Success; not found out of reach, refused when it is another person's.</returns>
    Task<IGenericResult> Delete(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Acknowledges a comment as the caller, and marks the caller's alert about it read. A person acknowledges a comment once: acknowledging again changes nothing
    /// and still succeeds.
    /// </summary>
    /// <param name="id">The comment's identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The comment with <see cref="UiFeedbackPayload.AcknowledgedByMe"/> set; not found when it is out of the caller's reach.</returns>
    Task<IGenericResult<UiFeedbackPayload>> Acknowledge(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Agrees with a comment as the caller. A person agrees once: agreeing again changes nothing and still succeeds. The person who left the comment cannot agree
    /// with it.
    /// </summary>
    /// <param name="id">The comment's identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The comment with <see cref="UiFeedbackPayload.AgreeCount"/> and <see cref="UiFeedbackPayload.AgreedByMe"/> as they now stand; not found when it is out of
    /// the caller's sight; a conflict when the caller left it.</returns>
    Task<IGenericResult<UiFeedbackPayload>> Agree(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Takes back the caller's agreement with a comment. Taking back an agreement that was not given changes nothing and still succeeds.</summary>
    /// <param name="id">The comment's identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The comment as it now stands; not found when it is out of the caller's sight.</returns>
    Task<IGenericResult<UiFeedbackPayload>> TakeBackAgreement(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Adds a comment to a note, as the caller.</summary>
    /// <param name="id">The note's identifier.</param>
    /// <param name="body">The text of the comment.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The comment as kept; refused naming the field when the text is missing or longer than <see cref="UiFeedbackLimits.MaxBodyLength"/>; not found when the
    /// note is out of the caller's sight.</returns>
    Task<IGenericResult<UiFeedbackCommentPayload>> AddComment(Guid id, string? body, CancellationToken cancellationToken = default);

    /// <summary>Lists the comments on a note, oldest first.</summary>
    /// <param name="id">The note's identifier.</param>
    /// <param name="skip">The number of comments to skip.</param>
    /// <param name="take">The number of comments to take, from 1 to <see cref="UiFeedbackLimits.MaxPageSize"/>.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The comments; not found when the note is out of the caller's sight.</returns>
    Task<IGenericResult<IReadOnlyList<UiFeedbackCommentPayload>>> ListComments(Guid id, int skip, int take, CancellationToken cancellationToken = default);

    /// <summary>Resolves an open comment.</summary>
    /// <param name="id">The comment's identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The comment as resolved; refused with a conflict when it is already resolved.</returns>
    Task<IGenericResult<UiFeedbackPayload>> Resolve(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Reopens a resolved comment.</summary>
    /// <param name="id">The comment's identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The comment as reopened; refused with a conflict when it is already open.</returns>
    Task<IGenericResult<UiFeedbackPayload>> Reopen(Guid id, CancellationToken cancellationToken = default);
}
