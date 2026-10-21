using System.Collections.Generic;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>A transformation defined once and attachable to field mappings (FDW-846).</summary>
public sealed class TransformationPayload
{
    /// <summary>
    /// Gets or sets the transformation's id (FDW-2007): what a mapping's chain stores for it, so a chain read back by id can be shown by name and a deleted
    /// transformation matches no item. Stated when a transformation is read or listed; a create or an update addresses it by <see cref="Name"/> and ignores it.
    /// </summary>
    public System.Guid Id { get; set; }

    /// <summary>Gets or sets the transformation's name -- how it is attached and addressed.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the description.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the implementation: Field, which transforms one column and attaches to field
    /// mappings, or a set kind (Map, Filter, Calculate, Aggregate, Lookup, Built) that runs in pipeline stages.
    /// </summary>
    public string Implementation { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a Field transformation's operation: a transform type name from <c>/transform-types</c>.
    /// Null for the set kinds, which are not per-field operations.
    /// </summary>
    public string? TransformationType { get; set; }

    /// <summary>Gets or sets whether this is a seeded standard transformation, which tenants cannot change.</summary>
    public bool IsSystemProvided { get; set; }

    /// <summary>Gets or sets a Field transformation's parameter values; empty for the set kinds.</summary>
    public IReadOnlyList<TransformationParameterPayload> Parameters { get; set; } = [];

    /// <summary>Gets or sets the inputs a Built transformation declares, in order; empty for the other kinds.</summary>
    public IReadOnlyList<TransformationInputPayload> Inputs { get; set; } = [];

    /// <summary>Gets or sets the results a Built transformation produces, in order; empty for the other kinds.</summary>
    public IReadOnlyList<TransformationOutputPayload> Outputs { get; set; } = [];

    /// <summary>
    /// Gets or sets a Built transformation's results with their formulas. Filled when one transformation is read, created or
    /// changed; empty in a list (read the transformation for its formulas) and for the other kinds.
    /// </summary>
    public IReadOnlyList<BuiltResultPayload> Results { get; set; } = [];

    /// <summary>Gets or sets the tables a Built transformation reads. Filled when one transformation is read, created or changed; empty otherwise.</summary>
    public IReadOnlyList<TableInputPayload> TableInputs { get; set; } = [];

    /// <summary>Gets or sets the totals and lookups a Built transformation's formulas read. Filled when one transformation is read, created or changed; empty otherwise.</summary>
    public IReadOnlyList<DerivedValuePayload> Derived { get; set; } = [];

    /// <summary>Gets or sets the tables a Built transformation writes, with their rows' sources and columns. Filled when one transformation is read, created or changed; empty otherwise.</summary>
    public IReadOnlyList<TableOutputPayload> TableOutputs { get; set; } = [];
}
