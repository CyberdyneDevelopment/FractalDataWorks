using System.Collections.Generic;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>
/// Defines a new transformation. The <see cref="Implementation"/> says which kind it is and so which members apply:
/// Field takes <see cref="TransformationType"/> and <see cref="Parameters"/>; Built takes <see cref="Inputs"/> and
/// <see cref="Results"/>. A member that belongs to another kind is refused.
/// </summary>
public sealed class CreateTransformationRequest
{
    /// <summary>Gets or sets the transformation's name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the kind of transformation: Field (one operation on one field) or Built (inputs and formula results). Required.</summary>
    public string Implementation { get; set; } = string.Empty;

    /// <summary>Gets or sets a Field transformation's operation: a transform type name from <c>/transform-types</c>. Field only.</summary>
    public string? TransformationType { get; set; }

    /// <summary>Gets or sets the values of the parameters a Field transformation's operation declares. Field only.</summary>
    public IList<TransformationParameterPayload> Parameters { get; set; } = [];

    /// <summary>Gets or sets the fields a Built transformation's formulas read; every field a formula reads must be declared here. Built only.</summary>
    public IList<TransformationInputPayload>? Inputs { get; set; }

    /// <summary>Gets or sets a Built transformation's results, in order, each with its formula; a formula may refer only to results before it. Built only.</summary>
    public IList<BuiltResultPayload>? Results { get; set; }

    /// <summary>Gets or sets the tables a Built transformation reads, by shape. Built only; with <see cref="TableOutputs"/> it reads and writes tables.</summary>
    public IList<TableInputPayload>? TableInputs { get; set; }

    /// <summary>Gets or sets the totals and lookups the formulas of a Built transformation can read by name. Built only.</summary>
    public IList<DerivedValuePayload>? Derived { get; set; }

    /// <summary>Gets or sets the tables a Built transformation writes. Built only; a transformation has table outputs or <c>results</c>, not both.</summary>
    public IList<TableOutputPayload>? TableOutputs { get; set; }
}
