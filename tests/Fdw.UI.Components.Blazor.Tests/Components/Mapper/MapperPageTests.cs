using Bunit;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Data.Components;
using Fdw.Data.Components.DataMapper;
using Fdw.Data.Components.Models;
using Fdw.Services.Data.Clients.Models;
using Fdw.UI.Components.Blazor.Tests.PipeInfra;
using MapperPage = Fdw.UI.Pages.Data.Pages.MapperPage;

namespace Fdw.UI.Components.Blazor.Tests.Components.Mapper;

/// <summary>
/// Component tests for the Field Mapper page (Mapper.razor), which drives two NestedObjectPicker
/// drill-downs and source/target field lists from a seeded <see cref="DataMapperContext"/>.
/// Relocated from reference-ui's deep MapperPageTests; the page's <c>DataMapperProvider</c> is
/// swapped for a stub yielding the seeded context.
/// </summary>
[Trait("Category", "Ui")]
[Collection(PageHostCollection.Name)]
public sealed class MapperPageTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    private static DataStoreNode Node(string name, string kind = DataStoreNodeKind.Container) =>
        new(name, kind);

    private static DataStoreFieldPayload Field(string name, string type = "int") =>
        new() { Name = name, NativeDataType = type };

    private IRenderedComponent<MapperPage> RenderWith(DataMapperContext context)
    {
        _ctx.RegisterPageInfrastructure();
        _ctx.ComponentFactories.Add(new ProviderFactory<DataMapperProvider, DataMapperContext>(context));
        return _ctx.Render<MapperPage>();
    }

    [Fact]
    public void RendersErrorBannerWhenErrorMessagePresent()
    {
        var cut = RenderWith(new DataMapperContext { LastResult = GenericResult.Failure(new GenericMessage("mapper boom")) });
        cut.Markup.ShouldContain("mapper boom", Case.Sensitive);
        cut.Markup.ShouldContain("alert-err", Case.Sensitive);
    }

    [Fact]
    public void RendersLoadingSpinnerWhenLoadingSchema()
    {
        var cut = RenderWith(new DataMapperContext { IsLoadingSchema = true });
        cut.FindAll(".spin").ShouldNotBeEmpty();
    }

    [Fact]
    public void RendersDataStorePickerLabelsAndOptions()
    {
        var cut = RenderWith(new DataMapperContext
        {
            DataStorePickerItems = [Node("Src", DataStoreNodeKind.DataStore), Node("Tgt", DataStoreNodeKind.DataStore)],
        });
        cut.Markup.ShouldContain("Source DataStore", Case.Sensitive);
        cut.Markup.ShouldContain("Target DataStore", Case.Sensitive);
        cut.Markup.ShouldContain("Src", Case.Sensitive);
        cut.Markup.ShouldContain("Tgt", Case.Sensitive);
    }

    [Fact]
    public void RendersExactlyTwoTopLevelSelectsBeforeDrillDown()
    {
        var cut = RenderWith(new DataMapperContext
        {
            DataStorePickerItems = [Node("Src", DataStoreNodeKind.DataStore), Node("Tgt", DataStoreNodeKind.DataStore)],
        });
        cut.FindAll("select").Count.ShouldBe(2);
    }

    [Fact]
    public void RendersSourceAndTargetFields()
    {
        var cut = RenderWith(new DataMapperContext
        {
            SourceContainer = "src_ctr",
            TargetContainer = "tgt_ctr",
            SourceFields = [Field("src_id"), Field("src_name")],
            TargetFields = [Field("tgt_id"), Field("tgt_name")],
        });
        cut.Markup.ShouldContain("src_id", Case.Sensitive);
        cut.Markup.ShouldContain("src_name", Case.Sensitive);
        cut.Markup.ShouldContain("tgt_id", Case.Sensitive);
        cut.Markup.ShouldContain("tgt_name", Case.Sensitive);
    }

    [Fact]
    public void RendersEmptyFieldHintsWhenNoFields()
    {
        var cut = RenderWith(new DataMapperContext());
        cut.Markup.ShouldContain("Select a source to load fields", Case.Sensitive);
        cut.Markup.ShouldContain("Select a DataStore to load fields", Case.Sensitive);
    }

    [Fact]
    public void RendersEmptyMappingsHint()
    {
        var cut = RenderWith(new DataMapperContext());
        cut.Markup.ShouldContain("Select source and target fields to create mappings", Case.Sensitive);
    }

    [Fact]
    public void RendersMappingRowsAndMappedCount()
    {
        var cut = RenderWith(new DataMapperContext
        {
            Mappings = [new FieldMappingDto { SourceField = "src_id", TargetField = "tgt_id" }],
        });
        cut.Markup.ShouldContain("1 mapped", Case.Sensitive);
        cut.Markup.ShouldContain("src_id", Case.Sensitive);
        cut.Markup.ShouldContain("tgt_id", Case.Sensitive);
    }

    [Fact]
    public void MappedSourceFieldRendersReducedOpacity()
    {
        var cut = RenderWith(new DataMapperContext
        {
            SourceContainer = "src_ctr",
            SourceFields = [Field("src_id")],
            Mappings = [new FieldMappingDto { SourceField = "src_id", TargetField = "tgt_id" }],
        });
        cut.Markup.ShouldContain("opacity:.5;", Case.Sensitive);
    }

    [Fact]
    public void SaveDisabledWhenNoMappings()
    {
        var cut = RenderWith(new DataMapperContext());
        cut.FindAll("button").First(b => b.TextContent.Contains("Save Mappings", StringComparison.Ordinal))
            .HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public void SaveEnabledWhenMappingsPresent()
    {
        var cut = RenderWith(new DataMapperContext
        {
            Mappings = [new FieldMappingDto { SourceField = "src_id", TargetField = "tgt_id" }],
        });
        cut.FindAll("button").First(b => b.TextContent.Contains("Save Mappings", StringComparison.Ordinal))
            .HasAttribute("disabled").ShouldBeFalse();
    }

    [Fact]
    public void AutoMapButtonInvokesOnAutoMap()
    {
        var fired = false;
        var cut = RenderWith(new DataMapperContext
        {
            OnAutoMap = () => { fired = true; return Task.CompletedTask; },
        });
        cut.FindAll("button").First(b => b.TextContent.Contains("Auto-Map", StringComparison.Ordinal)).Click();
        fired.ShouldBeTrue();
    }

    [Fact]
    public void ValidateButtonInvokesOnValidate()
    {
        var fired = false;
        var cut = RenderWith(new DataMapperContext
        {
            OnValidate = () => { fired = true; return Task.CompletedTask; },
        });
        cut.FindAll("button").First(b => b.TextContent.Contains("Validate", StringComparison.Ordinal)).Click();
        fired.ShouldBeTrue();
    }

    [Fact]
    public void SaveButtonInvokesOnSave()
    {
        var fired = false;
        var cut = RenderWith(new DataMapperContext
        {
            Mappings = [new FieldMappingDto { SourceField = "src_id", TargetField = "tgt_id" }],
            OnSave = () => { fired = true; return Task.CompletedTask; },
        });
        cut.FindAll("button").First(b => b.TextContent.Contains("Save Mappings", StringComparison.Ordinal)).Click();
        fired.ShouldBeTrue();
    }

    [Fact]
    public void ClickingSourceThenTargetFieldInvokesOnMappingsChanged()
    {
        IReadOnlyList<FieldMappingDto>? captured = null;
        var cut = RenderWith(new DataMapperContext
        {
            SourceContainer = "src_ctr",
            TargetContainer = "tgt_ctr",
            SourceFields = [Field("src_id")],
            TargetFields = [Field("tgt_id")],
            OnMappingsChanged = m => { captured = m; return Task.CompletedTask; },
        });
        cut.FindAll("button").First(b => b.TextContent.Contains("src_id", StringComparison.Ordinal)).Click();
        cut.FindAll("button").First(b => b.TextContent.Contains("tgt_id", StringComparison.Ordinal)).Click();

        captured.ShouldNotBeNull();
        captured.Count.ShouldBe(1);
        captured[0].SourceField.ShouldBe("src_id");
        captured[0].TargetField.ShouldBe("tgt_id");
    }

    public void Dispose() => _ctx.Dispose();
}
