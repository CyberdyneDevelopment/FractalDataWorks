using Fdw.Collections;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Defines how a <see cref="IDataSet"/> composes its sources into a unified result.
/// </summary>
/// <remarks>
/// Inherits Id, Name, and Description from ITypeOption.
/// </remarks>
public interface IDataSetCompositionType : ITypeOption<int, DataSetCompositionTypeBase>
{
    /// <summary>
    /// Gets whether this composition type permits more than one source.
    /// </summary>
    bool AllowsMultipleSources { get; }

    /// <summary>
    /// Gets whether this composition type requires <see cref="IDataSetJoin"/> entries.
    /// </summary>
    bool RequiresJoins { get; }
}
