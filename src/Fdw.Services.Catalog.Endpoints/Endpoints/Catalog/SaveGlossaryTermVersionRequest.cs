namespace Fdw.Services.Catalog.Endpoints;

/// <summary>
/// Request to save a version of a glossary term at a place (FDW-2331). A version carries what can differ by place; the category, the acronym facts,
/// the related terms and the linked data sets belong to the term itself.
/// </summary>
public class SaveGlossaryTermVersionRequest : GlossaryTermVersionPlaceRequest
{
    /// <summary>Gets or sets the business definition at that place.</summary>
    public string Definition { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional calculation formula at that place.</summary>
    public string? Formula { get; set; }

    /// <summary>Gets or sets the business owner at that place.</summary>
    public string Owner { get; set; } = string.Empty;

    /// <summary>Gets or sets the data steward at that place.</summary>
    public string? Steward { get; set; }
}
