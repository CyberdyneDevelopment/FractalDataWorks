namespace Fdw.Services.Multitenancy.Endpoints;

/// <summary>
/// Request to put a document of an organization under a legal hold (FDW-1575).
/// </summary>
public class PlaceLegalHoldRequest : OrganizationRetentionRequest
{
    /// <summary>Gets or sets the application's identifier of the document.</summary>
    public string DocumentId { get; set; } = string.Empty;

    /// <summary>Gets or sets why the document is held.</summary>
    public string Reason { get; set; } = string.Empty;
}
