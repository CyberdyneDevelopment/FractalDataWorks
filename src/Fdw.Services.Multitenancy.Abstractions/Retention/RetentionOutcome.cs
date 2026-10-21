namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// What the retention job did, or tried to do, to one document (FDW-1575).
/// </summary>
/// <param name="DocumentId">The application's identifier of the document.</param>
/// <param name="DocumentType">The document's type.</param>
/// <param name="EndAction">The end action the policy names.</param>
/// <param name="Reason">Why the action failed, or null when the document was ended.</param>
public sealed record RetentionOutcome(string DocumentId, string DocumentType, string EndAction, string? Reason);
