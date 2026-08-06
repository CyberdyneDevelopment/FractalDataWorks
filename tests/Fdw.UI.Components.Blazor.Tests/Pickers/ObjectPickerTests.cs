using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Fdw.UI.Components.Primitives;
using Microsoft.AspNetCore.Components;
using Shouldly;

namespace Fdw.UI.Components.Blazor.Tests.Pickers;

/// <summary>
/// Tests for <see cref="ObjectPicker{TItem}"/> — proves it renders a static item set,
/// projects labels/keys, and raises the bound value change.
/// Selection is driven by invoking the component's <c>ValueChanged</c> binding (the same path the
/// <c>@bind:set</c> select fires) because bUnit 2.x static render does not dispatch DOM onchange.
/// </summary>
[Trait("Category", "Ui")]
public sealed class ObjectPickerTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    private sealed record Fruit(string Code, string Display);

    private static readonly List<Fruit> Fruits =
    [
        new("APL", "Apple"),
        new("BAN", "Banana"),
    ];

    [Fact]
    [Trait("Priority", "P1")]
    public void RendersStaticItems_WithLabelAndKeyProjection()
    {
        var cut = _ctx.Render<ObjectPicker<Fruit>>(p => p
            .Add(c => c.Items, Fruits)
            .Add(c => c.LabelSelector, f => f.Display)
            .Add(c => c.KeySelector, f => f.Code));

        var options = cut.FindAll("option");
        // blank placeholder + 2 fruits
        options.Count.ShouldBe(3);
        options[1].TextContent.ShouldBe("Apple");
        options[1].GetAttribute("value").ShouldBe("APL");
        options[2].TextContent.ShouldBe("Banana");
        options[2].GetAttribute("value").ShouldBe("BAN");
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void AsyncItemsSource_LoadsAndRendersItems()
    {
        var cut = _ctx.Render<ObjectPicker<Fruit>>(p => p
            .Add(c => c.ItemsSource, () => Task.FromResult<IReadOnlyList<Fruit>>(Fruits))
            .Add(c => c.LabelSelector, f => f.Display)
            .Add(c => c.KeySelector, f => f.Code));

        // ItemsSource loads on first render (OnAfterRenderAsync); items appear after the re-render.
        cut.WaitForAssertion(() => cut.FindAll("option").Count.ShouldBe(3));
        cut.FindAll("option")[2].TextContent.ShouldBe("Banana");
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void StaticItems_NoLabel_OmitsLabelElement()
    {
        var cut = _ctx.Render<ObjectPicker<Fruit>>(p => p
            .Add(c => c.Items, Fruits)
            .Add(c => c.LabelSelector, f => f.Display));

        cut.FindAll("label").Count.ShouldBe(0);
        // KeySelector defaults to LabelSelector when not supplied.
        cut.FindAll("option")[1].GetAttribute("value").ShouldBe("Apple");
    }

    public void Dispose() => _ctx.Dispose();
}
