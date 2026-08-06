using Bunit;
using Fdw.Services.Authorization.Clients.Models;
using Fdw.Services.Authorization.Components.Roles;
using Fdw.UI.Components.Blazor.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Fdw.UI.Components.Blazor.Tests.Providers;

/// <summary>
/// Tests for <see cref="RoleProvider"/> headless component.
/// Uses MockHttpHandler because RoleApiClient is created internally via IHttpClientFactory.
/// </summary>
[Trait("Category", "Ui")]
public sealed class RoleProviderTests : IDisposable
{
    private readonly BunitContext _ctx;

    public RoleProviderTests()
    {
        _ctx = new BunitContext();
    }

    private IRenderedComponent<RoleProvider> RenderWithHandler(MockHttpHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        _ctx.Services.AddSingleton(factoryMock.Object);
        _ctx.Services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        _ctx.Services.AddSingleton<ILogger<RoleProvider>>(NullLogger<RoleProvider>.Instance);

        return _ctx.Render<RoleProvider>();
    }

    private static RoleContext GetContext(IRenderedComponent<RoleProvider> component)
    {
        var field = typeof(RoleProvider).GetField("_context",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (RoleContext)field!.GetValue(component.Instance)!;
    }

    // ── Load Tests ──────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnLoadRoles_LoadsItems()
    {
        var items = new List<RoleSummaryPayload>
        {
            new() { Name = "Admin", SortOrder = 1 },
            new() { Name = "Viewer", SortOrder = 2 }
        };

        var handler = new MockHttpHandler()
            .RespondWith("roles", items);

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadRoles();
        });

        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
        resultCtx.Roles.Count.ShouldBe(2);
        resultCtx.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnLoadRoles_EmptyList_ReturnsEmpty()
    {
        var handler = new MockHttpHandler()
            .RespondWith("roles", new List<RoleSummaryPayload>());

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadRoles();
        });

        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
        resultCtx.Roles.Count.ShouldBe(0);
        resultCtx.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnLoadRoles_ApiFailure_SetsErrorMessage()
    {
        var handler = new MockHttpHandler()
            .RespondError("roles");

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadRoles();
        });

        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
        resultCtx.Roles.Count.ShouldBe(0);
        resultCtx.ErrorMessage.ShouldNotBeNullOrEmpty();
    }

    // ── Permission Group Tests ───────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnLoadPermissionGroups_LoadsGroups()
    {
        var groups = new List<PermissionGroupPayload>
        {
            new() { Resource = "Connections" },
            new() { Resource = "DataSets" }
        };

        var handler = new MockHttpHandler()
            .RespondWith("permissions/grouped", groups);

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadPermissionGroups();
        });

        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
        resultCtx.PermissionGroups.Count.ShouldBe(2);
        resultCtx.ErrorMessage.ShouldBeNull();
    }

    // ── Delete Tests ────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnDeleteRole_Success_RemovesFromList()
    {
        // Load roles first, then delete
        var initial = new List<RoleSummaryPayload>
        {
            new() { Name = "OldRole", SortOrder = 1 }
        };

        var handler = new MockHttpHandler()
            .RespondWith("roles", initial)
            .RespondOk("roles/OldRole");

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadRoles();
        });

        bool deleted = false;
        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            deleted = await ctx.OnDeleteRole("OldRole");
        });

        deleted.ShouldBeTrue();
        var resultCtx = GetContext(component);
        resultCtx.Roles.ShouldBeEmpty();
    }

    public void Dispose() => _ctx.Dispose();
}
