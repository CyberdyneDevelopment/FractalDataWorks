using System.Collections.Generic;
using Fdw.Results;

namespace Fdw.Data.Abstractions;

/// <summary>
/// A logical dataset composed of one or more <see cref="IDataNode"/> sources.
/// </summary>
/// <remarks>
/// A DataSet progresses through four states based on which members are populated:
/// <list type="bullet">
/// <item><description>Described only — Name + Description; no Fields or Sources yet.</description></item>
/// <item><description>Described + Defined — Fields explicitly declared; no Sources yet (matchable by shape).</description></item>
/// <item><description>Described + Sourced — Sources present; Fields inferred from Sources on first access.</description></item>
/// <item><description>Fully resolved — Fields declared AND Sources present; query via Sources, verified against definition.</description></item>
/// </list>
/// State is determined at runtime from property presence, not from an explicit discriminator property.
/// </remarks>
public interface IDataSet : IDataNode
{
    /// <summary>
    /// Gets the composition strategy for this dataset.
    /// </summary>
    /// <remarks>
    /// <c>Singular</c> when the dataset wraps a single source without transformation.
    /// <c>Join</c> or <c>Union</c> when multiple sources are combined.
    /// </remarks>
    IDataSetCompositionType Composition { get; }

    /// <summary>
    /// Gets the data nodes that supply rows for this dataset.
    /// </summary>
    /// <remarks>
    /// Empty when the dataset is Described-only or Defined but not yet sourced.
    /// </remarks>
    IReadOnlyList<IDataSetSource> Sources { get; }

    /// <summary>
    /// Gets the join definitions when <see cref="Composition"/> requires joins.
    /// </summary>
    /// <remarks>
    /// Empty for Singular and Union compositions.
    /// </remarks>
    IReadOnlyList<IDataSetJoin> Joins { get; }

    /// <summary>
    /// Returns the field with the given name, or a failure result if absent.
    /// </summary>
    /// <param name="name">The field name to look up (case-insensitive).</param>
    /// <returns>
    /// Success with the matching <see cref="IDataField"/>, or Failure when no field with
    /// <paramref name="name"/> exists. Callers MUST check <c>IsSuccess</c> before using <c>.Value</c>.
    /// </returns>
    IGenericResult<IDataField> Field(string name);
}
