using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Base class for dataset composition type implementations.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption base class - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
public abstract class DataSetCompositionTypeBase : TypeOptionBase<int, DataSetCompositionTypeBase>, IDataSetCompositionType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataSetCompositionTypeBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this composition type.</param>
    /// <param name="name">The name of this composition type.</param>
    /// <param name="description">A description of this composition type's behavior.</param>
    /// <param name="allowsMultipleSources">Whether this composition type permits more than one source.</param>
    /// <param name="requiresJoins">Whether this composition type requires join definitions.</param>
    protected DataSetCompositionTypeBase(
        int id,
        string name,
        string description,
        bool allowsMultipleSources,
        bool requiresJoins)
        : base(id, name, description)
    {
        AllowsMultipleSources = allowsMultipleSources;
        RequiresJoins = requiresJoins;
    }

    /// <inheritdoc />
    public bool AllowsMultipleSources { get; }

    /// <inheritdoc />
    public bool RequiresJoins { get; }
}
