using Bunit;
using Fdw.Services.SecretManagers.Clients.Models;
using Fdw.Services.SecretManagers.Components.SecretManagers;
using Fdw.UI.Components.Blazor.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Fdw.UI.Components.Blazor.Tests.Providers;

/// <summary>
/// Tests for <see cref="SecretManagerProvider"/> headless component.
/// Uses MockHttpHandler because SecretManagerApiClient is created internally via IHttpClientFactory.
/// </summary>
[Trait("Category", "Ui")]
public sealed class SecretManagerProviderTests : IDisposable
{
    private readonly BunitContext _ctx;

    public SecretManagerProviderTests()
    {
        _ctx = new BunitContext();
    }

    private IRenderedComponent<SecretManagerProvider> RenderWithHandler(MockHttpHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        _ctx.Services.AddSingleton(factoryMock.Object);
        _ctx.Services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        _ctx.Services.AddSingleton<ILogger<SecretManagerProvider>>(NullLogger<SecretManagerProvider>.Instance);

        return _ctx.Render<SecretManagerProvider>();
    }

    private static SecretManagerContext GetContext(IRenderedComponent<SecretManagerProvider> component)
    {
        var field = typeof(SecretManagerProvider).GetField("_context",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (SecretManagerContext)field!.GetValue(component.Instance)!;
    }

    // ── Load Tests ──────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnRefresh_LoadsSecretManagers()
    {
        var items = new List<SecretManagerSummaryPayload>
        {
            new() { Name = "AzureKeyVault", SecretManagerType = "AzureKeyVault" },
            new() { Name = "EnvVars", SecretManagerType = "EnvironmentVariable" }
        };

        var handler = new MockHttpHandler()
            .RespondWith("secret-managers", items);

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnRefresh();
        });

        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
        resultCtx.SecretManagers.Count.ShouldBe(2);
        resultCtx.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task OnRefresh_ApiFailure_SetsErrorMessage()
    {
        var handler = new MockHttpHandler()
            .RespondError("secret-managers");

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnRefresh();
        });

        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
        resultCtx.SecretManagers.Count.ShouldBe(0);
        resultCtx.ErrorMessage.ShouldNotBeNullOrEmpty();
    }

    // ── Select Tests ────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnSelect_LoadsDetail()
    {
        var detail = new SecretManagerDetailPayload { Name = "AzureKeyVault" };

        var handler = new MockHttpHandler()
            .RespondWith("secret-managers/AzureKeyVault", detail)
            .RespondWith("secret-managers", new List<SecretManagerSummaryPayload>());

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnSelect("AzureKeyVault");
        });

        var resultCtx = GetContext(component);
        resultCtx.SelectedManager.ShouldNotBeNull();
        resultCtx.SelectedManager!.Name.ShouldBe("AzureKeyVault", StringCompareShould.IgnoreCase);
    }

    // ── Create Tests ────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnCreate_Success_RefreshesList()
    {
        var created = new SecretManagerDetailPayload { Name = "NewManager" };
        var refreshed = new List<SecretManagerSummaryPayload>
        {
            new() { Name = "NewManager" }
        };

        var handler = new MockHttpHandler()
            .RespondWith("secret-managers", refreshed);

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnCreate(new CreateSecretManagerPayload { Name = "NewManager", SecretManagerType = "EnvVar" });
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
            .RespondOk("secret-managers/OldManager")
            .RespondWith("secret-managers", new List<SecretManagerSummaryPayload>());

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnDelete("OldManager");
        });

        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
        resultCtx.SecretManagers.Count.ShouldBe(0);
    }

    public void Dispose() => _ctx.Dispose();
}
