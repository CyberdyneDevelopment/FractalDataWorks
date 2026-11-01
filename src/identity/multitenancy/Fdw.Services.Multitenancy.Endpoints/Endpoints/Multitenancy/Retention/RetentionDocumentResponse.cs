using System;
using System.Collections.Generic;

namespace Fdw.Services.Multitenancy.Endpoints;

/// <summary>
/// One document of an organization, as the application lists it for retention (FDW-2906).
/// </summary>
public class RetentionDocumentResponse
{
    /// <summary>Gets or sets the identifier the application gives the document; a legal hold names it.</summary>
    public string DocumentId { get; set; } = string.Empty;

    /// <summary>Gets or sets the document type.</summary>
    public string DocumentType { get; set; } = string.Empty;

    /// <summary>Gets or sets the dates the application knows, by the name a retention policy counts from.</summary>
    public IReadOnlyDictionary<string, DateTimeOffset> Dates { get; set; } = new Dictionary<string, DateTimeOffset>(StringComparer.OrdinalIgnoreCase);
}
