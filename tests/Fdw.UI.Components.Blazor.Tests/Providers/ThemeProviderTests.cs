using Bunit;
using Fdw.UI.Themes.Clients.Models;
using Fdw.UI.Themes.Components.Themes;
using Fdw.UI.Components.Blazor.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Fdw.UI.Components.Blazor.Tests.Providers;

/// <summary>
/// Tests for <see cref="ThemeProvider"/> headless component.
/// Uses MockHttpHandler because ThemeApiClient is created internally via IHttpClientFactory.
/// </summary>
[Trait("Category", "Ui")]
public sealed class ThemeProviderTests : IDisposable
{
    private readonly BunitContext _ctx;

    public ThemeProviderTests()
    {
        _ctx = new BunitContext();
    }

    private IRenderedComponent<ThemeProvider> RenderWithHandler(MockHttpHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        _ctx.Services.AddSingleton(factoryMock.Object);
        _ctx.Services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        _ctx.Services.AddSingleton<ILogger<ThemeProvider>>(NullLogger<ThemeProvider>.Instance);

        return _ctx.Render<ThemeProvider>();
    }

    private static ThemeContext GetContext(IRenderedComponent<ThemeProvider> component)
    {
        var field = typeof(ThemeProvider).GetField("_context",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (ThemeContext)field!.GetValue(component.Instance)!;
    }

    // ── Load Tests ──────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnLoadThemes_LoadsItems()
    {
        var items = new List<ThemeSummaryPayload>
        {
            new() { Name = "DarkBlue", IsDarkMode = true },
            new() { Name = "LightGray", IsDarkMode = false }
        };

        var handler = new MockHttpHandler()
            .RespondWith("themes", items);

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadThemes();
        });

        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
        resultCtx.Themes.Count.ShouldBe(2);
        resultCtx.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnLoadThemes_EmptyList_ReturnsEmpty()
    {
        var handler = new MockHttpHandler()
            .RespondWith("themes", new List<ThemeSummaryPayload>());

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadThemes();
        });

        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
        resultCtx.Themes.Count.ShouldBe(0);
        resultCtx.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnLoadThemes_ApiFailure_SetsErrorMessage()
    {
        var handler = new MockHttpHandler()
            .RespondError("themes");

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadThemes();
        });

        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
        resultCtx.Themes.Count.ShouldBe(0);
        resultCtx.ErrorMessage.ShouldNotBeNullOrEmpty();
    }

    // ── Default Theme Tests ──────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnLoadDefaultTheme_LoadsCurrentTheme()
    {
        var defaultTheme = new ThemeConfiguration { Name = "DarkBlue" };

        var handler = new MockHttpHandler()
            .RespondWith("themes/default", defaultTheme);

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadDefaultTheme();
        });

        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
        resultCtx.CurrentTheme.ShouldNotBeNull();
        resultCtx.CurrentTheme!.Name.ShouldBe("DarkBlue");
    }

    // ── Delete Tests ────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnDeleteTheme_Success_RemovesFromList()
    {
        var items = new List<ThemeSummaryPayload>
        {
            new() { Name = "OldTheme" }
        };

        // Why: Load themes first so list is populated, then delete removes the entry locally.
        var handler = new MockHttpHandler()
            .RespondWith("themes", items)
            .RespondOk("themes/OldTheme");

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadThemes();
        });

        bool deleted = false;
        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            deleted = await ctx.OnDeleteTheme("OldTheme");
        });

        deleted.ShouldBeTrue();
        var resultCtx = GetContext(component);
        resultCtx.Themes.ShouldBeEmpty();
    }

    public void Dispose() => _ctx.Dispose();
}
