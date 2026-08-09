using Bunit;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.UI.Components.Blazor.Tests.PipeInfra;
using Microsoft.Extensions.DependencyInjection;
using IndexPage = Fdw.UI.Pages.Pipelines.Pages.Pipelines.PipelinesIndexPage;

namespace Fdw.UI.Components.Blazor.Tests.Components.Pipelines;

/// <summary>
/// Component tests for the pipelines <see cref="IndexPage"/> (Index.razor). Relocated from
/// reference-ui's PipelinesPageTests. Unlike provider-swap pages, Index captures its
/// <c>PipelineProvider</c> through a typed <c>@@ref</c>, so swapping the provider for a stub throws
/// InvalidCastException. Instead the REAL <c>PipelineProvider</c> is rendered with Moq-injected
/// <see cref="IPipelineClient"/> / <see cref="IPipelineJobClient"/> returning seeded data, and
/// async settling is awaited via <c>WaitForAssertion</c>.
/// </summary>
[Trait("Category", "Ui")]
[Collection(PageHostCollection.Name)]
public sealed class PipelineIndexPageTests : IDisposable
{
    private readonly BunitContext _ctx = new();
    private readonly Mock<IPipelineClient> _pipelineClient = new();
    private readonly Mock<IPipelineJobClient> _jobClient = new();

    private static PipelineSummaryResponse P(string name, string type = "BatchCopy") =>
        new() { Id = Guid.NewGuid(), Name = name, PipelineType = type };

    private void SeedList(IReadOnlyList<PipelineSummaryResponse> pipelines) =>
        _pipelineClient
            .Setup(c => c.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<PipelineSummaryResponse>>.Success(pipelines));

    private IRenderedComponent<IndexPage> RenderReal()
    {
        _ctx.RegisterPageInfrastructure();
        _ctx.Services.AddSingleton(_pipelineClient.Object);
        _ctx.Services.AddSingleton(_jobClient.Object);
        return _ctx.Render<IndexPage>();
    }

    [Fact]
    public void RendersEmptyStateWhenListReturnsNoPipelines()
    {
        SeedList([]);
        var cut = RenderReal();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("No pipelines configured"));
    }

    [Fact]
    public void RendersRowsAndCountWhenPipelinesPresent()
    {
        SeedList([P("nightly-load", "BatchCopy"), P("stream-sync", "Streaming")]);
        var cut = RenderReal();
        cut.WaitForAssertion(() =>
        {
            // Why: the old reference-ui test asserted a "Managed processes: N" eyebrow; the current
            // page renders the count in the "Pipelines & Scheduling · N" eyebrow instead.
            cut.Markup.ShouldContain("Pipelines &amp; Scheduling · 2", Case.Insensitive);
            cut.Markup.ShouldContain("nightly-load");
            cut.Markup.ShouldContain("stream-sync");
            cut.Markup.ShouldContain("BatchCopy");
            cut.Markup.ShouldContain("Streaming");
        });
    }

    [Fact]
    public void RendersEmptyStateWhenListReturnsFailure()
    {
        _pipelineClient
            .Setup(c => c.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<PipelineSummaryResponse>>.Failure(new GenericMessage("boom")));
        var cut = RenderReal();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("No pipelines configured"));
    }

    [Fact]
    public void RendersEmptyStateWhenListThrows()
    {
        _pipelineClient
            .Setup(c => c.List(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException());
        var cut = RenderReal();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("No pipelines configured"));
    }

    [Fact]
    public void NewPipelineLinkPointsToNewRoute()
    {
        SeedList([]);
        var cut = RenderReal();
        cut.WaitForAssertion(() =>
            cut.FindAll("a").ShouldContain(a => a.GetAttribute("href") == "/pipelines/new"));
    }

    [Fact]
    public void RowEditLinkPointsToBuilderRoute()
    {
        var pipeline = P("nightly-load");
        SeedList([pipeline]);
        var cut = RenderReal();
        cut.WaitForAssertion(() =>
            cut.FindAll("a").ShouldContain(a => a.GetAttribute("href") == $"/pipelines/builder/{pipeline.Id}"));
    }

    [Fact]
    public void ExecuteButtonTriggersJobWithPipelineNameAndUiSource()
    {
        SeedList([P("nightly-load")]);
        TriggerPipelineRequest? captured = null;
        _jobClient
            .Setup(c => c.Trigger(It.IsAny<TriggerPipelineRequest>(), It.IsAny<CancellationToken>()))
            .Callback<TriggerPipelineRequest, CancellationToken>((r, _) => captured = r)
            .ReturnsAsync(GenericResult<TriggerPipelineResponse>.Success(
                new TriggerPipelineResponse { ExecutionId = Guid.NewGuid(), Status = "Queued" }));

        var cut = RenderReal();
        cut.WaitForAssertion(() => cut.FindAll("button").ShouldContain(b => b.GetAttribute("title") == "Execute"));
        cut.FindAll("button").First(b => b.GetAttribute("title") == "Execute").Click();

        cut.WaitForAssertion(() =>
        {
            captured.ShouldNotBeNull();
            captured!.Name.ShouldBe("nightly-load");
            captured.TriggerSource.ShouldBe("UI");
        });
    }

    [Fact]
    public void ExecuteButtonDoesNotThrowWhenTriggerFails()
    {
        SeedList([P("nightly-load")]);
        _jobClient
            .Setup(c => c.Trigger(It.IsAny<TriggerPipelineRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<TriggerPipelineResponse>.Failure(new GenericMessage("nope")));

        var cut = RenderReal();
        cut.WaitForAssertion(() => cut.FindAll("button").ShouldContain(b => b.GetAttribute("title") == "Execute"));
        Should.NotThrow(() =>
            cut.FindAll("button").First(b => b.GetAttribute("title") == "Execute").Click());
    }

    public void Dispose() => _ctx.Dispose();
}
