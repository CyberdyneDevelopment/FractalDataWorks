using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Themes;

/// <summary>
/// Unicode icon set - uses Unicode symbols for modern terminals.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(IconSets), "Unicode", RestrictToCurrentCompilation = true)]
public sealed class UnicodeIconSet : IconSetBase
{
    /// <summary>
    /// Creates the Unicode icon set.
    /// </summary>
    public UnicodeIconSet() : base(1, "Unicode") { }

    /// <inheritdoc />
    public override string SelectedIndicator => "\u25cf";  // ●

    /// <inheritdoc />
    public override string UnselectedIndicator => "\u25cb";  // ○

    /// <inheritdoc />
    public override string CheckedIndicator => "\u2611";  // ☑

    /// <inheritdoc />
    public override string UncheckedIndicator => "\u2610";  // ☐

    /// <inheritdoc />
    public override string RequiredIndicator => "*";

    /// <inheritdoc />
    public override string SuccessIcon => "\u2713";  // ✓

    /// <inheritdoc />
    public override string ErrorIcon => "\u2717";  // ✗

    /// <inheritdoc />
    public override string WarningIcon => "\u26a0";  // ⚠

    /// <inheritdoc />
    public override string InfoIcon => "\u2139";  // ℹ

    /// <inheritdoc />
    public override string ExpandedIcon => "\u25bc";  // ▼

    /// <inheritdoc />
    public override string CollapsedIcon => "\u25b6";  // ▶

    /// <inheritdoc />
    public override string LoadingIcon => "\u231b";  // ⌛
}
