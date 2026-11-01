namespace Fdw.Services.Multitenancy.Endpoints;

/// <summary>
/// What the retention job did, or tried to do, to one document (FDW-1575).
/// </summary>
public class RetentionOutcomeResponse
{
    /// <summary>Gets or sets the application's identifier of the document.</summary>
    public string DocumentId { get; set; } = string.Empty;

    /// <summary>Gets or sets the document's type.</summary>
    public string DocumentType { get; set; } = string.Empty;

    /// <summary>Gets or sets the end action the policy names.</summary>
    public string EndAction { get; set; } = string.Empty;

    /// <summary>Gets or sets why the action failed, or null when the document was ended.</summary>
    public string? Reason { get; set; }
}
