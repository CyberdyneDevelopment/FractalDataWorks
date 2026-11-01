using Fdw.Collections;

namespace Fdw.Services.Data;

/// <summary>
/// Interface for a lineage closure read direction. Each direction knows which
/// <see cref="DataSetLineageClosureRow"/> column it filters on, so the read path never switches on
/// the direction itself.
/// </summary>
public interface ILineageClosureDirection : ITypeOption<int, LineageClosureDirectionBase>
{
    /// <summary>
    /// Gets the name of the <see cref="DataSetLineageClosureRow"/> property this direction filters on.
    /// </summary>
    string ClosureIdPropertyName { get; }
}
