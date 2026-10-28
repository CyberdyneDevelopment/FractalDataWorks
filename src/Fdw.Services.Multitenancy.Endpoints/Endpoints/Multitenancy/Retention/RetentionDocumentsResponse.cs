using System.Collections.Generic;

namespace Fdw.Services.Multitenancy.Endpoints;

/// <summary>
/// The documents of one type that belong to an organization (FDW-2906).
/// </summary>
public class RetentionDocumentsResponse
{
    /// <summary>Gets or sets the documents.</summary>
    public IReadOnlyList<RetentionDocumentResponse> Documents { get; set; } = [];
}
