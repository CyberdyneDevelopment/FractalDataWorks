using Bunit;
using Fdw.UI.Components.Icons;

namespace Fdw.UI.Components.Blazor.Tests.Components.Icons;

/// <summary>
/// Component tests for <see cref="Icon"/> and the <see cref="IconGlyphs"/> registry behind it. Covers the
/// attributes a glyph ships with, the call-site overrides for size, colour and weight, the empty
/// stroke-width that leaves the weight to be inherited from css, and the loud failures on a name that
/// names nothing.
/// </summary>
[Trait("Category", "Ui")]
public sealed class IconTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    [Fact]
    public void RegistryHoldsEveryGlyphUnderADistinctName()
    {
        var all = IconGlyphs.All();

        all.ShouldNotBeEmpty();
        all.Select(glyph => glyph.Name).Distinct(StringComparer.Ordinal).Count().ShouldBe(all.Count);
    }

    [Fact]
    public void ByNameFindsAGlyph()
    {
        IconGlyphs.ByName("Delete").Paths.ShouldNotBeEmpty();
    }

    [Fact]
    public void ByNameAnswersNotFoundForAnUnregisteredName()
    {
        IconGlyphs.ByName("NoSuchGlyph").ShouldBe(IconGlyphs.NotFound);
    }

    [Fact]
    public void RendersTheAttributesTheGlyphShipsWith()
    {
        var markup = _ctx.Render<Icon>(p => p.Add(x => x.Name, "Delete")).Markup;

        markup.ShouldContain("viewBox=\"0 0 24 24\"");
        markup.ShouldContain("fill=\"none\"");
        markup.ShouldContain("stroke=\"currentColor\"");
        markup.ShouldContain("stroke-width=\"2\"");
        markup.ShouldContain("stroke-linecap=\"round\"");
        markup.ShouldContain("stroke-linejoin=\"round\"");
        markup.ShouldContain(IconGlyphs.ByName("Delete").Paths[0]);
    }

    [Fact]
    public void RendersAGlyphOwnStrokeColourRatherThanCurrentColor()
    {
        _ctx.Render<Icon>(p => p.Add(x => x.Name, "DeleteAlt")).Markup
            .ShouldContain("stroke=\"#e05c4a\"");
    }

    [Fact]
    public void OmitsStrokeOnASolidGlyph()
    {
        var markup = _ctx.Render<Icon>(p => p.Add(x => x.Name, "Stop")).Markup;

        markup.ShouldContain("fill=\"currentColor\"");
        markup.ShouldNotContain("stroke=");
    }

    [Fact]
    public void RendersEveryPathOfAMultiPathGlyph()
    {
        _ctx.Render<Icon>(p => p.Add(x => x.Name, "Run"))
            .FindAll("path").Count.ShouldBe(2);
    }

    [Fact]
    public void PassesClassAndStyleThroughToTheSvg()
    {
        var svg = _ctx.Render<Icon>(p => p
            .Add(x => x.Name, "Add")
            .Add(x => x.Class, "ic")
            .Add(x => x.Style, "width:15px;height:15px;")).Find("svg");

        svg.GetAttribute("class").ShouldBe("ic");
        svg.GetAttribute("style").ShouldBe("width:15px;height:15px;");
    }

    [Fact]
    public void StrokeWidthOverridesTheGlyphWeight()
    {
        _ctx.Render<Icon>(p => p
            .Add(x => x.Name, "Edit")
            .Add(x => x.StrokeWidth, "1.8")).Find("path")
            .GetAttribute("stroke-width").ShouldBe("1.8");
    }

    [Fact]
    public void EmptyStrokeWidthDrawsNoAttributeSoTheWeightIsInherited()
    {
        _ctx.Render<Icon>(p => p
            .Add(x => x.Name, "Check")
            .Add(x => x.StrokeWidth, string.Empty)).Find("path")
            .HasAttribute("stroke-width").ShouldBeFalse();
    }

    [Fact]
    public void RoundedOverridesTheGlyphCaps()
    {
        var path = _ctx.Render<Icon>(p => p
            .Add(x => x.Name, "ChevronLeft")
            .Add(x => x.Rounded, false)).Find("path");

        path.HasAttribute("stroke-linecap").ShouldBeFalse();
        path.HasAttribute("stroke-linejoin").ShouldBeFalse();
    }

    [Fact]
    public void AnUnregisteredNameFailsLoudly()
    {
        Should.Throw<InvalidOperationException>(
            () => _ctx.Render<Icon>(p => p.Add(x => x.Name, "NoSuchGlyph")));
    }

    [Fact]
    public void AMissingNameFailsLoudly()
    {
        Should.Throw<InvalidOperationException>(() => _ctx.Render<Icon>());
    }

    public void Dispose() => _ctx.Dispose();
}
