using Bunit;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.UI.Components.Blazor.Tests.PipeInfra;
using Fdw.UI.Pipelines.Clients;
using Fdw.UI.Pipelines.Clients.Models;
using Microsoft.Extensions.DependencyInjection;
using BuilderPage = Fdw.UI.Pages.Pipelines.Pages.Pipelines.PipelineBuilderPage;

namespace Fdw.UI.Components.Blazor.Tests.Components.Pipelines;

/// <summary>
/// Component tests for the pipelines <see cref="BuilderPage"/> (Builder.razor). Relocated from
/// reference-ui's PipelineBuilderTests. Builder captures its <c>PipelineBuilderProvider</c> through a
/// typed <c>@@ref</c>, so a provider-stub swap throws InvalidCastException; the REAL provider is
/// rendered with a Moq-injected <see cref="IPipelineDesignerClient"/>. The provider also constructs
/// HTTP-backed Configuration / DataSet clients in OnInitialized — <c>RegisterPageInfrastructure</c>
/// supplies the no-op <see cref="IHttpClientFactory"/> those need.
/// </summary>
[Trait("Category", "Ui")]
[Collection(PageHostCollection.Name)]
public sealed class PipelineBuilderPageTests : IDisposable
{
    private readonly BunitContext _ctx = new();
    private readonly Mock<IPipelineDesignerClient> _designer = new();

    public PipelineBuilderPageTests()
    {
        _designer
            .Setup(d => d.GetTaskTypes(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<TaskTypeInfo>>.Success([]));
    }

    private IRenderedComponent<BuilderPage> RenderReal(Action<Bunit.ComponentParameterCollectionBuilder<BuilderPage>>? extra = null)
    {
        _ctx.RegisterPageInfrastructure();
        _ctx.Services.AddSingleton(_designer.Object);
        var pipelineApi = new Mock<IPipelineClient>();
        pipelineApi.Setup(c => c.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<PipelineSummaryResponse>>.Success([]));
        pipelineApi.Setup(c => c.GetPipelineTypes(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<PipelineTypeSummary>>.Success([]));
        _ctx.Services.AddSingleton(pipelineApi.Object);
        return _ctx.Render<BuilderPage>(p => extra?.Invoke(p));
    }

    [Fact]
    public void FreshBuilderRendersABlankCanvasRatherThanTheDragHint()
    {
        var cut = RenderReal();

        cut.WaitForAssertion(() => cut.FindAll(".canvas").Count.ShouldBe(1));
        cut.Markup.ShouldNotContain("Drag tasks from the palette", Case.Sensitive);
    }

    [Fact]
    public void RendersPaletteCategories()
    {
        var cut = RenderReal();
        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("Nodes");
            cut.Markup.ShouldContain("Source");
            cut.Markup.ShouldContain("Transform");
            cut.Markup.ShouldContain("Filter");
            cut.Markup.ShouldContain("Aggregate");
            cut.Markup.ShouldContain("Trash");
        });
    }

    [Fact]
    public void RendersEmptyInspectorPrompt()
    {
        var cut = RenderReal();
        cut.WaitForAssertion(() =>
            cut.Markup.ShouldContain("Select a node to edit its properties."));
    }

    [Fact]
    public void UndoAndRedoDisabledOnFreshCanvas()
    {
        var cut = RenderReal();
        cut.WaitForAssertion(() =>
        {
            var undo = cut.FindAll("button").First(b => b.TextContent.Contains("Undo", StringComparison.Ordinal));
            var redo = cut.FindAll("button").First(b => b.TextContent.Contains("Redo", StringComparison.Ordinal));
            undo.HasAttribute("disabled").ShouldBeTrue();
            redo.HasAttribute("disabled").ShouldBeTrue();
        });
    }

    [Fact]
    public void PublishDisabledUntilPipelineSaved()
    {
        var cut = RenderReal();
        cut.WaitForAssertion(() =>
        {
            var publish = cut.FindAll("button").First(b => b.TextContent.Contains("Publish", StringComparison.Ordinal));
            publish.HasAttribute("disabled").ShouldBeTrue();
        });
    }

    [Fact]
    public void TestOnUnsavedPipelineSurfacesSaveFirstMessage()
    {
        var cut = RenderReal();
        cut.WaitForAssertion(() =>
            cut.FindAll("button").ShouldContain(b => b.GetAttribute("title") == "Run pipeline in test mode (bounded, no destination writes)"));
        cut.FindAll("button")
            .First(b => b.GetAttribute("title") == "Run pipeline in test mode (bounded, no destination writes)")
            .Click();
        cut.WaitForAssertion(() =>
            cut.Markup.ShouldContain("Save the pipeline before running a test"));
    }

    public void Dispose() => _ctx.Dispose();
}
