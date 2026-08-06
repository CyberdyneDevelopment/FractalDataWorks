using Bunit;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Scheduling.Clients.Abstractions;
using Fdw.Services.Scheduling.Components.Schedules;
using Fdw.UI.Components.Blazor.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Fdw.UI.Components.Blazor.Tests.Providers;

/// <summary>
/// Tests for <see cref="ScheduleProvider"/> headless component.
/// Mocks IScheduleClient directly (injected interface) and uses MockHttpHandler
/// for ConfigurationApiClient (created via IHttpClientFactory).
/// </summary>
[Trait("Category", "Ui")]
public sealed class ScheduleProviderTests : IDisposable
{
    private readonly BunitContext _ctx;

    public ScheduleProviderTests()
    {
        _ctx = new BunitContext();
    }

    private IRenderedComponent<ScheduleProvider> RenderWithMocks(
        Mock<IScheduleClient> scheduleMock,
        MockHttpHandler? configHandler = null)
    {
        // Why: ScheduleProvider also creates a ConfigurationApiClient via IHttpClientFactory
        // to load schedule types. We provide a handler that returns an empty list to prevent errors.
        configHandler ??= new MockHttpHandler()
            .RespondWith("configuration/types", new List<object>());

        var httpClient = new HttpClient(configHandler) { BaseAddress = new Uri("http://localhost/") };
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        _ctx.Services.AddSingleton(scheduleMock.Object);
        _ctx.Services.AddSingleton(factoryMock.Object);
        _ctx.Services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        _ctx.Services.AddSingleton<ILogger<ScheduleProvider>>(NullLogger<ScheduleProvider>.Instance);

        return _ctx.Render<ScheduleProvider>();
    }

    private static ScheduleContext GetContext(IRenderedComponent<ScheduleProvider> component)
    {
        var field = typeof(ScheduleProvider).GetField("_context",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (ScheduleContext)field!.GetValue(component.Instance)!;
    }

    // ── Load Tests ──────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnLoadData_LoadsSchedules()
    {
        var items = new List<ScheduleInfoDto>
        {
            new() { Name = "DailyRun", PipelineName = "DailySales", IsEnabled = true },
            new() { Name = "HourlySync", PipelineName = "HourlySync", IsEnabled = false }
        };

        var scheduleMock = new Mock<IScheduleClient>();
        scheduleMock
            .Setup(c => c.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<ScheduleInfoDto>>.Success(items));

        var component = RenderWithMocks(scheduleMock);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadData();
        });

        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
        resultCtx.Schedules.Count.ShouldBe(2);
        resultCtx.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnLoadData_EmptyList_ReturnsEmpty()
    {
        var scheduleMock = new Mock<IScheduleClient>();
        scheduleMock
            .Setup(c => c.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<ScheduleInfoDto>>.Success(
                new List<ScheduleInfoDto>()));

        var component = RenderWithMocks(scheduleMock);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadData();
        });

        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
        resultCtx.Schedules.Count.ShouldBe(0);
        resultCtx.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnLoadData_ApiFailure_SetsErrorMessage()
    {
        var scheduleMock = new Mock<IScheduleClient>();
        scheduleMock
            .Setup(c => c.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<ScheduleInfoDto>>.Failure(new GenericMessage("load failed")));

        var component = RenderWithMocks(scheduleMock);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadData();
        });

        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
        resultCtx.Schedules.Count.ShouldBe(0);
        resultCtx.ErrorMessage.ShouldNotBeNullOrEmpty();
    }

    // ── Toggle Tests ────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnToggleSchedule_Success_ReturnsTrue()
    {
        var schedules = new List<ScheduleInfoDto>
        {
            new() { Name = "DailyRun", PipelineName = "DailySales", IsEnabled = false }
        };

        var scheduleMock = new Mock<IScheduleClient>();
        scheduleMock
            .Setup(c => c.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<ScheduleInfoDto>>.Success(schedules));
        scheduleMock
            .Setup(c => c.UpdateSchedule("DailyRun", It.IsAny<UpdateScheduleClientRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());

        var component = RenderWithMocks(scheduleMock);

        // Load first so the schedule list is populated
        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadData();
        });

        bool result = false;
        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            result = await ctx.OnToggleSchedule("DailyRun", true);
        });

        result.ShouldBeTrue();
    }

    // ── Delete Tests ────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnDeleteSchedule_Success_RefreshesList()
    {
        var scheduleMock = new Mock<IScheduleClient>();
        scheduleMock
            .Setup(c => c.DeleteSchedule("DailyRun", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());
        scheduleMock
            .Setup(c => c.List(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<ScheduleInfoDto>>.Success(
                new List<ScheduleInfoDto>()));

        var component = RenderWithMocks(scheduleMock);

        bool deleted = false;
        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            deleted = await ctx.OnDeleteSchedule("DailyRun");
        });

        deleted.ShouldBeTrue();
        var resultCtx = GetContext(component);
        resultCtx.Schedules.Count.ShouldBe(0);
    }

    public void Dispose() => _ctx.Dispose();
}
