using System.Collections.Generic;

namespace Fdw.Data.Abstractions;

/// <summary>
/// A field binding that derives its value from a <see cref="IFilterExpression"/>
/// combining one or more source fields through a computation or transformation.
/// </summary>
/// <remarks>
/// Full computation support (expression evaluation, code generation) lands in Phase 3.
/// The <see cref="Expression"/> and <see cref="Dependencies"/> members are available
/// for shape inspection and lineage graph traversal immediately.
/// </remarks>
public interface IComputedFieldBinding : IFieldBinding
{
    /// <summary>
    /// Gets the expression that computes the field value from the <see cref="Dependencies"/>.
    /// </summary>
    IFilterExpression Expression { get; }

    /// <summary>
    /// Gets the source fields that feed into <see cref="Expression"/>.
    /// </summary>
    IReadOnlyList<IDataField> Dependencies { get; }
}
