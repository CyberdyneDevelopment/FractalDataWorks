using System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Fdw.UI.Components.Icons;

/// <summary>
/// Renders a glyph from <see cref="IconGlyphs"/> as an <c>svg</c> element. The glyph supplies the shape
/// and the attributes it ships with; the call site supplies size and colour.
/// </summary>
/// <remarks>
/// A plain <see cref="ComponentBase"/> rather than a .razor file so the one place svg markup is composed
/// is C#, and no .razor document in the UI packages contains an <c>svg</c> element at all.
/// </remarks>
public sealed class Icon : ComponentBase
{
    /// <summary>
    /// Gets or sets the registered glyph name, e.g. <c>Delete</c>.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the css class placed on the <c>svg</c> element.
    /// </summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>
    /// Gets or sets the inline style placed on the <c>svg</c> element — how a call site sizes and colours
    /// the glyph.
    /// </summary>
    [Parameter]
    public string? Style { get; set; }

    /// <summary>
    /// Gets or sets a stroke width that overrides the glyph's own. An empty string draws the glyph with no
    /// stroke-width attribute at all.
    /// </summary>
    [Parameter]
    public string? StrokeWidth { get; set; }

    /// <summary>
    /// Gets or sets a value overriding whether the glyph's paths take round caps and joins.
    /// </summary>
    [Parameter]
    public bool? Rounded { get; set; }

    /// <inheritdoc/>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (string.IsNullOrEmpty(Name))
            throw new InvalidOperationException("Icon requires a Name; no glyph can be selected without one.");

        var glyph = IconGlyphs.ByName(Name);
        if (ReferenceEquals(glyph, IconGlyphs.NotFound))
            throw new InvalidOperationException($"No icon glyph is registered under the name '{Name}'.");

        builder.OpenElement(0, "svg");
        builder.AddAttribute(1, "viewBox", glyph.ViewBox);
        builder.AddAttribute(2, "fill", glyph.Fill);

        if (!string.IsNullOrEmpty(glyph.Stroke))
            builder.AddAttribute(3, "stroke", glyph.Stroke);

        if (!string.IsNullOrEmpty(Class))
            builder.AddAttribute(4, "class", Class);

        if (!string.IsNullOrEmpty(Style))
            builder.AddAttribute(5, "style", Style);

        BuildPaths(builder, glyph);

        builder.CloseElement();
    }

    private void BuildPaths(RenderTreeBuilder builder, IIconGlyph glyph)
    {
        var strokeWidth = StrokeWidth is null ? glyph.StrokeWidth : StrokeWidth;
        var rounded = Rounded.HasValue ? Rounded.Value : glyph.Rounded;

        foreach (var pathData in glyph.Paths)
        {
            builder.OpenElement(10, "path");

            if (rounded)
            {
                builder.AddAttribute(11, "stroke-linecap", "round");
                builder.AddAttribute(12, "stroke-linejoin", "round");
            }

            if (!string.IsNullOrEmpty(strokeWidth))
                builder.AddAttribute(13, "stroke-width", strokeWidth);

            builder.AddAttribute(14, "d", pathData);
            builder.CloseElement();
        }
    }
}
