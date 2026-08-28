using System;
using Fdw.Messages;
using Fdw.Results;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Fdw.Data.Components.Annotations;
using Fdw.Data.Components.DataSets;
using Fdw.Services.Catalog.Clients.Models;
using Fdw.Services.Data.Clients.Models;
using Fdw.UI.Components.Blazor.Tests.DataInfra;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Fdw.UI.Components.Blazor.Tests.Components.Data;

/// <summary>
/// Branch-complete bUnit tests for the FDW <c>DataSetDetail</c> page
/// (<c>Fdw.UI.Pages.Data.Pages.DataSetDetailPage</c>). Relocated from reference-ui's
/// DataSetDetailPageTests.
///
/// Covers the top-level render branches (loading / not-found / loaded), each of the five tabs
/// (fields / sources / lineage / preview / annotations) including their nested conditional
/// content, and the nested <see cref="AnnotationProvider"/> branches (loading / error / empty /
/// populated) plus the annotation submit action.
///
/// Uses TWO swap mechanisms: an <see cref="InheritingProviderFactory{TActual,TStub}"/> for the
/// page's <c>@ref</c>-captured <see cref="DataSetProvider"/> (seeded via a cascading
/// <see cref="DataSetContextSeed"/>), and a <see cref="ProviderFactory{TActual,TContext}"/> for
/// the nested <see cref="AnnotationProvider"/>.
/// </summary>
[Trait("Category", "Ui")]
public sealed class DataSetDetailPageTests : IDisposable
{
    private static readonly string[] ExpectedDataSetTags = ["pii", "finance"];

    private readonly BunitContext _ctx = new();

    private DataSetContext _dsSeed = new();

    private void Swap(DataSetContext? ds = null, AnnotationContext? ann = null)
    {
        _dsSeed = ds ?? new DataSetContext();
        _ctx.RegisterProviderInfrastructure();
        _ctx.ComponentFactories.Add(new InheritingProviderFactory<DataSetProvider, StubDataSetProvider>());
        _ctx.ComponentFactories.Add(new ProviderFactory<AnnotationProvider, AnnotationContext>(ann));
    }

    private static DataSetDetailPayload Detail(
        IReadOnlyList<DataSetFieldPayload>? fields = null,
        IReadOnlyList<DataSetSourcePayload>? sources = null) => new()
    {
        Name = "Customers",
        Fields = fields ?? [],
        Sources = sources ?? []
    };

    private IRenderedComponent<Fdw.UI.Pages.Data.Pages.DataSetDetailPage> RenderDetail() =>
        _ctx.Render<Fdw.UI.Pages.Data.Pages.DataSetDetailPage>(p => p
            .AddCascadingValue(new DataSetContextSeed { Value = _dsSeed })
            .Add(x => x.Name, "Customers"));

    // ── Top-level branches ──────────────────────────────────────────────────

    [Fact]
    public void RendersLoadingSpinnerWhenLoading()
    {
        Swap(new DataSetContext { IsLoading = true });
        var cut = RenderDetail();
        // Markup drift: spinner class is now ".spin" (inside ".loadwrap"), formerly ".animate-spin".
        cut.Find(".spin").ShouldNotBeNull();
    }

    [Fact]
    public void RendersNotFoundWhenCurrentDataSetNull()
    {
        Swap(new DataSetContext { IsLoading = false, CurrentDataSet = null });
        var cut = RenderDetail();
        cut.Markup.ShouldContain("DataSet not found.");
    }

    [Fact]
    public void RendersActiveBadgeAndTabsWhenLoaded()
    {
        Swap(new DataSetContext { CurrentDataSet = Detail() });
        var cut = RenderDetail();
        cut.Markup.ShouldContain("Active");
        cut.Markup.ShouldContain("Fields (0)");
        cut.Markup.ShouldContain("Sources (0)");
    }

    // ── Fields tab (default) ────────────────────────────────────────────────

    [Fact]
    public void FieldsTabRendersFieldRowsOrderedByOrdinal()
    {
        var fields = new DataSetFieldPayload[]
        {
            new() { Name = "Id", DataType = "Guid", Ordinal = 1, IsKey = true, IsRequired = true },
            new() { Name = "Email", DataType = "String", Ordinal = 0, IsRequired = false }
        };
        Swap(new DataSetContext { CurrentDataSet = Detail(fields) });
        var cut = RenderDetail();
        var rows = cut.FindAll("tbody tr");
        rows.Count.ShouldBe(2);
        // ordered by ordinal => Email (0) first
        rows[0].TextContent.ShouldContain("Email", Case.Sensitive);
        rows[1].TextContent.ShouldContain("Id", Case.Sensitive);
        // nullable column: IsRequired=false => "Yes"
        rows[0].TextContent.ShouldContain("Yes", Case.Sensitive);
        rows[1].TextContent.ShouldContain("No", Case.Sensitive);
    }

    // ── Sources tab ─────────────────────────────────────────────────────────

    [Fact]
    public void SourcesTabShowsEmptyMessageWhenNoSources()
    {
        Swap(new DataSetContext { CurrentDataSet = Detail() });
        var cut = RenderDetail();
        cut.FindAll(".tabs a").First(a => a.TextContent.Contains("Sources (", StringComparison.Ordinal)).Click();
        cut.Markup.ShouldContain("No sources configured.");
    }

    [Fact]
    public void SourcesTabRendersSourceRowsAndMappingsTableWhenPresent()
    {
        var src = new DataSetSourcePayload
        {
            SourceName = "Primary",
            DataStoreName = "Sql1",
            PathValue = "dbo.Customers",
            IsPrimary = true,
            FieldMappings = [new DataSetFieldMappingPayload { DataSetFieldName = "Id", SourceName = "Primary", SourceFieldName = "CustId" }]
        };
        Swap(new DataSetContext { CurrentDataSet = Detail(sources: [src]) });
        var cut = RenderDetail();
        cut.FindAll(".tabs a").First(a => a.TextContent.Contains("Sources (", StringComparison.Ordinal)).Click();
        cut.Markup.ShouldContain("Primary");
        cut.Markup.ShouldContain("dbo.Customers");
        cut.Markup.ShouldContain("Field Mappings");
        cut.Markup.ShouldContain("CustId");
    }

    [Fact]
    public void SourcesTabHidesMappingsTableWhenNoMappings()
    {
        var src = new DataSetSourcePayload { SourceName = "Primary", DataStoreName = "Sql1", FieldMappings = [] };
        Swap(new DataSetContext { CurrentDataSet = Detail(sources: [src]) });
        var cut = RenderDetail();
        cut.FindAll(".tabs a").First(a => a.TextContent.Contains("Sources (", StringComparison.Ordinal)).Click();
        cut.Markup.ShouldNotContain("Field Mappings");
    }

    // ── Lineage tab ─────────────────────────────────────────────────────────

    [Fact]
    public void LineageTabRendersOpenLineageButtonAndNavigates()
    {
        Swap(new DataSetContext { CurrentDataSet = Detail() });
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();
        var cut = RenderDetail();
        cut.FindAll(".tabs a").First(a => string.Equals(a.TextContent.Trim(), "Lineage", StringComparison.Ordinal)).Click();
        cut.Markup.ShouldContain("Open Lineage Explorer");
        cut.FindAll("button").First(b => b.TextContent.Contains("Open Lineage Explorer", StringComparison.Ordinal)).Click();
        nav.Uri.ShouldEndWith("/lineage/DataSet/Customers");
    }

    // ── Preview tab ─────────────────────────────────────────────────────────


    [Fact]
    public void PreviewTabRendersInlinePreviewPaneControls()
    {
        Swap(new DataSetContext { CurrentDataSet = Detail() });
        var cut = RenderDetail();
        cut.FindAll(".tabs a").First(a => string.Equals(a.TextContent.Trim(), "Preview", StringComparison.Ordinal)).Click();

        cut.Markup.ShouldContain("Run Query");
        cut.FindAll("option").Select(o => o.TextContent.Trim())
            .ShouldBe(["10 rows", "25 rows", "50 rows", "100 rows"], ignoreOrder: false);
    }

    [Fact]
    public void PreviewTabRunQueryIsDisabledUntilTheDataSetHasASource()
    {
        // The page gates Run Query on ctx.CurrentDataSet.Sources.Count — a dataset with no source
        // has nothing to query, so the button must not be clickable.
        Swap(new DataSetContext { CurrentDataSet = Detail() });
        var cut = RenderDetail();
        cut.FindAll(".tabs a").First(a => string.Equals(a.TextContent.Trim(), "Preview", StringComparison.Ordinal)).Click();

        cut.FindAll("button").First(b => b.TextContent.Contains("Run Query", StringComparison.Ordinal))
            .HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public void PreviewTabRunQueryIsEnabledOnceASourceExists()
    {
        Swap(new DataSetContext
        {
            CurrentDataSet = Detail(sources:
            [
                new DataSetSourcePayload { SourceName = "Primary", DataStoreName = "Sql1", FieldMappings = [] }
            ])
        });
        var cut = RenderDetail();
        cut.FindAll(".tabs a").First(a => string.Equals(a.TextContent.Trim(), "Preview", StringComparison.Ordinal)).Click();

        cut.FindAll("button").First(b => b.TextContent.Contains("Run Query", StringComparison.Ordinal))
            .HasAttribute("disabled").ShouldBeFalse();
    }

    // ── Annotations tab — nested AnnotationProvider branches ────────────────

    [Fact]
    public void AnnotationsTabShowsSpinnerWhenAnnotationContextLoading()
    {
        Swap(new DataSetContext { CurrentDataSet = Detail() }, new AnnotationContext { IsLoading = true });
        var cut = RenderDetail();
        cut.FindAll(".tabs a").First(a => string.Equals(a.TextContent.Trim(), "Annotations", StringComparison.Ordinal)).Click();
        // Markup drift: spinner class is now ".spin", formerly ".animate-spin".
        cut.Find(".spin").ShouldNotBeNull();
    }

    [Fact]
    public void AnnotationsTabShowsErrorWhenAnnotationContextHasError()
    {
        Swap(new DataSetContext { CurrentDataSet = Detail() }, new AnnotationContext { LastResult = GenericResult.Failure(new GenericMessage("ann-boom")) });
        var cut = RenderDetail();
        cut.FindAll(".tabs a").First(a => string.Equals(a.TextContent.Trim(), "Annotations", StringComparison.Ordinal)).Click();
        cut.Markup.ShouldContain("ann-boom");
    }

    [Fact]
    public void AnnotationsTabShowsEmptyMessageWhenNoAnnotations()
    {
        Swap(new DataSetContext { CurrentDataSet = Detail() }, new AnnotationContext { Annotations = [] });
        var cut = RenderDetail();
        cut.FindAll(".tabs a").First(a => string.Equals(a.TextContent.Trim(), "Annotations", StringComparison.Ordinal)).Click();
        cut.Markup.ShouldContain("No annotations for this DataSet");
    }

    [Fact]
    public void AnnotationsTabRendersAnnotationRowsWithClassificationAndTags()
    {
        var ann = new DataSetAnnotationPayload
        {
            DataSetName = "Customers",
            Owner = "alice",
            Steward = "bob",
            Classification = "Confidential",
            Tags = ["pii", "finance"]
        };
        Swap(new DataSetContext { CurrentDataSet = Detail() }, new AnnotationContext { Annotations = [ann] });
        var cut = RenderDetail();
        cut.FindAll(".tabs a").First(a => string.Equals(a.TextContent.Trim(), "Annotations", StringComparison.Ordinal)).Click();
        cut.Markup.ShouldContain("Catalog Metadata");
        cut.Markup.ShouldContain("alice");
        cut.Markup.ShouldContain("bob");
        cut.Markup.ShouldContain("Confidential");
        cut.Markup.ShouldContain("pii");
        cut.Markup.ShouldContain("finance");
    }

    [Fact]
    public void AnnotationsTabShowsDashesWhenOwnerStewardClassificationBlank()
    {
        var ann = new DataSetAnnotationPayload { DataSetName = "Customers", Owner = null, Steward = null, Classification = null, Tags = [] };
        Swap(new DataSetContext { CurrentDataSet = Detail() }, new AnnotationContext { Annotations = [ann] });
        var cut = RenderDetail();
        cut.FindAll(".tabs a").First(a => string.Equals(a.TextContent.Trim(), "Annotations", StringComparison.Ordinal)).Click();
        // owner cell falls back to "-"
        cut.Markup.ShouldContain("Catalog Metadata");
        string.Equals(cut.FindAll("tbody tr td")[0].TextContent.Trim(), "-", StringComparison.Ordinal).ShouldBeTrue();
    }

    [Fact]
    public async Task AnnotationsTabSubmitAnnotationInvokesOnCreateWithSplitTags()
    {
        CreateAnnotationRequest? captured = null;
        Swap(new DataSetContext { CurrentDataSet = Detail() }, new AnnotationContext
        {
            Annotations = [],
            OnCreate = req => { captured = req; return Task.CompletedTask; }
        });
        var cut = RenderDetail();
        cut.FindAll(".tabs a").First(a => string.Equals(a.TextContent.Trim(), "Annotations", StringComparison.Ordinal)).Click();

        cut.FindAll("input").First(i => string.Equals(i.GetAttribute("placeholder"), "Data owner", StringComparison.Ordinal)).Change("alice");
        cut.FindAll("input").First(i => string.Equals(i.GetAttribute("placeholder"), "Data steward", StringComparison.Ordinal)).Change("bob");
        cut.Find("select.fin").Change("Internal");
        cut.FindAll("input").First(i => i.GetAttribute("placeholder")!.Contains("finance", StringComparison.Ordinal)).Change("pii, finance");

        cut.FindAll("button").First(b => b.TextContent.Contains("Save Annotation", StringComparison.Ordinal)).Click();
        await Task.Yield();

        captured.ShouldNotBeNull();
        captured!.Owner.ShouldBe("alice");
        captured.Steward.ShouldBe("bob");
        captured.Classification.ShouldBe("Internal");
        captured.Tags.ShouldBe(ExpectedDataSetTags);
    }

    public void Dispose() => _ctx.Dispose();
}
