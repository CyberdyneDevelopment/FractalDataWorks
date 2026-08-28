using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Abstractions.Methods;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.WebMcp.Hosting.Tests;

/// <summary>
/// Which credential the middleware recognises, and what it says about the caller afterwards.
/// </summary>
/// <remarks>
/// Agent keys and personal access tokens both arrive as <c>Bearer fdx_*</c>, so the branch between
/// them is the whole behaviour worth testing: routed wrongly, an agent key is reported as a bad PAT
/// and the caller is never marked as an agent.
/// </remarks>
public class WebMcpApiKeyMiddlewareTests
{
    private const string AgentKey = "fdx_agent_abc123";
    private const string PersonalToken = "fdx_prod_abc123";

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task AgentKeyIsValidatedByTheAgentServiceAndCarriesTheAgentClaims()
    {
        var userId = Guid.NewGuid();
        var agentService = new Mock<IAgentKeyService>();
        agentService
            .Setup(s => s.ValidateKey(AgentKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<AgentKeyValidationResult>.Success(new AgentKeyValidationResult
            {
                IsValid = true,
                UserId = userId,
                KeyId = Guid.NewGuid(),
                AgentKeyId = 42,
                Label = "mike - claude code",
            }));

        var context = await Invoke($"Bearer {AgentKey}", agentService.Object, patService: null);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status200OK);

        // sub stays the OWNER's: the agent acts as that person, so every permission check and RLS
        // predicate downstream must keep seeing them. The agent claims say who is driving.
        context.User.FindFirst(ClaimDefinitions.sub.Name)!.Value.ShouldBe(userId.ToString());
        context.User.FindFirst(ClaimDefinitions.agent.Name)!.Value.ShouldBe("true");
        context.User.FindFirst(ClaimDefinitions.agentLabel.Name)!.Value.ShouldBe("mike - claude code");
        context.User.FindFirst(ClaimDefinitions.agentKeyId.Name)!.Value.ShouldBe("42");
        context.User.Identity!.AuthenticationType.ShouldBe(AuthenticationSchemes.AgentKey);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task PersonalAccessTokenIsNotMarkedAsAnAgent()
    {
        // The signal a person scripting with their own token must NOT trip. Deriving "is an agent"
        // from the credential family rather than this claim attributed them to the agent.
        var patService = new Mock<IPersonalAccessTokenService>();
        patService
            .Setup(s => s.ValidateToken(PersonalToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<PersonalAccessTokenValidationResult>.Success(
                new PersonalAccessTokenValidationResult { IsValid = true, UserId = Guid.NewGuid() }));

        var context = await Invoke($"Bearer {PersonalToken}", agentService: null, patService.Object);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status200OK);
        context.User.FindFirst(ClaimDefinitions.agent.Name).ShouldBeNull();
        context.User.Identity!.AuthenticationType.ShouldBe(AuthenticationSchemes.PatBearer);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task AnAgentKeyIsNeverOfferedToThePersonalTokenService()
    {
        // Why this is asserted rather than left implied: sending an agent key to the PAT service
        // reports an unrecognised agent key as a bad token, and an operator reading the log cannot
        // tell which credential actually failed.
        var patService = new Mock<IPersonalAccessTokenService>(MockBehavior.Strict);
        var agentService = new Mock<IAgentKeyService>();
        agentService
            .Setup(s => s.ValidateKey(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<AgentKeyValidationResult>.Success(
                new AgentKeyValidationResult { IsValid = false }));

        var context = await Invoke($"Bearer {AgentKey}", agentService.Object, patService.Object);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status401Unauthorized);
        patService.VerifyNoOtherCalls();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Security")]
    public async Task AnUnregisteredAgentServiceRefusesRatherThanFallingBackToThePatService()
    {
        // NO FALLBACKS: with no agent service the answer is 401, not "try it as a PAT". A fallback
        // would let a misconfigured host silently authenticate agent keys with the wrong validator.
        var patService = new Mock<IPersonalAccessTokenService>(MockBehavior.Strict);

        var context = await Invoke($"Bearer {AgentKey}", agentService: null, patService.Object);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status401Unauthorized);
        patService.VerifyNoOtherCalls();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Security")]
    public async Task AHeaderThatIsNotAnFdxCredentialPassesStraightThrough()
    {
        var context = await Invoke("Bearer some.jwt.value", agentService: null, patService: null);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status200OK);
        context.User.Identity!.IsAuthenticated.ShouldBeFalse();
    }

    private static async Task<HttpContext> Invoke(
        string authorizationHeader,
        IAgentKeyService? agentService,
        IPersonalAccessTokenService? patService)
    {
        var services = new ServiceCollection();
        if (agentService is not null)
        {
            services.AddSingleton(agentService);
        }

        if (patService is not null)
        {
            services.AddSingleton(patService);
        }

        var context = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider(),
            User = new ClaimsPrincipal(new ClaimsIdentity()),
        };
        context.Request.Headers.Authorization = authorizationHeader;
        context.Request.Path = "/messages";

        var middleware = new WebMcpApiKeyMiddleware(
            _ => Task.CompletedTask,
            NullLogger<WebMcpApiKeyMiddleware>.Instance);

        await middleware.Invoke(context);

        return context;
    }
}
