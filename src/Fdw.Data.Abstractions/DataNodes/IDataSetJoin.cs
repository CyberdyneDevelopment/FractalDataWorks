namespace Fdw.Data.Abstractions;

/// <summary>
/// Defines a JOIN relationship between two <see cref="IDataSetSource"/> entries in an
/// <see cref="IDataSet"/> whose <see cref="IDataSet.Composition"/> requires joins.
/// </summary>
public interface IDataSetJoin
{
    /// <summary>
    /// Gets the left-hand source of the join.
    /// </summary>
    IDataSetSource Left { get; }

    /// <summary>
    /// Gets the right-hand source of the join.
    /// </summary>
    IDataSetSource Right { get; }

    /// <summary>
    /// Gets the filter expression that forms the ON clause of the join.
    /// </summary>
    IFilterExpression Condition { get; }

    /// <summary>
    /// Gets the type of join (INNER, LEFT, RIGHT, or FULL OUTER).
    /// </summary>
    IJoinType Type { get; }
}
