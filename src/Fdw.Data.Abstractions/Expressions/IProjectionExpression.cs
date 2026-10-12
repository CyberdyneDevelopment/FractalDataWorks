using System.Collections.Generic;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Interface for projection expressions (SELECT clause representation).
/// </summary>
/// <remarks>
/// Represents which fields to select/project.
/// Translators convert this to SQL SELECT, OData $select, property filtering, etc.
/// If null or empty, all fields are selected.
/// </remarks>
public interface IProjectionExpression
{
    /// <summary>
    /// Gets the fields to project.
    /// </summary>
    /// <value>A collection of fields to include in the result. Empty means select all.</value>
    IReadOnlyList<ProjectionField> Fields { get; }

    /// <summary>
    /// Gets the property names to project (convenience property).
    /// </summary>
    /// <value>A collection of property names extracted from Fields.</value>
    IReadOnlyList<string>? PropertyNames { get; }
}
