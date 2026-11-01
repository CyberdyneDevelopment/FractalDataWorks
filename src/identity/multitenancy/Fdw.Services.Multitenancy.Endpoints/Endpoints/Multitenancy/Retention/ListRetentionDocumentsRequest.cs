namespace Fdw.Services.Multitenancy.Endpoints;

/// <summary>
/// Request naming an organization and the document type whose documents are listed (FDW-2906).
/// </summary>
public class ListRetentionDocumentsRequest : OrganizationRetentionRequest
{
    /// <summary>Gets or sets the document type, as named in a retention policy. A query value: a type is a folder path and may hold a slash.</summary>
    public string DocumentType { get; set; } = string.Empty;
}
