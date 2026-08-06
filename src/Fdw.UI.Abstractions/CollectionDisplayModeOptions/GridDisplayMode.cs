using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.CollectionDisplayModeOptions;

/// <summary>
/// Grid layout.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(CollectionDisplayModes), "Grid", RestrictToCurrentCompilation = true)]
public sealed class GridDisplayMode : CollectionDisplayModeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GridDisplayMode"/> class.
    /// </summary>
    public GridDisplayMode() : base(3, "Grid", supportsExpandCollapse: false, supportsGrouping: true) { }
}
