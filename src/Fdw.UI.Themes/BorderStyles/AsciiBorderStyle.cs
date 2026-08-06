using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Spectre.Console;

namespace Fdw.UI.Themes;

/// <summary>
/// ASCII border style - maximum compatibility, uses only ASCII characters.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(BorderStyles), "Ascii", RestrictToCurrentCompilation = true)]
public sealed class AsciiBorderStyle : BorderStyleBase
{
    /// <summary>
    /// Creates the ASCII border style.
    /// </summary>
    public AsciiBorderStyle() : base(3, "Ascii") { }

    /// <inheritdoc />
    public override BoxBorder Panel => BoxBorder.Ascii;

    /// <inheritdoc />
    public override BoxBorder Input => BoxBorder.Ascii;

    /// <inheritdoc />
    public override BoxBorder Menu => BoxBorder.Ascii;

    /// <inheritdoc />
    public override BoxBorder Dialog => BoxBorder.Ascii;

    /// <inheritdoc />
    public override TableBorder Table => TableBorder.Ascii;

    /// <inheritdoc />
    public override BoxBorder Selection => BoxBorder.Ascii;
}
