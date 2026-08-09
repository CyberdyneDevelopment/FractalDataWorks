using Bunit;
using Fdw.Messages;
using Fdw.Results;
using Bunit.ComponentFactories;
using Fdw.Services.Authentication.Clients.Models;
using Fdw.Services.Authentication.Components.ApiKeys;
using Fdw.UI.Components.Blazor.Tests.Helpers;
using ApiKeysPage = Fdw.UI.Pages.Authentication.Pages.ApiKeysPage;

namespace Fdw.UI.Components.Blazor.Tests.Components.Auth;

/// <summary>
/// Component tests for the FDW <c>ApiKeys</c> page (Authentication.UI.Pages). Relocated from
/// reference-ui's Auth/ApiKeysPageTests, which asserted these behaviours through the hosted page;
/// here they run directly against the page component with a seeded <see cref="ApiKeyContext"/>
/// swapped in for the live <see cref="ApiKeyProvider"/>.
/// </summary>
[Trait("Category", "Ui")]
public sealed class ApiKeysPageContentTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    private IRenderedComponent<ApiKeysPage> Render(ApiKeyContext seed)
    {
        _ctx.ComponentFactories.Add(new ProviderStubFactory<ApiKeyProvider, ApiKeyContext>(seed));
        return _ctx.Render<ApiKeysPage>();
    }

    private static PersonalAccessTokenSummaryPayload Token(string label = "CI") =>
        new() { TokenId = Guid.NewGuid(), Label = label, Prefix = "pat_abc", CreatedAt = DateTime.UtcNow };

    private static AgentKeySummaryPayload AgentKey(string label = "bot") =>
        new() { KeyId = Guid.NewGuid(), Label = label, Prefix = "agt_xyz", CreatedAt = DateTime.UtcNow };

    [Fact]
    public void RendersHeaderAndRefreshButton()
    {
        var cut = Render(new ApiKeyContext());
        cut.Markup.ShouldContain("API Keys");
        cut.FindAll("button").Any(b => b.TextContent.Contains("Refresh", StringComparison.Ordinal)).ShouldBeTrue();
    }

    [Fact]
    public async Task RefreshButtonInvokesOnRefresh()
    {
        var called = false;
        var cut = Render(new ApiKeyContext { OnRefresh = () => { called = true; return Task.FromResult<IGenericResult>(GenericResult.Success()); } });
        cut.FindAll("button").First(b => b.TextContent.Contains("Refresh", StringComparison.Ordinal)).Click();
        await Task.Yield();
        called.ShouldBeTrue();
    }

    [Fact]
    public void ErrorBranchRendersErrorMessage()
    {
        var cut = Render(new ApiKeyContext { LastResult = GenericResult.Failure(new GenericMessage("boom")) });
        cut.Markup.ShouldContain("boom");
    }

    [Fact]
    public void NewTokenValueRendersCopyBannerAndValue()
    {
        var cut = Render(new ApiKeyContext { NewTokenValue = "pat_secret_value" });
        cut.Markup.ShouldContain("Copy this token now");
        cut.Markup.ShouldContain("pat_secret_value");
    }

    [Fact]
    public void NewTokenValueEmptyHidesCopyBanner()
    {
        var cut = Render(new ApiKeyContext());
        cut.Markup.ShouldNotContain("Copy this token now");
    }

    [Fact]
    public void LoadingBranchShowsLoadingBadgeWhenNoData()
    {
        var cut = Render(new ApiKeyContext { IsLoading = true });
        cut.FindAll(".badge.b-run").Count.ShouldBeGreaterThan(0);
        cut.Markup.ShouldContain("Loading");
    }

    [Fact]
    public void EmptyStateShowsNoTokensAndNoKeys()
    {
        var cut = Render(new ApiKeyContext());
        cut.Markup.ShouldContain("No personal access tokens.");
        cut.Markup.ShouldContain("No agent keys.");
    }

    [Fact]
    public void ListRendersTokenAndAgentKeyLabels()
    {
        var cut = Render(new ApiKeyContext
        {
            PersonalTokens = [Token("CI/CD")],
            AgentKeys = [AgentKey("nightly")],
        });
        cut.Markup.ShouldContain("CI/CD");
        cut.Markup.ShouldContain("nightly");
        cut.Markup.ShouldNotContain("No personal access tokens.");
        cut.Markup.ShouldNotContain("No agent keys.");
    }

    [Fact]
    public async Task RevokeTokenInvokesOnRevokeTokenWithTokenId()
    {
        var token = Token();
        Guid? revoked = null;
        var cut = Render(new ApiKeyContext
        {
            PersonalTokens = [token],
            OnRevokeToken = id => { revoked = id; return Task.CompletedTask; },
        });
        cut.FindAll("button").First(b => b.TextContent.Contains("Revoke", StringComparison.OrdinalIgnoreCase)).Click();
        await Task.Yield();
        revoked.ShouldBe(token.TokenId);
    }

    [Fact]
    public async Task DeleteAgentKeyInvokesOnDeleteAgentKeyWithKeyId()
    {
        var key = AgentKey();
        Guid? deleted = null;
        var cut = Render(new ApiKeyContext
        {
            AgentKeys = [key],
            OnDeleteAgentKey = id => { deleted = id; return Task.CompletedTask; },
        });
        cut.FindAll("button").First(b => b.TextContent.Contains("Delete", StringComparison.OrdinalIgnoreCase)
                                          || b.TextContent.Contains("Revoke", StringComparison.OrdinalIgnoreCase)).Click();
        await Task.Yield();
        deleted.ShouldBe(key.KeyId);
    }

    [Fact]
    public void NewTokenButtonOpensCreateFormWithLabelInput()
    {
        var cut = Render(new ApiKeyContext());
        cut.FindAll("button").First(b => b.TextContent.Contains("New Token", StringComparison.Ordinal)).Click();
        cut.FindAll("input").Any(i => i.GetAttribute("placeholder")?.Contains("CI/CD", StringComparison.Ordinal) == true).ShouldBeTrue();
    }

    [Fact]
    public async Task CreateTokenWithLabelInvokesOnCreateToken()
    {
        var created = false;
        var cut = Render(new ApiKeyContext
        {
            OnCreateToken = _ => { created = true; return Task.FromResult<string?>("new_value"); },
        });
        cut.FindAll("button").First(b => b.TextContent.Contains("New Token", StringComparison.Ordinal)).Click();
        cut.FindAll("input").First(i => i.GetAttribute("placeholder")?.Contains("CI/CD", StringComparison.Ordinal) == true)
           .Change("My Token");
        cut.FindAll("button").First(b => string.Equals(b.TextContent.Trim(), "Create", StringComparison.Ordinal)).Click();
        await Task.Yield();
        created.ShouldBeTrue();
    }

    [Fact]
    public async Task CreateTokenWithBlankLabelDoesNotInvokeOnCreateToken()
    {
        var created = false;
        var cut = Render(new ApiKeyContext
        {
            OnCreateToken = _ => { created = true; return Task.FromResult<string?>("x"); },
        });
        cut.FindAll("button").First(b => b.TextContent.Contains("New Token", StringComparison.Ordinal)).Click();
        cut.FindAll("button").First(b => string.Equals(b.TextContent.Trim(), "Create", StringComparison.Ordinal)).Click();
        await Task.Yield();
        created.ShouldBeFalse();
    }

    public void Dispose() => _ctx.Dispose();
}
