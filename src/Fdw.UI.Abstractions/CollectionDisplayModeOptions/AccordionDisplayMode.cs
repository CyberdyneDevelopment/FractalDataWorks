using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.CollectionDisplayModeOptions;

/// <summary>
/// Accordion/collapsible panels.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(CollectionDisplayModes), "Accordion", RestrictToCurrentCompilation = true)]
public sealed class AccordionDisplayMode : CollectionDisplayModeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AccordionDisplayMode"/> class.
    /// </summary>
    public AccordionDisplayMode() : base(0, "Accordion", supportsExpandCollapse: true, supportsGrouping: true) { }
}
