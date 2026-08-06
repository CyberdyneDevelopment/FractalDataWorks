using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.UI.Abstractions.CollectionDisplayModeOptions;

/// <summary>
/// Base class for collection display modes.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption base class - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
public abstract class CollectionDisplayModeBase : TypeOptionBase<int, CollectionDisplayModeBase>, ICollectionDisplayMode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CollectionDisplayModeBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this collection display mode.</param>
    /// <param name="name">The name of this collection display mode.</param>
    /// <param name="supportsExpandCollapse">Whether this display mode supports expand/collapse.</param>
    /// <param name="supportsGrouping">Whether this display mode supports grouping.</param>
    protected CollectionDisplayModeBase(int id, string name, bool supportsExpandCollapse, bool supportsGrouping)
        : base(id, name)
    {
        SupportsExpandCollapse = supportsExpandCollapse;
        SupportsGrouping = supportsGrouping;
    }

    /// <inheritdoc />
    public bool SupportsExpandCollapse { get; }

    /// <inheritdoc />
    public bool SupportsGrouping { get; }
}
