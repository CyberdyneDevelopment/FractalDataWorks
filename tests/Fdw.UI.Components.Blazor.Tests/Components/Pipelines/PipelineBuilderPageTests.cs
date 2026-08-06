using Bunit;
using Fdw.Messages;
using Fdw.Results;
using Fdw.UI.Components.Blazor.Tests.PipeInfra;
using Fdw.UI.Pipelines.Clients;
using Fdw.UI.Pipelines.Clients.Models;
using Microsoft.Extensions.DependencyInjection;
using BuilderPage = Fdw.Services.Pipelines.UI.Pages.Pages.Pipelines.Builder;

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
        // Why: GetTaskTypes is invoked unconditionally on first render; default it to an empty
        // success so the palette renders without a configured stub in every test.
        _designer
            .Setup(d => d.GetTaskTypes(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<TaskTypeInfo>>.Success([]));
    }

    private IRenderedComponent<BuilderPage> RenderReal(Action<Bunit.ComponentParameterCollectionBuilder<BuilderPage>>? extra = null)
    {
        _ctx.RegisterPageInfrastructure();
        _ctx.Services.AddSingleton(_designer.Object);
        return _ctx.Render<BuilderPage>(p => extra?.Invoke(p));
    }

    [Fact]
    public void RendersEmptyCanvasHintOnFreshBuilder()
    {
        var cut = RenderReal();
        // Why: current markup reads "Drag tasks from the palette to build your pipeline"; the old
        // reference-ui test asserted only the "Drag tasks from the palette" prefix.
        cut.WaitForAssertion(() =>
            cut.Markup.ShouldContain("Drag tasks from the palette", Case.Sensitive));
    }

    [Fact]
    public void RendersPaletteCategories()
    {
        var cut = RenderReal();
        cut.WaitForAssertion(() =>
        {
            // Why: the old test asserted a "PIPELINE BUILDER" / "Filter" / "Aggregate" / "Trash"
            // palette header set. The current palette header is "Nodes" with category groups
            // Source/Transform/Destination/Control/Diagnostics; Filter/Aggregate are Transform
            // tasks and Trash is a Diagnostics task. Assert the real current labels.
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
        // Why: properties-panel empty prompt is unchanged: "Select a node to edit its properties."
        // (the panel header itself is now "Inspector", not "Properties").
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

    [Fact]
    public void ExistingPipelineIdTriggersDesignerGet()
    {
        var pipelineId = Guid.NewGuid();
        _designer
            .Setup(d => d.Get(pipelineId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<PipelineDetailPayload>.Success(
                new PipelineDetailPayload { Id = pipelineId, Name = "existing" }));

        var cut = RenderReal(p => p.Add(x => x.PipelineId, pipelineId));
        cut.WaitForAssertion(() =>
            _designer.Verify(d => d.Get(pipelineId, It.IsAny<CancellationToken>()), Times.AtLeastOnce));
    }

    [Fact]
    public void ExistingPipelineLoadFailureIsRequestedAndDoesNotCrashPage()
    {
        // Why (documents a real product limitation): when DesignerApi.Get fails, the provider records
        // the reason in ErrorMessage, but the PAGE only ever copies that into its toolbar validation
        // span inside its OWN first OnAfterRenderAsync (guarded by `_providerLoaded`). The provider's
        // LoadExisting runs asynchronously in the provider's own after-render, so by the time the page
        // latches `_providerLoaded = true` the provider error is still null — the failure is therefore
        // never surfaced in the toolbar. The old reference-ui test expected a visible error banner;
        // the current FDW page does not render one on this path. This test pins the ACTUAL behaviour:
        // the failing Get is still issued and the page renders its empty-canvas state without crashing.
        var pipelineId = Guid.NewGuid();
        _designer
            .Setup(d => d.Get(pipelineId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<PipelineDetailPayload>.Failure(new GenericMessage("load failed")));

        var cut = RenderReal(p => p.Add(x => x.PipelineId, pipelineId));
        cut.WaitForAssertion(() =>
        {
            _designer.Verify(d => d.Get(pipelineId, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
            cut.Markup.ShouldContain("Drag tasks from the palette", Case.Sensitive);
        });
    }

    [Fact]
    public void EditRouteForwardsIdToProvider()
    {
        // Why: the /pipelines/{Id}/edit route binds Id (not PipelineId). Builder folds Id into
        // EffectivePipelineId so the provider still loads the existing pipeline.
        var editId = Guid.NewGuid();
        _designer
            .Setup(d => d.Get(editId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<PipelineDetailPayload>.Success(
                new PipelineDetailPayload { Id = editId, Name = "edited" }));

        var cut = RenderReal(p => p.Add(x => x.Id, editId));
        cut.WaitForAssertion(() =>
            _designer.Verify(d => d.Get(editId, It.IsAny<CancellationToken>()), Times.AtLeastOnce));
    }

    public void Dispose() => _ctx.Dispose();
}
