using Bunit;
using Bunit.ComponentFactories;
using Fdw.Services.Authorization.Clients.Models;
using Fdw.Services.Authorization.Components.Roles;
using Fdw.UI.Components.Blazor.Tests.Helpers;
using RolesPage = Fdw.UI.Pages.Authorization.Pages.RolesPage;

namespace Fdw.UI.Components.Blazor.Tests.Components.Auth;

/// <summary>
/// Component tests for the FDW <c>Roles</c> list page (Authorization.UI.Pages). Relocated from
/// reference-ui's Auth/RolesPageTests, which asserted these behaviours through the hosted page;
/// here they run directly against the page component with a seeded <see cref="RoleContext"/>
/// swapped in for the live <see cref="RoleProvider"/>.
/// </summary>
[Trait("Category", "Ui")]
public sealed class RolesPageContentTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    private IRenderedComponent<RolesPage> Render(RoleContext seed)
    {
        _ctx.ComponentFactories.Add(new ProviderStubFactory<RoleProvider, RoleContext>(seed));
        return _ctx.Render<RolesPage>();
    }

    private static RoleSummaryPayload Role(string name = "Admin") => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        DisplayName = name,
        Description = $"{name} role",
        IsTenantScoped = false,
        SortOrder = 1,
    };

    [Fact]
    public void RendersHeaderAndNewRoleButton()
    {
        var cut = Render(new RoleContext());
        cut.Markup.ShouldContain("Roles");
        cut.FindAll("button").Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void LoadingBranchWhenNoRoles()
    {
        var cut = Render(new RoleContext { IsLoading = true });
        cut.FindAll("a[href^='/roles/']").Count.ShouldBe(0);
    }

    [Fact]
    public void ListBranchRendersRoles()
    {
        var cut = Render(new RoleContext { Roles = [Role("Admin"), Role("Operator")] });
        cut.Markup.ShouldContain("Admin");
        cut.Markup.ShouldContain("Operator");
    }

    [Fact]
    public void CreateDialogOpensOnNewRoleClickWithNameAndDescriptionInputs()
    {
        var cut = Render(new RoleContext());
        cut.FindAll("button").First(b => b.TextContent.Contains("New", StringComparison.OrdinalIgnoreCase)
                                          || b.TextContent.Contains("Create", StringComparison.OrdinalIgnoreCase)).Click();
        cut.FindAll("input[placeholder='Role name']").Count.ShouldBe(1);
        cut.FindAll("input[placeholder='Description']").Count.ShouldBe(1);
    }

    [Fact]
    public async Task CreateWithNameInvokesOnCreateRoleAndReloads()
    {
        CreateRolePayload? captured = null;
        var reloaded = false;
        var cut = Render(new RoleContext
        {
            OnCreateRole = req => { captured = req; return Task.FromResult<RoleDetailPayload?>(new RoleDetailPayload()); },
            OnLoadRoles = () => { reloaded = true; return Task.CompletedTask; },
        });
        cut.FindAll("button").First(b => b.TextContent.Contains("New", StringComparison.OrdinalIgnoreCase)
                                          || b.TextContent.Contains("Create", StringComparison.OrdinalIgnoreCase)).Click();
        cut.Find("input[placeholder='Role name']").Change("Auditor");
        cut.FindAll("button").First(b => string.Equals(b.TextContent.Trim(), "Create", StringComparison.Ordinal)).Click();
        await Task.Yield();
        captured.ShouldNotBeNull();
        captured!.Name.ShouldBe("Auditor");
        reloaded.ShouldBeTrue();
    }

    [Fact]
    public async Task CreateWithBlankNameDoesNotInvokeOnCreateRole()
    {
        var called = false;
        var cut = Render(new RoleContext
        {
            OnCreateRole = _ => { called = true; return Task.FromResult<RoleDetailPayload?>(new RoleDetailPayload()); },
        });
        cut.FindAll("button").First(b => b.TextContent.Contains("New", StringComparison.OrdinalIgnoreCase)
                                          || b.TextContent.Contains("Create", StringComparison.OrdinalIgnoreCase)).Click();
        cut.FindAll("button").First(b => string.Equals(b.TextContent.Trim(), "Create", StringComparison.Ordinal)).Click();
        await Task.Yield();
        called.ShouldBeFalse();
    }

    [Fact]
    public async Task DeleteInvokesOnDeleteRoleWithRoleName()
    {
        string? deleted = null;
        var cut = Render(new RoleContext
        {
            Roles = [Role("Operator")],
            OnDeleteRole = name => { deleted = name; return Task.FromResult(true); },
        });
        cut.FindAll("button").First(b => string.IsNullOrWhiteSpace(b.TextContent) && b.QuerySelector("svg") != null).Click();
        await Task.Yield();
        deleted.ShouldBe("Operator");
    }

    public void Dispose() => _ctx.Dispose();
}
