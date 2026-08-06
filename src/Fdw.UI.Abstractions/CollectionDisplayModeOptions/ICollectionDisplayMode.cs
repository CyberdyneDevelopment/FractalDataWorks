using Fdw.Collections;

namespace Fdw.UI.Abstractions.CollectionDisplayModeOptions;

/// <summary>
/// Interface for collection display modes.
/// Extends ITypeOption to enable TypeCollection discovery.
/// </summary>
public interface ICollectionDisplayMode : ITypeOption<int, CollectionDisplayModeBase>
{
    /// <summary>
    /// Gets a value indicating whether this display mode supports expand/collapse.
    /// </summary>
    bool SupportsExpandCollapse { get; }

    /// <summary>
    /// Gets a value indicating whether this display mode supports grouping.
    /// </summary>
    bool SupportsGrouping { get; }
}
