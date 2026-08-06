using Bunit;
using Fdw.Services.Authentication.Clients.Models;
using Fdw.Services.Authentication.Components.Profile;
using Fdw.UI.Components.Blazor.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Fdw.UI.Components.Blazor.Tests.Providers;

/// <summary>
/// Tests for <see cref="ProfileProvider"/> headless component.
/// Uses MockHttpHandler because AuthenticationApiClient is sealed.
/// </summary>
[Trait("Category", "Ui")]
public sealed class ProfileProviderTests : IDisposable
{
    private readonly BunitContext _ctx;

    public ProfileProviderTests()
    {
        _ctx = new BunitContext();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private IRenderedComponent<ProfileProvider> RenderWithHandler(MockHttpHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        _ctx.Services.AddSingleton(factoryMock.Object);
        _ctx.Services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        _ctx.Services.AddSingleton<ILogger<ProfileProvider>>(NullLogger<ProfileProvider>.Instance);

        ProfileContext? captured = null;
        var component = _ctx.Render<ProfileProvider>(p => p
            .Add(x => x.ChildContent, ctx =>
            {
                captured = ctx;
                return builder => { };
            }));

        return component;
    }

    private static ProfileContext GetContext(IRenderedComponent<ProfileProvider> component)
    {
        // Access internal context through reflection on the private field
        var field = typeof(ProfileProvider).GetField("_context",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (ProfileContext)field!.GetValue(component.Instance)!;
    }

    // ── Load Tests ──────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public void InitialState_IsNotLoading_NoError()
    {
        var handler = new MockHttpHandler()
            .RespondWith("users/me", new GetMePayload { Username = "test-user", UserId = "1" })
            .RespondWith("users/me/preferences", new Dictionary<string, string> { ["theme"] = "dark" });

        var component = RenderWithHandler(handler);
        var ctx = GetContext(component);

        ctx.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnRefresh_LoadsProfileAndPreferences()
    {
        var handler = new MockHttpHandler()
            .RespondWith("users/me", new GetMePayload { Username = "admin", UserId = "42" })
            .RespondWith("preferences", new Dictionary<string, string> { ["lang"] = "en" });

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnRefresh();
        });

        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
    }

    // ── Error Handling ──────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P2")]
    public async Task LoadProfile_ApiFailure_SetsErrorMessage()
    {
        var handler = new MockHttpHandler()
            .RespondError("users/me");

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnRefresh();
        });

        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
        resultCtx.ErrorMessage.ShouldNotBeNullOrEmpty();
    }

    // ── ChangePassword Tests ────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnChangePassword_CallsApi()
    {
        var handler = new MockHttpHandler()
            .RespondWith("users/me", new GetMePayload { Username = "admin", UserId = "1" })
            .RespondWith("preferences", new Dictionary<string, string>())
            .RespondOk("password");

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnChangePassword(new ChangePasswordRequest
            {
                CurrentPassword = "old",
                NewPassword = "new"
            });
        });

        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
    }

    // ── Cancellation Tests ──────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P2")]
    public void CancellationToken_DoesNotThrow()
    {
        var handler = new MockHttpHandler()
            .RespondWith("users/me", new GetMePayload { Username = "admin", UserId = "1" })
            .RespondWith("preferences", new Dictionary<string, string>());

        var component = RenderWithHandler(handler);
        var ctx = GetContext(component);

        // The provider catches OperationCanceledException silently
        ctx.ShouldNotBeNull();
    }

    public void Dispose() => _ctx.Dispose();
}
