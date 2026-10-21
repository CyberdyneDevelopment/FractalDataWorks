namespace Fdw.Services.Multitenancy.Endpoints;

/// <summary>
/// Request to remove the retention policy of a document type in an organization (FDW-1575).
/// </summary>
public class RemoveRetentionPolicyRequest : OrganizationRetentionRequest
{
    /// <summary>Gets or sets the document type whose policy is removed.</summary>
    public string DocumentType { get; set; } = string.Empty;
}
