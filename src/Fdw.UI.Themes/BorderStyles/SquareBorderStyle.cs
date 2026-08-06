using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Spectre.Console;

namespace Fdw.UI.Themes;

/// <summary>
/// Square border style - classic, sharp corners.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(BorderStyles), "Square", RestrictToCurrentCompilation = true)]
public sealed class SquareBorderStyle : BorderStyleBase
{
    /// <summary>
    /// Creates the square border style.
    /// </summary>
    public SquareBorderStyle() : base(2, "Square") { }

    /// <inheritdoc />
    public override BoxBorder Panel => BoxBorder.Square;

    /// <inheritdoc />
    public override BoxBorder Input => BoxBorder.Square;

    /// <inheritdoc />
    public override BoxBorder Menu => BoxBorder.Square;

    /// <inheritdoc />
    public override BoxBorder Dialog => BoxBorder.Double;

    /// <inheritdoc />
    public override TableBorder Table => TableBorder.Square;

    /// <inheritdoc />
    public override BoxBorder Selection => BoxBorder.Square;
}
