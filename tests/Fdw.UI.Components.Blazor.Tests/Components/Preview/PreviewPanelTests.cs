using Bunit;
using Fdw.Data.UI.Components;
using Microsoft.AspNetCore.Components;

namespace Fdw.UI.Components.Blazor.Tests.Components.Preview;

/// <summary>
/// Component tests for the standalone <see cref="PreviewPanel"/> FDW UI component. Relocated from
/// reference-ui's PreviewPanelTests, which asserted these behaviours via the reference app; here
/// they run directly against the param-only component. Render branches: loading spinner, results
/// table (Columns &amp; Rows), empty ("No results" when Columns but no Rows), nothing when no
/// Columns. Inputs: Columns/Rows binding incl. missing-cell fallback. Action: Export CSV → callback.
/// </summary>
[Trait("Category", "Ui")]
public sealed class PreviewPanelTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    private static IReadOnlyDictionary<string, object?>[] Rows(params IReadOnlyDictionary<string, object?>[] r) => r;

    private static Dictionary<string, object?> Row(params (string, object?)[] cells) =>
        cells.ToDictionary(c => c.Item1, c => c.Item2, StringComparer.Ordinal);

    [Fact]
    public void RendersLoadingSpinner()
    {
        var cut = _ctx.Render<PreviewPanel>(p => p.Add(x => x.IsLoading, true));
        // Why: current markup renders a `.loadwrap > .spin` spinner (no "Executing query" text).
        cut.Find(".loadwrap .spin").ShouldNotBeNull();
    }

    [Fact]
    public void RendersResultsTableWithRowCount()
    {
        var cut = _ctx.Render<PreviewPanel>(p => p
            .Add(x => x.Columns, ["id", "name"])
            .Add(x => x.Rows, Rows(Row(("id", 1), ("name", "Ann")), Row(("id", 2), ("name", "Bob")))));
        cut.Markup.ShouldContain("Results");
        cut.Markup.ShouldContain("2 rows returned");
        cut.Markup.ShouldContain("Ann");
        cut.Markup.ShouldContain("Bob");
        cut.FindAll("tbody tr").Count.ShouldBe(2);
    }

    [Fact]
    public void RendersMissingCellAsEmpty()
    {
        var cut = _ctx.Render<PreviewPanel>(p => p
            .Add(x => x.Columns, ["id", "missing"])
            .Add(x => x.Rows, Rows(Row(("id", 7)))));
        // Why: "missing" column absent from the row → empty cell, no exception.
        cut.FindAll("tbody td").Count.ShouldBe(2);
        cut.Markup.ShouldContain("7");
    }

    [Fact]
    public void RendersEmptyWhenColumnsButNoRows()
    {
        var cut = _ctx.Render<PreviewPanel>(p => p
            .Add(x => x.Columns, ["id"])
            .Add(x => x.Rows, Rows()));
        cut.Markup.ShouldContain("No results returned");
    }

    [Fact]
    public void RendersNothingWhenNoColumns()
    {
        var cut = _ctx.Render<PreviewPanel>(p => p.Add(x => x.Columns, []));
        cut.Markup.ShouldNotContain("Results");
        cut.Markup.ShouldNotContain("No results returned");
    }

    [Fact]
    public async Task ExportCsvButtonInvokesCallback()
    {
        var exported = false;
        var cut = _ctx.Render<PreviewPanel>(p => p
            .Add(x => x.Columns, ["id"])
            .Add(x => x.Rows, Rows(Row(("id", 1))))
            .Add(x => x.OnExportCsv, EventCallback.Factory.Create(this, () => exported = true)));
        cut.FindAll("button").First(b => b.TextContent.Contains("Export CSV", StringComparison.Ordinal)).Click();
        await Task.Yield();
        exported.ShouldBeTrue();
    }

    public void Dispose() => _ctx.Dispose();
}
