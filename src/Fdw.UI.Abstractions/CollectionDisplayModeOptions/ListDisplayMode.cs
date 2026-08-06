using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.CollectionDisplayModeOptions;

/// <summary>
/// Flat list.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(CollectionDisplayModes), "List", RestrictToCurrentCompilation = true)]
public sealed class ListDisplayMode : CollectionDisplayModeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListDisplayMode"/> class.
    /// </summary>
    public ListDisplayMode() : base(2, "List", supportsExpandCollapse: false, supportsGrouping: false) { }
}
