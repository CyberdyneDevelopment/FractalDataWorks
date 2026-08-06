using System.Collections.Generic;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Represents a constraint on a data schema.
/// </summary>
public interface ISchemaConstraint
{
    /// <summary>
    /// Gets the constraint name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the constraint type.
    /// </summary>
    string ConstraintType { get; }

    /// <summary>
    /// Gets the fields affected by this constraint.
    /// </summary>
    IReadOnlyCollection<string> AffectedFields { get; }
}
