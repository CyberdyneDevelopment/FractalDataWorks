using System;
using System.Threading.Tasks;
using Fdw.Services.Authentication.Abstractions.Sessions;
using Fdw.Services.Authentication.Sessions;
using Fdw.Services.Authentication.Sessions.Csrf;
using Fdw.Services.Authentication.Validation;
using Fdw.Services.TokenManagers.Abstractions;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace Fdw.Services.Authentication.Tests.Sessions;

/// <summary>
/// The CSRF protections a Session service can name (FDW-903, FDW-945): what each accepts and
/// refuses on a cookie-authenticated write.
/// </summary>
public sealed class SessionCsrfProtectionTests
{
    private const string Token = "a-csrf-token";

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void Both_protections_are_registered_under_the_names_a_row_carries()
    {
        SessionCsrfProtectionTypes.ByName("OriginCheck").ShouldBeOfType<OriginCheckCsrfProtection>();
        SessionCsrfProtectionTypes.ByName("SynchronizerToken").ShouldBeOfType<SynchronizerTokenCsrfProtection>();
        SessionCsrfProtectionTypes.ByName("Antiforgery").ShouldBeOfType<AntiforgeryCsrfProtection>();
        SessionCsrfProtectionTypes.ByName("DoubleSubmit").ShouldBe(SessionCsrfProtectionTypes.NotFound);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task Origin_check_accepts_a_write_from_a_listed_origin()
        => (await new OriginCheckCsrfProtection()
            .Verify(Write(origin: "https://console.example"), Session(), OriginService(), NullLogger.Instance, TestContext.Current.CancellationToken))
            .IsSuccess.ShouldBeTrue();

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task Origin_check_refuses_a_write_from_another_origin()
        => (await new OriginCheckCsrfProtection()
            .Verify(Write(origin: "https://attacker.example"), Session(), OriginService(), NullLogger.Instance, TestContext.Current.CancellationToken))
            .IsSuccess.ShouldBeFalse();

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task Origin_check_refuses_a_write_that_names_no_origin()
        => (await new OriginCheckCsrfProtection()
            .Verify(Write(), Session(), OriginService(), NullLogger.Instance, TestContext.Current.CancellationToken))
            .IsSuccess.ShouldBeFalse();

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task Origin_check_compares_origins_exactly()
        => (await new OriginCheckCsrfProtection()
            .Verify(Write(origin: "https://console.example/"), Session(), OriginService(), NullLogger.Instance, TestContext.Current.CancellationToken))
            .IsSuccess.ShouldBeFalse();

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task Synchronizer_token_accepts_a_write_that_echoes_the_session_token()
        => (await new SynchronizerTokenCsrfProtection()
            .Verify(Write(header: Token), Session(DatabaseSessionStore.Hash(Token)), TokenService(), NullLogger.Instance, TestContext.Current.CancellationToken))
            .IsSuccess.ShouldBeTrue();

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task Synchronizer_token_refuses_a_write_with_a_different_token()
        => (await new SynchronizerTokenCsrfProtection()
            .Verify(Write(header: "guessed"), Session(DatabaseSessionStore.Hash(Token)), TokenService(), NullLogger.Instance, TestContext.Current.CancellationToken))
            .IsSuccess.ShouldBeFalse();

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task Synchronizer_token_refuses_a_write_with_no_token()
        => (await new SynchronizerTokenCsrfProtection()
            .Verify(Write(), Session(DatabaseSessionStore.Hash(Token)), TokenService(), NullLogger.Instance, TestContext.Current.CancellationToken))
            .IsSuccess.ShouldBeFalse();

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task Synchronizer_token_refuses_every_write_until_a_token_is_issued()
        => (await new SynchronizerTokenCsrfProtection()
            .Verify(Write(header: Token), Session(), TokenService(), NullLogger.Instance, TestContext.Current.CancellationToken))
            .IsSuccess.ShouldBeFalse();

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task Antiforgery_accepts_a_write_that_carries_a_valid_token_pair()
    {
        var services = AntiforgeryServices();
        var request = PrimedAntiforgeryRequest(services);

        var result = await new AntiforgeryCsrfProtection().Verify(request, Session(), AntiforgeryService(), NullLogger.Instance, TestContext.Current.CancellationToken);
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task Antiforgery_refuses_a_write_with_no_token_pair()
    {
        var context = new DefaultHttpContext { RequestServices = AntiforgeryServices() };
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/api/v1/things";

        var result = await new AntiforgeryCsrfProtection().Verify(context.Request, Session(), AntiforgeryService(), NullLogger.Instance, TestContext.Current.CancellationToken);
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Antiforgery_validate_configuration_rejects_allowed_origins_set()
        => new AntiforgeryCsrfProtection()
            .ValidateConfiguration(OriginService(), NullLogger.Instance)
            .IsSuccess.ShouldBeFalse();

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Antiforgery_validate_configuration_accepts_no_allowed_origins()
        => new AntiforgeryCsrfProtection()
            .ValidateConfiguration(AntiforgeryService(), NullLogger.Instance)
            .IsSuccess.ShouldBeTrue();

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void Antiforgery_issues_a_token_through_IAntiforgery()
    {
        var context = new DefaultHttpContext { RequestServices = AntiforgeryServices() };
        var issued = new AntiforgeryCsrfProtection().IssueToken(context, Guid.CreateVersion7(), NullLogger.Instance);

        issued.IsSuccess.ShouldBeTrue();
        issued.Value.ShouldNotBeNullOrEmpty();
    }

    private static ServiceProvider AntiforgeryServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddAntiforgery(o => o.HeaderName = "X-CSRF-Token");
        return services.BuildServiceProvider();
    }

    // Mints a token pair on one context (as sign-in would) and carries both halves — the cookie the
    // response set and the header value — onto a second context, the way a browser round-trip would.
    private static HttpRequest PrimedAntiforgeryRequest(IServiceProvider services)
    {
        var issuing = new DefaultHttpContext { RequestServices = services };
        var tokens = services.GetRequiredService<IAntiforgery>().GetAndStoreTokens(issuing);
        var cookiePair = issuing.Response.Headers.SetCookie.ToString().Split(';')[0];

        var context = new DefaultHttpContext { RequestServices = services };
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/api/v1/things";
        context.Request.Headers.Cookie = cookiePair;
        context.Request.Headers["X-CSRF-Token"] = tokens.RequestToken;
        return context.Request;
    }

    private static HttpRequest Write(string? origin = null, string? header = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/api/v1/things";
        if (origin is not null)
            context.Request.Headers.Origin = origin;
        if (header is not null)
            context.Request.Headers["X-CSRF-Token"] = header;
        return context.Request;
    }

    private static SessionRecord Session(byte[]? csrfTokenHash = null)
        => new()
        {
            Id = Guid.CreateVersion7(),
            ServiceName = "ConsoleSession",
            UserId = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow,
            AbsoluteExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            Assertion = new IssuanceRequest
            {
                PrincipalId = Guid.NewGuid(),
                TenantId = Guid.NewGuid(),
                Audience = "reference-api",
                Scopes = [],
                AuthenticationMethods = ["pwd"],
            },
            CsrfTokenHash = csrfTokenHash,
        };

    private static SessionAuthenticationConfiguration OriginService()
        => new()
        {
            Name = "ConsoleSession",
            CookieName = "fdw_session",
            SameSite = "Strict",
            CsrfProtection = "OriginCheck",
            AllowedOrigins = "https://console.example",
        };

    private static SessionAuthenticationConfiguration TokenService()
        => new()
        {
            Name = "ConsoleSession",
            CookieName = "fdw_session",
            SameSite = "Strict",
            CsrfProtection = "SynchronizerToken",
            CsrfHeaderName = "X-CSRF-Token",
        };

    private static SessionAuthenticationConfiguration AntiforgeryService()
        => new()
        {
            Name = "ConsoleSession",
            CookieName = "fdw_session",
            SameSite = "Strict",
            CsrfProtection = "Antiforgery",
        };
}
