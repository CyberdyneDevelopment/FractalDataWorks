using System;
using System.Collections.Generic;
using System.Text.Json;
using Fdw.UI.Abstractions.Composition;
using Shouldly;
using Xunit;

namespace Fdw.UI.Rendering.Conformance.Tests;

/// <summary>
/// Covers the persisted arrangement schema — the data a user's composed view is saved as.
/// </summary>
/// <remarks>
/// Round-tripping is the contract that matters: a layout is written as JSON through the existing
/// per-user ISessionStateService and read back by a later session, possibly by a different renderer
/// than the one that wrote it. Anything that does not survive serialisation cannot be on it.
/// </remarks>
[Trait("Priority", "P0")]
[Trait("Category", "Rendering")]
public class ComposedViewLayoutTests
{
    private static ComposedViewLayout SampleLayout() => new()
    {
        ViewId = "billing-overview",
        DisplayName = "Billing Overview",
        ColumnCount = 12,
        Components =
        [
            new PlacedComponent
            {
                Id = Guid.Parse("0195f1a0-0000-7000-8000-000000000001"),
                ComponentKey = "connection-health",
                Column = 0, Row = 0, Width = 6, Height = 2,
                Settings = new Dictionary<string, string>(StringComparer.Ordinal) { ["ConnectionName"] = "OpsDb" },
            },
            new PlacedComponent
            {
                Id = Guid.Parse("0195f1a0-0000-7000-8000-000000000002"),
                ComponentKey = "connection-health",
                Column = 6, Row = 0, Width = 6, Height = 2,
                Settings = new Dictionary<string, string>(StringComparer.Ordinal) { ["ConnectionName"] = "NflDb" },
            },
        ],
    };

    [Fact]
    public void LayoutRoundTripsThroughJson()
    {
        var json = JsonSerializer.Serialize(SampleLayout());
        var restored = JsonSerializer.Deserialize<ComposedViewLayout>(json);

        restored.ShouldNotBeNull();
        restored!.ViewId.ShouldBe("billing-overview");
        restored.ColumnCount.ShouldBe(12);
        restored.Components.Count.ShouldBe(2);
        restored.Components[0].ComponentKey.ShouldBe("connection-health");
        restored.Components[0].Width.ShouldBe(6);
        restored.Components[0].Settings["ConnectionName"].ShouldBe("OpsDb");
    }

    /// <summary>
    /// The same component placed twice must stay independently addressable — that is why a
    /// placement carries its own Id distinct from the catalogue key.
    /// </summary>
    [Fact]
    public void TheSameComponentCanBePlacedTwiceWithDifferentSettings()
    {
        var restored = JsonSerializer.Deserialize<ComposedViewLayout>(JsonSerializer.Serialize(SampleLayout()))!;

        restored.Components[0].ComponentKey.ShouldBe(restored.Components[1].ComponentKey);
        restored.Components[0].Id.ShouldNotBe(restored.Components[1].Id);
        restored.Components[0].Settings["ConnectionName"]
            .ShouldNotBe(restored.Components[1].Settings["ConnectionName"]);
    }

    [Fact]
    public void SessionStateKeyFollowsTheDomainPageComponentConvention()
    {
        // ISessionStateService documents keys as {domain}:{page}:{component}; layouts sort and
        // enumerate alongside the filter/view state the UI already stores there.
        ComposedViewLayout.KeyFor("billing-overview").ShouldBe("layout:billing-overview:composition");
    }

    [Fact]
    public void ColumnCountTravelsWithTheLayout()
    {
        // Placements are expressed in grid units, so the column count they were authored against is
        // what makes those units mean anything — a host imposing its own would reflow every saved view.
        var restored = JsonSerializer.Deserialize<ComposedViewLayout>(
            JsonSerializer.Serialize(new ComposedViewLayout { ViewId = "v", ColumnCount = 24 }))!;

        restored.ColumnCount.ShouldBe(24);
    }
}
