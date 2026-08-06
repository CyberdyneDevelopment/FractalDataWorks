using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Spectre.Console;

namespace Fdw.UI.Themes;

/// <summary>
/// Rounded border style - modern, soft appearance.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(BorderStyles), "Rounded", RestrictToCurrentCompilation = true)]
public sealed class RoundedBorderStyle : BorderStyleBase
{
    /// <summary>
    /// Creates the rounded border style.
    /// </summary>
    public RoundedBorderStyle() : base(1, "Rounded") { }

    /// <inheritdoc />
    public override BoxBorder Panel => BoxBorder.Rounded;

    /// <inheritdoc />
    public override BoxBorder Input => BoxBorder.Rounded;

    /// <inheritdoc />
    public override BoxBorder Menu => BoxBorder.Rounded;

    /// <inheritdoc />
    public override BoxBorder Dialog => BoxBorder.Rounded;

    /// <inheritdoc />
    public override TableBorder Table => TableBorder.Rounded;

    /// <inheritdoc />
    public override BoxBorder Selection => BoxBorder.Rounded;
}
