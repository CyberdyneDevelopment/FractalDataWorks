using Fdw.UI.Themes.Clients.ApiClients;
using Fdw.Services.Settings.Clients;
using Bunit;
using Fdw.Services.Settings.Clients.Models;
using Fdw.UI.Themes.Clients.Models;
using Fdw.Services.Settings.Components.Settings;
using Fdw.UI.Components.Blazor.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Fdw.UI.Components.Blazor.Tests.Providers;

/// <summary>
/// Tests for <see cref="SettingsProvider"/> headless component.
/// Uses MockHttpHandler behind the typed clients the provider injects. SettingsProvider takes
/// SettingsApiClient and ThemeApiClient from DI rather than building them from an
/// IHttpClientFactory, because their client service types already register them.
/// </summary>
[Trait("Category", "Ui")]
public sealed class SettingsProviderTests : IDisposable
{
    private readonly BunitContext _ctx;

    public SettingsProviderTests()
    {
        _ctx = new BunitContext();
    }

    private IRenderedComponent<SettingsProvider> RenderWithHandler(MockHttpHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        _ctx.Services.AddSingleton(factoryMock.Object);
        _ctx.Services.AddSingleton(new SettingsApiClient(httpClient, NullLogger<SettingsApiClient>.Instance));
        _ctx.Services.AddSingleton(new ThemeApiClient(httpClient, NullLogger<ThemeApiClient>.Instance));
        _ctx.Services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        _ctx.Services.AddSingleton<ILogger<SettingsProvider>>(NullLogger<SettingsProvider>.Instance);

        return _ctx.Render<SettingsProvider>();
    }

    private static SettingsContext GetContext(IRenderedComponent<SettingsProvider> component)
    {
        var field = typeof(SettingsProvider).GetField("_context",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (SettingsContext)field!.GetValue(component.Instance)!;
    }

    // ── Load Tests ──────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnLoadSettings_LoadsItems()
    {
        var settings = new List<ServerSettingResponse>
        {
            new() { SettingName = "MaxConnections", SettingValue = "100", DataType = "int" },
            new() { SettingName = "DefaultTimeout", SettingValue = "30", DataType = "int" }
        };

        var handler = new MockHttpHandler()
            .RespondWith("settings/server", settings)
            .RespondWith("themes", new List<ThemeSummaryPayload>());

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadSettings();
        });

        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
        resultCtx.Settings.Count.ShouldBe(2);
        resultCtx.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnLoadSettings_EmptyList_ReturnsEmpty()
    {
        var handler = new MockHttpHandler()
            .RespondWith("settings/server", new List<ServerSettingResponse>())
            .RespondWith("themes", new List<ThemeSummaryPayload>());

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadSettings();
        });

        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
        resultCtx.Settings.Count.ShouldBe(0);
        resultCtx.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnLoadSettings_ApiFailure_SetsErrorMessage()
    {
        var handler = new MockHttpHandler()
            .RespondError("settings/server")
            .RespondWith("themes", new List<ThemeSummaryPayload>());

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadSettings();
        });

        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
        resultCtx.Settings.Count.ShouldBe(0);
        resultCtx.ErrorMessage.ShouldNotBeNullOrEmpty();
    }

    // ── Theme Load Tests ─────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnLoadThemes_LoadsAvailableThemes()
    {
        var themes = new List<ThemeSummaryPayload>
        {
            new() { Name = "DarkBlue", IsDefault = true },
            new() { Name = "LightGray", IsDefault = false }
        };

        var handler = new MockHttpHandler()
            .RespondWith("themes", themes)
            .RespondWith("settings/server", new List<ServerSettingResponse>());

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadThemes();
        });

        var resultCtx = GetContext(component);
        resultCtx.Themes.Count.ShouldBe(2);
    }

    // ── GetSettingValue Tests ────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public async Task GetSettingValue_KnownSetting_ReturnsValue()
    {
        var settings = new List<ServerSettingResponse>
        {
            new() { SettingName = "MaxConnections", SettingValue = "50", DataType = "int" }
        };

        var handler = new MockHttpHandler()
            .RespondWith("settings/server", settings)
            .RespondWith("themes", new List<ThemeSummaryPayload>());

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadSettings();
        });

        var resultCtx = GetContext(component);
        var value = resultCtx.GetSettingValue("MaxConnections", "0");
        value.ShouldBe("50");
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task GetSettingValue_UnknownSetting_ReturnsDefault()
    {
        var handler = new MockHttpHandler()
            .RespondWith("settings/server", new List<ServerSettingResponse>())
            .RespondWith("themes", new List<ThemeSummaryPayload>());

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadSettings();
        });

        var resultCtx = GetContext(component);
        var value = resultCtx.GetSettingValue("NonExistentSetting", "fallback");
        value.ShouldBe("fallback");
    }

    public void Dispose() => _ctx.Dispose();
}
