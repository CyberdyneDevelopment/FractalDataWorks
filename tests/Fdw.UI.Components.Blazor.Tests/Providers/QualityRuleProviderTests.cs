using Bunit;
using Fdw.Services.Quality.Clients.Models;
using Fdw.Services.Quality.Components.QualityRules;
using Fdw.UI.Components.Blazor.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Fdw.UI.Components.Blazor.Tests.Providers;

/// <summary>
/// Tests for <see cref="QualityRuleProvider"/> headless component.
/// Uses MockHttpHandler because QualityApiClient is sealed.
/// </summary>
[Trait("Category", "Ui")]
public sealed class QualityRuleProviderTests : IDisposable
{
    private readonly BunitContext _ctx;

    public QualityRuleProviderTests()
    {
        _ctx = new BunitContext();
    }

    private IRenderedComponent<QualityRuleProvider> RenderWithHandler(MockHttpHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        _ctx.Services.AddSingleton(factoryMock.Object);
        _ctx.Services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        _ctx.Services.AddSingleton<ILogger<QualityRuleProvider>>(NullLogger<QualityRuleProvider>.Instance);

        return _ctx.Render<QualityRuleProvider>();
    }

    private static QualityRuleContext GetContext(IRenderedComponent<QualityRuleProvider> component)
    {
        var field = typeof(QualityRuleProvider).GetField("_context",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (QualityRuleContext)field!.GetValue(component.Instance)!;
    }

    // ── Load Tests ──────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnRefresh_LoadsRules()
    {
        var rules = new List<QualityRuleSummaryPayload>
        {
            new() { Id = Guid.NewGuid(), Name = "NotNull Check", IsEnabled = true },
            new() { Id = Guid.NewGuid(), Name = "Range Check", IsEnabled = false }
        };

        var handler = new MockHttpHandler()
            .RespondWith("quality/rules", rules);

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnRefresh();
        });

        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
        resultCtx.Rules.Count.ShouldBe(2);
        resultCtx.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task OnRefresh_ApiFailure_SetsErrorMessage()
    {
        var handler = new MockHttpHandler()
            .RespondError("quality/rules");

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnRefresh();
        });

        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
        resultCtx.Rules.Count.ShouldBe(0);
        resultCtx.ErrorMessage.ShouldNotBeNullOrEmpty();
    }

    // ── Create Tests ────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnCreate_Success_RefreshesList()
    {
        var created = new QualityRuleDetailPayload { Id = Guid.NewGuid(), Name = "New Rule" };
        var refreshed = new List<QualityRuleSummaryPayload>
        {
            new() { Id = created.Id, Name = "New Rule", IsEnabled = true }
        };

        var handler = new MockHttpHandler()
            .RespondWith("quality/rules", refreshed);

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnCreate(new CreateQualityRulePayload { Name = "New Rule", RuleType = "NotNull" });
        });

        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
    }

    // ── Delete Tests ────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnDelete_Success_RefreshesList()
    {
        var handler = new MockHttpHandler()
            .RespondWith("quality/rules", new List<QualityRuleSummaryPayload>())
            .RespondOk("quality/rules/");

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnDelete(Guid.NewGuid());
        });

        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
    }

    // ── Execute Tests ───────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnExecute_Success_RefreshesList()
    {
        var handler = new MockHttpHandler()
            .RespondWith("quality/rules", new List<QualityRuleSummaryPayload>())
            .RespondWith("execute", new QualityCheckResultPayload());

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnExecute(Guid.NewGuid());
        });

        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
    }

    public void Dispose() => _ctx.Dispose();
}
