using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Abstractions;

namespace Fdw.Data;

/// <summary>
/// Implementation of IProjectionExpression for SELECT clause representation.
/// </summary>
public sealed class ProjectionExpression : IProjectionExpression
{
    /// <summary>
    /// Gets or sets the fields to project.
    /// Empty list means select all fields.
    /// </summary>
    public required IReadOnlyList<ProjectionField> Fields { get; init; }

    /// <summary>
    /// Gets the property names extracted from Fields.
    /// </summary>
    public IReadOnlyList<string>? PropertyNames => Fields?.Select(f => f.PropertyName).ToList();
}
