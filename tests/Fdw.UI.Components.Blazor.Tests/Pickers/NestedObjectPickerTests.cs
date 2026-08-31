using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Fdw.Data.UI.Components;
using Fdw.UI.Components.Primitives;
using Microsoft.AspNetCore.Components;
using Shouldly;

namespace Fdw.UI.Components.Blazor.Tests.Pickers;

/// <summary>
/// Tests for <see cref="NestedObjectPicker{TNode}"/> — proves the recursive drill-down over a
/// fake DataStore -> Path -> Container tree builds the full root->leaf selection chain and
/// terminates at leaves. Recursion is driven by invoking the rendered child
/// <see cref="ObjectPicker{TItem}"/>'s <c>ValueChanged</c> binding, which is wired to the
/// NestedObjectPicker's real selection handler (bUnit 2.x static render does not dispatch DOM onchange).
/// </summary>
[Trait("Category", "Ui")]
public sealed class NestedObjectPickerTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    // Minimal navigable node mirroring DataStore -> Path -> Container.
    private sealed class Node
    {
        public required string Name { get; init; }
        public IReadOnlyList<Node> Children { get; init; } = [];
    }

    // DataStore "Sales" -> Path "dbo" -> Containers "Orders" (leaf), "Customers" (leaf)
    private static IReadOnlyList<Node> BuildTree() =>
    [
        new Node
        {
            Name = "Sales",
            Children =
            [
                new Node
                {
                    Name = "dbo",
                    Children =
                    [
                        new Node { Name = "Orders" },
                        new Node { Name = "Customers" },
                    ],
                },
            ],
        },
    ];

    private IRenderedComponent<NestedObjectPicker<Node>> Render(
        IReadOnlyList<Node> roots,
        Action<IReadOnlyList<Node>> onChainChanged)
        => _ctx.Render<NestedObjectPicker<Node>>(p => p
            .Add(c => c.Items, roots)
            .Add(c => c.LabelSelector, (Func<Node, string>)(n => n.Name))
            .Add(c => c.KeySelector, (Func<Node, string>)(n => n.Name))
            .Add(c => c.GetChildren,
                (Func<Node, Task<IReadOnlyList<Node>>>)(n => Task.FromResult(n.Children)))
            .Add(c => c.IsLeaf, (Func<Node, bool>)(n => n.Children.Count == 0))
            .Add(c => c.SelectionChanged,
                EventCallback.Factory.Create<IReadOnlyList<Node>>(this, onChainChanged)));

    // Invoke the Nth (0-based) level picker's ValueChanged — the binding wired to the real handler.
    private static async Task SelectAtLevel(IRenderedComponent<NestedObjectPicker<Node>> cut, int level, string? key)
    {
        var picker = cut.FindComponents<ObjectPicker<Node>>()[level];
        await cut.InvokeAsync(() => picker.Instance.ValueChanged.InvokeAsync(key));
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void RootLevel_RendersOnlyOnePicker_BeforeSelection()
    {
        var cut = Render(BuildTree(), _ => { });
        // Only the root level picker exists until a selection drills in.
        cut.FindComponents<ObjectPicker<Node>>().Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task DrillingToLeaf_BuildsFullChain_AndTerminates()
    {
        IReadOnlyList<Node> chain = [];
        var cut = Render(BuildTree(), c => chain = c);

        // Select the DataStore (root). Recurses -> Path level picker appears.
        await SelectAtLevel(cut, 0, "Sales");
        cut.FindComponents<ObjectPicker<Node>>().Count.ShouldBe(2);
        chain.Select(n => n.Name).ShouldBe(["Sales"]);

        // Select the Path. Recurses -> Container level picker appears.
        await SelectAtLevel(cut, 1, "dbo");
        cut.FindComponents<ObjectPicker<Node>>().Count.ShouldBe(3);
        chain.Select(n => n.Name).ShouldBe(["Sales", "dbo"]);

        // Select a leaf Container. No further picker — recursion terminates at the leaf.
        await SelectAtLevel(cut, 2, "Orders");
        cut.FindComponents<ObjectPicker<Node>>().Count.ShouldBe(3);
        chain.Select(n => n.Name).ShouldBe(["Sales", "dbo", "Orders"]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task ChangingParent_ResetsDeeperLevels()
    {
        IReadOnlyList<Node> chain = [];
        var cut = Render(BuildTree(), c => chain = c);

        await SelectAtLevel(cut, 0, "Sales");
        await SelectAtLevel(cut, 1, "dbo");
        await SelectAtLevel(cut, 2, "Orders");
        chain.Count.ShouldBe(3);

        // Clearing the root selection collapses the whole chain.
        await SelectAtLevel(cut, 0, null);
        cut.FindComponents<ObjectPicker<Node>>().Count.ShouldBe(1);
        chain.Count.ShouldBe(0);
    }

    public void Dispose() => _ctx.Dispose();
}
