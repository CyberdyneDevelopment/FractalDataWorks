using System.Collections.Generic;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>Changes a transformation. Every member is optional: an omitted one keeps its current value.</summary>
public sealed class UpdateTransformationRequest
{
    /// <summary>Gets or sets the transformation's name, from the route.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the new description, or null to keep the current one.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the new operation, or null to keep the current one.</summary>
    public string? TransformationType { get; set; }

    /// <summary>
    /// Gets or sets the full new set of parameter values, or null to keep the current set. An empty
    /// list clears them.
    /// </summary>
    public IList<TransformationParameterPayload>? Parameters { get; set; }

    /// <summary>Gets or sets a Built transformation's full new set of inputs, or null to keep the current set. Built only.</summary>
    public IList<TransformationInputPayload>? Inputs { get; set; }

    /// <summary>Gets or sets a Built transformation's full new set of results with their formulas, or null to keep the current set. Built only.</summary>
    public IList<BuiltResultPayload>? Results { get; set; }

    /// <summary>Gets or sets the full new set of tables a Built transformation reads, or null to keep the current set. Built only; with <see cref="TableOutputs"/> it reads and writes tables.</summary>
    public IList<TableInputPayload>? TableInputs { get; set; }

    /// <summary>Gets or sets the totals and lookups the formulas of a Built transformation can read by name. Built only.</summary>
    public IList<DerivedValuePayload>? Derived { get; set; }

    /// <summary>Gets or sets the tables a Built transformation writes. Built only; a transformation has table outputs or <c>results</c>, not both.</summary>
    public IList<TableOutputPayload>? TableOutputs { get; set; }
}
