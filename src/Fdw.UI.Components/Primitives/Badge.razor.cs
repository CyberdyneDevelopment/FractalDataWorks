using Fdw.UI.Components.Services;
using Microsoft.AspNetCore.Components;

namespace Fdw.UI.Components.Primitives;

/// <summary>
/// Renders a status pill. The <see cref="Variant"/> supplies the colour; the call site supplies the text.
/// </summary>
/// <remarks>
/// A page picks a tone — "this execution failed" — and never a css class. The tone-to-class mapping lives
/// once, on the <see cref="StatusVariants"/> member, which is what keeps two pages showing the same state
/// in the same colour.
/// </remarks>
public partial class Badge
{
    /// <summary>
    /// Gets or sets the tone the pill is drawn in.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public StatusVariantBase Variant { get; set; } = default!;

    /// <summary>
    /// Gets or sets a value indicating whether the leading colour dot is drawn. Defaults to
    /// <see langword="true"/>, which is the shape the pill was designed around.
    /// </summary>
    [Parameter]
    public bool ShowDot { get; set; } = true;

    /// <summary>
    /// Gets or sets css classes appended after the variant's own, for the call sites that add a utility
    /// such as a font override.
    /// </summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>
    /// Gets or sets the pill's content.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private string PillClass => string.IsNullOrEmpty(Class)
        ? "badge " + Variant.BadgeClass
        : "badge " + Variant.BadgeClass + " " + Class;
}
