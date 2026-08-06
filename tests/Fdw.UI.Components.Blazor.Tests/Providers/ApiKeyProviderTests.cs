using Bunit;
using Fdw.Services.Authentication.Clients.Models;
using Fdw.Services.Authentication.Components.ApiKeys;
using Fdw.UI.Components.Blazor.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Fdw.UI.Components.Blazor.Tests.Providers;

/// <summary>
/// Tests for <see cref="ApiKeyProvider"/> headless component.
/// Uses MockHttpHandler because AuthenticationApiClient is sealed.
/// </summary>
[Trait("Category", "Ui")]
public sealed class ApiKeyProviderTests : IDisposable
{
    private readonly BunitContext _ctx;

    public ApiKeyProviderTests()
    {
        _ctx = new BunitContext();
    }

    private IRenderedComponent<ApiKeyProvider> RenderWithHandler(MockHttpHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        _ctx.Services.AddSingleton(factoryMock.Object);
        _ctx.Services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        _ctx.Services.AddSingleton<ILogger<ApiKeyProvider>>(NullLogger<ApiKeyProvider>.Instance);

        return _ctx.Render<ApiKeyProvider>();
    }

    private static ApiKeyContext GetContext(IRenderedComponent<ApiKeyProvider> component)
    {
        var field = typeof(ApiKeyProvider).GetField("_context",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (ApiKeyContext)field!.GetValue(component.Instance)!;
    }

    // ── Load Tests ──────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public void InitialState_EmptyLists_NoError()
    {
        var handler = new MockHttpHandler()
            .RespondWith("tokens", new List<PersonalAccessTokenSummaryPayload>())
            .RespondWith("agent-keys", new List<AgentKeySummaryPayload>());

        var component = RenderWithHandler(handler);
        var ctx = GetContext(component);

        ctx.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnRefresh_LoadsTokensAndAgentKeys()
    {
        var tokens = new List<PersonalAccessTokenSummaryPayload>
        {
            new() { TokenId = Guid.NewGuid(), Label = "CI Token" }
        };
        var keys = new List<AgentKeySummaryPayload>
        {
            new() { KeyId = Guid.NewGuid(), Label = "Agent-1" }
        };

        var handler = new MockHttpHandler()
            .RespondWith("tokens", tokens)
            .RespondWith("agent-keys", keys);

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnRefresh();
        });

        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
        resultCtx.PersonalTokens.Count.ShouldBe(1);
        resultCtx.AgentKeys.Count.ShouldBe(1);
    }

    // ── Error Handling ──────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P2")]
    public async Task OnRefresh_ApiFailure_SetsErrorMessage()
    {
        var handler = new MockHttpHandler()
            .RespondError("tokens");

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

    // ── Create Token Tests ──────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnCreateToken_Success_SetsNewTokenValue()
    {
        var handler = new MockHttpHandler()
            .RespondWith("tokens", new CreateTokenResponse { RawToken = "fdw_abc123", Label = "My Token" })
            .RespondWith("agent-keys", new List<AgentKeySummaryPayload>());

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnCreateToken(new CreateTokenRequest { Label = "My Token" });
        });

        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
    }

    // ── Revoke Token Tests ──────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnRevokeToken_CallsApiAndRefreshes()
    {
        var handler = new MockHttpHandler()
            .RespondWith("tokens", new List<PersonalAccessTokenSummaryPayload>())
            .RespondWith("agent-keys", new List<AgentKeySummaryPayload>());

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnRevokeToken(Guid.NewGuid());
        });

        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
    }

    // ── Create Agent Key Tests ──────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnCreateAgentKey_Success()
    {
        var handler = new MockHttpHandler()
            .RespondWith("agent-keys", new CreateAgentKeyResponse { RawKey = "ak_xyz", Label = "Agent-1" })
            .RespondWith("tokens", new List<PersonalAccessTokenSummaryPayload>());

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnCreateAgentKey(new CreateAgentKeyRequest { Label = "Agent-1" });
        });

        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
    }

    // ── Delete Agent Key Tests ──────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnDeleteAgentKey_Success()
    {
        var handler = new MockHttpHandler()
            .RespondWith("tokens", new List<PersonalAccessTokenSummaryPayload>())
            .RespondWith("agent-keys", new List<AgentKeySummaryPayload>());

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnDeleteAgentKey(Guid.NewGuid());
        });

        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
    }

    public void Dispose() => _ctx.Dispose();
}
