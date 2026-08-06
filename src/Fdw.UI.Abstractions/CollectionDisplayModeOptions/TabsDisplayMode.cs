using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.CollectionDisplayModeOptions;

/// <summary>
/// Tabbed interface.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(CollectionDisplayModes), "Tabs", RestrictToCurrentCompilation = true)]
public sealed class TabsDisplayMode : CollectionDisplayModeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TabsDisplayMode"/> class.
    /// </summary>
    public TabsDisplayMode() : base(1, "Tabs", supportsExpandCollapse: false, supportsGrouping: false) { }
}
