namespace Fdw.Services.Multitenancy.Endpoints;

/// <summary>
/// Request to lift the legal hold on a document of an organization (FDW-1575).
/// </summary>
public class LiftLegalHoldRequest : OrganizationRetentionRequest
{
    /// <summary>Gets or sets the application's identifier of the document.</summary>
    public string DocumentId { get; set; } = string.Empty;
}
