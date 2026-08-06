using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Themes;

/// <summary>
/// ASCII icon set - maximum compatibility using only ASCII characters.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(IconSets), "Ascii", RestrictToCurrentCompilation = true)]
public sealed class AsciiIconSet : IconSetBase
{
    /// <summary>
    /// Creates the ASCII icon set.
    /// </summary>
    public AsciiIconSet() : base(2, "Ascii") { }

    /// <inheritdoc />
    public override string SelectedIndicator => ">";

    /// <inheritdoc />
    public override string UnselectedIndicator => " ";

    /// <inheritdoc />
    public override string CheckedIndicator => "[x]";

    /// <inheritdoc />
    public override string UncheckedIndicator => "[ ]";

    /// <inheritdoc />
    public override string RequiredIndicator => "*";

    /// <inheritdoc />
    public override string SuccessIcon => "[OK]";

    /// <inheritdoc />
    public override string ErrorIcon => "[ERR]";

    /// <inheritdoc />
    public override string WarningIcon => "[WARN]";

    /// <inheritdoc />
    public override string InfoIcon => "[INFO]";

    /// <inheritdoc />
    public override string ExpandedIcon => "[-]";

    /// <inheritdoc />
    public override string CollapsedIcon => "[+]";

    /// <inheritdoc />
    public override string LoadingIcon => "[...]";
}
