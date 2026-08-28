using Bunit;
using Fdw.Messages;
using Fdw.Results;
using Bunit.ComponentFactories;
using Fdw.Services.Authorization.Components.Roles;
using Fdw.Services.Authorization.Components.Users;
using Fdw.Services.Users.Clients.Models;
using Fdw.UI.Components.Blazor.Tests.Helpers;
using UsersPage = Fdw.UI.Pages.Authorization.Pages.UsersPage;

namespace Fdw.UI.Components.Blazor.Tests.Components.Auth;

/// <summary>
/// Component tests for the FDW <c>Users</c> page (Authorization.UI.Pages). Relocated from
/// reference-ui's Auth/UsersPageTests, which asserted these behaviours through the hosted page;
/// here they run directly against the page component with a seeded <see cref="UserContext"/> (and
/// the nested <see cref="RoleContext"/> for the role-checkbox list) swapped in for the live
/// providers.
/// </summary>
[Trait("Category", "Ui")]
public sealed class UsersPageContentTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    private IRenderedComponent<UsersPage> Render(UserContext seed, RoleContext? roleSeed = null)
    {
        _ctx.ComponentFactories.Add(new ProviderStubFactory<UserProvider, UserContext>(seed));
        _ctx.ComponentFactories.Add(new ProviderStubFactory<RoleProvider, RoleContext>(roleSeed ?? new RoleContext()));
        return _ctx.Render<UsersPage>();
    }

    private static UserSummaryPayload User(string username = "alice", bool active = true, string[]? roles = null) => new()
    {
        Id = Guid.NewGuid(),
        Username = username,
        Email = $"{username}@example.com",
        IsActive = active,
        Roles = roles ?? ["Admin"],
        CreatedAt = DateTime.UtcNow,
    };

    [Fact]
    public void RendersHeaderAndNewUserButton()
    {
        var cut = Render(new UserContext());
        cut.Markup.ShouldContain("Users");
        cut.FindAll("button").Any(b => b.TextContent.Contains("New User", StringComparison.Ordinal)).ShouldBeTrue();
    }

    [Fact]
    public void LoadingBranchShowsLoadingBadgeWhenNoUsers()
    {
        var cut = Render(new UserContext { IsLoading = true });
        cut.FindAll(".badge.b-run").Count.ShouldBeGreaterThan(0);
        cut.Markup.ShouldContain("Loading");
    }

    [Fact]
    public void ListBranchRendersUserRows()
    {
        var cut = Render(new UserContext { FilteredUsers = [User("alice"), User("bob")] });
        cut.Markup.ShouldContain("alice");
        cut.Markup.ShouldContain("bob");
        cut.Markup.ShouldContain("alice@example.com");
    }

    [Fact]
    public void ActiveUserShowsActiveBadgeInactiveShowsInactive()
    {
        var cut = Render(new UserContext { FilteredUsers = [User("on", active: true), User("off", active: false)] });
        cut.Markup.ShouldContain("Active");
        cut.Markup.ShouldContain("Inactive");
    }

    [Fact]
    public void LastLoginNullRendersNever()
    {
        var u = User();
        u.LastLoginAt = null;
        var cut = Render(new UserContext { FilteredUsers = [u] });
        cut.Markup.ShouldContain("Never");
    }

    [Fact]
    public async Task SearchInputInvokesOnSearchChanged()
    {
        string? searched = null;
        var cut = Render(new UserContext
        {
            FilteredUsers = [User()],
            OnSearchChanged = s => { searched = s; return Task.CompletedTask; },
        });
        cut.Find("input[placeholder='Search users...']").Input("ali");
        await Task.Yield();
        searched.ShouldBe("ali");
    }

    [Fact]
    public async Task DeleteUserInvokesOnDeleteUserWithUserId()
    {
        var u = User();
        Guid? deleted = null;
        var cut = Render(new UserContext
        {
            FilteredUsers = [u],
            OnDeleteUser = id => { deleted = id; return Task.FromResult(true); },
        });
        var rowButtons = cut.FindAll("tbody button");
        rowButtons[^1].Click();
        await Task.Yield();
        deleted.ShouldBe(u.Id);
    }

    [Fact]
    public void NewUserButtonOpensCreateForm()
    {
        var cut = Render(new UserContext());
        cut.FindAll("button").First(b => b.TextContent.Contains("New User", StringComparison.Ordinal)).Click();
        cut.Markup.ShouldContain("Create User");
        cut.FindAll("input[placeholder='Username']").Count.ShouldBe(1);
        cut.FindAll("input[placeholder='Min 8 characters']").Count.ShouldBe(1);
    }

    [Fact]
    public async Task CreateWithShortUsernameShowsValidationErrorAndDoesNotCallCreate()
    {
        var called = false;
        var cut = Render(new UserContext
        {
            OnCreateUser = _ => { called = true; return Task.FromResult<UserDetailPayload?>(new UserDetailPayload()); },
        });
        cut.FindAll("button").First(b => b.TextContent.Contains("New User", StringComparison.Ordinal)).Click();
        cut.Find("input[placeholder='Username']").Change("ab");
        cut.FindAll("button").First(b => string.Equals(b.TextContent.Trim(), "Create", StringComparison.Ordinal)).Click();
        await Task.Yield();
        cut.Markup.ShouldContain("Username must be at least 3 characters");
        called.ShouldBeFalse();
    }

    [Fact]
    public async Task CreateWithShortPasswordShowsValidationError()
    {
        var cut = Render(new UserContext
        {
            OnCreateUser = _ => Task.FromResult<UserDetailPayload?>(new UserDetailPayload()),
        });
        cut.FindAll("button").First(b => b.TextContent.Contains("New User", StringComparison.Ordinal)).Click();
        cut.Find("input[placeholder='Username']").Change("validname");
        cut.Find("input[placeholder='Min 8 characters']").Change("short");
        cut.FindAll("button").First(b => string.Equals(b.TextContent.Trim(), "Create", StringComparison.Ordinal)).Click();
        await Task.Yield();
        cut.Markup.ShouldContain("Password must be at least 8 characters");
    }

    [Fact]
    public async Task CreateWithValidInputInvokesOnCreateUserAndReloads()
    {
        CreateUserRequest? captured = null;
        var reloaded = false;
        var cut = Render(new UserContext
        {
            OnCreateUser = req => { captured = req; return Task.FromResult<UserDetailPayload?>(new UserDetailPayload()); },
            OnLoadUsers = () => { reloaded = true; return Task.CompletedTask; },
        });
        cut.FindAll("button").First(b => b.TextContent.Contains("New User", StringComparison.Ordinal)).Click();
        cut.Find("input[placeholder='Username']").Change("validuser");
        cut.Find("input[placeholder='Min 8 characters']").Change("longenough");
        cut.FindAll("button").First(b => string.Equals(b.TextContent.Trim(), "Create", StringComparison.Ordinal)).Click();
        await Task.Yield();
        captured.ShouldNotBeNull();
        captured!.Username.ShouldBe("validuser");
        reloaded.ShouldBeTrue();
    }

    [Fact]
    public async Task CreateWhenServiceReturnsNullShowsContextError()
    {
        var cut = Render(new UserContext
        {
            LastResult = GenericResult.Failure(new GenericMessage("duplicate username")),
            OnCreateUser = _ => Task.FromResult<UserDetailPayload?>(null),
        });
        cut.FindAll("button").First(b => b.TextContent.Contains("New User", StringComparison.Ordinal)).Click();
        cut.Find("input[placeholder='Username']").Change("validuser");
        cut.Find("input[placeholder='Min 8 characters']").Change("longenough");
        cut.FindAll("button").First(b => string.Equals(b.TextContent.Trim(), "Create", StringComparison.Ordinal)).Click();
        await Task.Yield();
        cut.Markup.ShouldContain("duplicate username");
    }

    [Fact]
    public void EditUserButtonOpensFormInEditModePasswordHidden()
    {
        var cut = Render(new UserContext { FilteredUsers = [User("alice")] });
        cut.FindAll("tbody button")[0].Click();
        cut.Markup.ShouldContain("Edit User");
        cut.FindAll("input[placeholder='Min 8 characters']").Count.ShouldBe(0);
    }

    [Fact]
    public void EmptyUsernameRendersSafelyWithoutThrowing()
    {
        var cut = Should.NotThrow(() => Render(new UserContext { FilteredUsers = [User(string.Empty)] }));
        cut.FindAll("tbody tr").Count.ShouldBe(1);
    }

    public void Dispose() => _ctx.Dispose();
}
