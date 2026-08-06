using Bunit;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Components.Pipelines;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Fdw.UI.Components.Blazor.Tests.Providers;

/// <summary>
/// Tests for <see cref="PipelineProvider"/> headless component.
/// Mocks IPipelineClient and IPipelineJobClient directly because PipelineProvider
/// accepts these interfaces via DI injection (not HttpClientFactory).
/// </summary>
[Trait("Category", "Ui")]
public sealed class PipelineProviderTests : IDisposable
{
    private readonly BunitContext _ctx;

    public PipelineProviderTests()
    {
        _ctx = new BunitContext();
    }

    private IRenderedComponent<PipelineProvider> RenderWithMocks(
        Mock<IPipelineClient> pipelineMock,
        Mock<IPipelineJobClient>? jobMock = null)
    {
        jobMock ??= new Mock<IPipelineJobClient>();

        _ctx.Services.AddSingleton(pipelineMock.Object);
        _ctx.Services.AddSingleton(jobMock.Object);
        _ctx.Services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        _ctx.Services.AddSingleton<ILogger<PipelineProvider>>(NullLogger<PipelineProvider>.Instance);

        return _ctx.Render<PipelineProvider>();
    }

    private static PipelineContext GetContext(IRenderedComponent<PipelineProvider> component)
    {
        var field = typeof(PipelineProvider).GetField("_context",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (PipelineContext)field!.GetValue(component.Instance)!;
    }

    // ── Load Tests ──────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnLoadData_LoadsPipelines()
    {
        var items = new List<PipelineSummaryResponse>
        {
            new() { Name = "DailySales", PipelineType = "BatchCopy" },
            new() { Name = "HourlySync", PipelineType = "BatchCopy" }
        };

        var pipelineMock = new Mock<IPipelineClient>();
        pipelineMock
            .Setup(c => c.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<PipelineSummaryResponse>>.Success(items));

        var component = RenderWithMocks(pipelineMock);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadData();
        });

        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
        resultCtx.Pipelines.Count.ShouldBe(2);
        resultCtx.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnLoadData_EmptyList_ReturnsEmpty()
    {
        var pipelineMock = new Mock<IPipelineClient>();
        pipelineMock
            .Setup(c => c.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<PipelineSummaryResponse>>.Success(
                new List<PipelineSummaryResponse>()));

        var component = RenderWithMocks(pipelineMock);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadData();
        });

        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
        resultCtx.Pipelines.Count.ShouldBe(0);
        resultCtx.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnLoadData_ApiFailure_SetsErrorMessage()
    {
        var pipelineMock = new Mock<IPipelineClient>();
        pipelineMock
            .Setup(c => c.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<PipelineSummaryResponse>>.Failure(new GenericMessage("load failed")));

        var component = RenderWithMocks(pipelineMock);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadData();
        });

        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
        resultCtx.Pipelines.Count.ShouldBe(0);
        resultCtx.ErrorMessage.ShouldNotBeNullOrEmpty();
    }

    // ── Detail Tests ────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnGetPipelineDetails_Success_ReturnsDetail()
    {
        var detail = new PipelineDetailResponse { Name = "DailySales" };

        var pipelineMock = new Mock<IPipelineClient>();
        pipelineMock
            .Setup(c => c.Get("DailySales", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<PipelineDetailResponse>.Success(detail));

        var component = RenderWithMocks(pipelineMock);

        PipelineDetailResponse? result = null;
        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            result = await ctx.OnGetPipelineDetails("DailySales");
        });

        result.ShouldNotBeNull();
        result!.Name.ShouldBe("DailySales");
    }

    public void Dispose() => _ctx.Dispose();
}
