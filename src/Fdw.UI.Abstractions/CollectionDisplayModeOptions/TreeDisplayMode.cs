using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.CollectionDisplayModeOptions;

/// <summary>
/// Tree view with expand/collapse.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(CollectionDisplayModes), "Tree", RestrictToCurrentCompilation = true)]
public sealed class TreeDisplayMode : CollectionDisplayModeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TreeDisplayMode"/> class.
    /// </summary>
    public TreeDisplayMode() : base(4, "Tree", supportsExpandCollapse: true, supportsGrouping: true) { }
}
