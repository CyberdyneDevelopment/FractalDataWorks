using System;
using Fdw.Data;

namespace Fdw.Operations.Endpoints;

/// <summary>
/// Internal entity for querying chain steps for lineage tracking.
/// </summary>
[GenerateMapper]
public class ChainStepLineageRecord
{
    /// <summary>Gets or sets the chain step identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the parent chain definition identifier.</summary>
    public Guid ChainDefinitionId { get; set; }
    /// <summary>Gets or sets the step name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the operation type.</summary>
    public string? OperationType { get; set; }
    /// <summary>Gets or sets the target field name.</summary>
    public string? TargetField { get; set; }
}
