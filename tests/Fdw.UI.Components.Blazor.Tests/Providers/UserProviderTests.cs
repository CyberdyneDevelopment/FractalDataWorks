using Bunit;
using Fdw.Services.Users.Clients.Models;
using Fdw.Services.Authorization.Components.Users;
using Fdw.UI.Components.Blazor.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Fdw.UI.Components.Blazor.Tests.Providers;

/// <summary>
/// Tests for <see cref="UserProvider"/> headless component.
/// Uses MockHttpHandler because UserApiClient is created internally via IHttpClientFactory.
/// </summary>
[Trait("Category", "Ui")]
public sealed class UserProviderTests : IDisposable
{
    private readonly BunitContext _ctx;

    public UserProviderTests()
    {
        _ctx = new BunitContext();
    }

    private IRenderedComponent<UserProvider> RenderWithHandler(MockHttpHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        _ctx.Services.AddSingleton(factoryMock.Object);
        _ctx.Services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        _ctx.Services.AddSingleton<ILogger<UserProvider>>(NullLogger<UserProvider>.Instance);

        return _ctx.Render<UserProvider>();
    }

    private static UserContext GetContext(IRenderedComponent<UserProvider> component)
    {
        var field = typeof(UserProvider).GetField("_context",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (UserContext)field!.GetValue(component.Instance)!;
    }

    // ── Load Tests ──────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnLoadUsers_LoadsItems()
    {
        var items = new List<UserSummaryPayload>
        {
            new() { Username = "alice", IsActive = true },
            new() { Username = "bob", IsActive = true }
        };

        var handler = new MockHttpHandler()
            .RespondWith("users", items);

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadUsers();
        });

        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
        resultCtx.Users.Count.ShouldBe(2);
        resultCtx.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnLoadUsers_EmptyList_ReturnsEmpty()
    {
        var handler = new MockHttpHandler()
            .RespondWith("users", new List<UserSummaryPayload>());

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadUsers();
        });

        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
        resultCtx.Users.Count.ShouldBe(0);
        resultCtx.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnLoadUsers_ApiFailure_SetsErrorMessage()
    {
        var handler = new MockHttpHandler()
            .RespondError("users");

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadUsers();
        });

        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
        resultCtx.Users.Count.ShouldBe(0);
        resultCtx.ErrorMessage.ShouldNotBeNullOrEmpty();
    }

    // ── Filter Tests ────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnSearchChanged_FiltersUsers()
    {
        var items = new List<UserSummaryPayload>
        {
            new() { Username = "alice", IsActive = true },
            new() { Username = "bob", IsActive = true }
        };

        var handler = new MockHttpHandler()
            .RespondWith("users", items);

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadUsers();
        });

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnSearchChanged("ali");
        });

        var resultCtx = GetContext(component);
        resultCtx.FilteredUsers.Count.ShouldBe(1);
        resultCtx.FilteredUsers.First().Username.ShouldBe("alice");
    }

    // ── Delete Tests ────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnDeleteUser_Success_RefreshesList()
    {
        var userId = Guid.NewGuid();

        var handler = new MockHttpHandler()
            .RespondOk($"users/{userId}")
            .RespondWith("users", new List<UserSummaryPayload>());

        var component = RenderWithHandler(handler);

        bool deleted = false;
        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            deleted = await ctx.OnDeleteUser(userId);
        });

        deleted.ShouldBeTrue();
        var resultCtx = GetContext(component);
        resultCtx.Users.Count.ShouldBe(0);
    }

    public void Dispose() => _ctx.Dispose();
}
