using System;

namespace Fdw.Operations.Endpoints;

/// <summary>
/// Internal entity for querying chain step source fields for lineage tracking.
/// </summary>
public class ChainStepSourceFieldRecord
{
    /// <summary>Gets or sets the source field record identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the parent chain step identifier.</summary>
    public Guid ChainStepId { get; set; }
    /// <summary>Gets or sets the field name.</summary>
    public string FieldName { get; set; } = string.Empty;
}
