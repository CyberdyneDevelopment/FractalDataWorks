using System;
using System.Net;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Steps;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Authentication.Tests.Steps;

/// <summary>
/// FDW-5730: an OIDC authority row says where its callback address comes from. With RequestHost, a sign-in on a
/// slot's staging hostname returns to staging and one on the public hostname to the live slot, though both read
/// one row; a host not on the row's list is refused; FixedAddress is the row's address whatever the host. Every
/// refusal has its own code.
/// </summary>
public sealed class OidcRedirectAddressSourceTests
{
    private const string Live = "https://app.example.test";
    private const string Staging = "https://app-staging.preview.example.test";
    private const string CallbackPath = "/api/v1/auth/oidc/callback";
    private const string BothOrigins = Live + ", " + Staging;

    private static IGenericResult<OidcRedirectAddress> Read(string? source, string? redirectUri, string? origins)
        => OidcRedirectAddressSources.Read("Authentik", source, redirectUri, origins, NullLogger.Instance);

    private static OidcRedirectStepConfiguration Authority(OidcRedirectAddress address) => new()
    {
        Name = "Authentik",
        Issuer = "https://login.example.test/application/o/app/",
        AuthorizationEndpoint = new Uri("https://login.example.test/application/o/authorize/"),
        TokenEndpoint = new Uri("https://login.example.test/application/o/token/"),
        JwksUri = new Uri("https://login.example.test/application/o/app/jwks/"),
        ClientId = "client",
        ClientAuthenticationType = "None",
        RedirectUri = address.RedirectUri,
        RedirectAddressSource = address.Source,
        AllowedRedirectOrigins = address.AllowedOrigins,
        Scopes = ["openid"],
        SubjectClaim = "sub",
        ValidAudiences = ["client"],
        ValidAlgorithms = ["RS256"],
        ClockSkew = TimeSpan.FromSeconds(30),
        AssertableMethods = ["pwd"],
    };

    private static DefaultHttpContext Request(string scheme, string? host)
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = scheme;
        if (host is not null)
            context.Request.Host = new HostString(host);
        return context;
    }

    private static IGenericResult<Uri> Resolve(OidcRedirectStepConfiguration authority, HttpContext? context)
    {
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(a => a.HttpContext).Returns(context);
        return new HttpOidcRedirectAddressResolver(accessor.Object).Resolve(authority);
    }

    private static OidcRedirectStepConfiguration RequestHostAuthority()
        => Authority(Read("RequestHost", CallbackPath, BothOrigins).Value.ShouldNotBeNull());

    // The row an A/B host keeps in its one shared database: RedirectUri stays the absolute address a build from
    // before FDW-5730 reads (RST OidcAuthorityReader.cs:102, Uri.TryCreate(row.RedirectUri, UriKind.Absolute)).
    private const string SharedRowRedirectUri = Live + CallbackPath;

    private static OidcRedirectStepConfiguration SharedRowAuthority()
        => Authority(Read("RequestHost", SharedRowRedirectUri, BothOrigins).Value.ShouldNotBeNull());

    private static void ShouldFailWith<T>(IGenericResult<T> result, string code)
    {
        result.IsFailure.ShouldBeTrue();
        result.Messages[0].Code.ShouldBe(code);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ASignInOnTheStagingHostnameReturnsToStaging()
        => Resolve(RequestHostAuthority(), Request("https", "app-staging.preview.example.test"))
            .Value.ShouldBe(new Uri(Staging + CallbackPath));

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ASignInOnThePublicHostnameReturnsToLive()
        => Resolve(RequestHostAuthority(), Request("https", "app.example.test"))
            .Value.ShouldBe(new Uri(Live + CallbackPath));

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void AHostNotOnTheListIsRefused()
        => ShouldFailWith(Resolve(RequestHostAuthority(), Request("https", "attacker.example.test")), "AUTHENTICATION-101290");

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void AListedHostOverPlainHttpIsNotTheHttpsOrigin()
        => ShouldFailWith(Resolve(RequestHostAuthority(), Request("http", "app.example.test")), "AUTHENTICATION-101290");

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void AListedHostOnAnotherPortIsRefused()
        => ShouldFailWith(Resolve(RequestHostAuthority(), Request("https", "app.example.test:8443")), "AUTHENTICATION-101290");

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void TheHostIsComparedWithoutRegardToCase()
        => Resolve(RequestHostAuthority(), Request("https", "APP.Example.TEST"))
            .Value.ShouldBe(new Uri(Live + CallbackPath));

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task TheSchemeIsTheOneTheForwardedHeadersMiddlewareReports()
    {
        // Behind Caddy the app sees plain http; X-Forwarded-Proto says the person used https.
        var context = Request("http", "app-staging.preview.example.test");
        context.Connection.RemoteIpAddress = IPAddress.Loopback;
        context.Request.Headers["X-Forwarded-Proto"] = "https";
        var middleware = new ForwardedHeadersMiddleware(
            _ => Task.CompletedTask,
            NullLoggerFactory.Instance,
            Options.Create(new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedProto }));

        await middleware.Invoke(context);

        Resolve(RequestHostAuthority(), context).Value.ShouldBe(new Uri(Staging + CallbackPath));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void AFixedAddressIsTheRowsWhateverTheHost()
    {
        var authority = Authority(Read("FixedAddress", Live + CallbackPath, null).Value.ShouldNotBeNull());

        Resolve(authority, Request("https", "app-staging.preview.example.test")).Value.ShouldBe(new Uri(Live + CallbackPath));
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void ARowThatNamesNoSourceIsRefused()
        => ShouldFailWith(Read(null, Live + CallbackPath, null), "AUTHENTICATION-61012");

    [Fact]
    [Trait("Priority", "P1")]
    public void ARowThatNamesAnUnknownSourceIsRefused()
        => ShouldFailWith(Read("HostHeader", CallbackPath, BothOrigins), "AUTHENTICATION-61013");

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ARequestHostRowWithNoListIsRefused()
        => ShouldFailWith(Read("RequestHost", CallbackPath, " "), "AUTHENTICATION-61014");

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    [InlineData("https://*.example.test")]
    [InlineData("*")]
    [InlineData("https://app.example.test/api")]
    [InlineData("https://app.example.test?x=1")]
    [InlineData("https://user@app.example.test")]
    [InlineData("ftp://app.example.test")]
    [InlineData("app.example.test")]
    public void AListEntryThatIsNotAPlainOriginIsRefused(string entry)
        => ShouldFailWith(Read("RequestHost", CallbackPath, Live + ", " + entry), "AUTHENTICATION-61015");

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ASharedRowWithAnAbsoluteRedirectUriReturnsAStagingSignInToStaging()
        => Resolve(SharedRowAuthority(), Request("https", "app-staging.preview.example.test"))
            .Value.ShouldBe(new Uri(Staging + CallbackPath));

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ASharedRowWithAnAbsoluteRedirectUriReturnsALiveSignInToLive()
        => Resolve(SharedRowAuthority(), Request("https", "app.example.test"))
            .Value.ShouldBe(new Uri(Live + CallbackPath));

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ASharedRowRefusesAHostOutsideTheList()
        => ShouldFailWith(Resolve(SharedRowAuthority(), Request("https", "attacker.example.test")), "AUTHENTICATION-101290");

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void AStoredAbsoluteRedirectUriWhoseOriginIsNotListedIsRefused()
        => ShouldFailWith(Read("RequestHost", "https://elsewhere.example.test" + CallbackPath, BothOrigins), "AUTHENTICATION-61020");

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void TheStoredOriginIsNotUsedOnlyItsPath()
        // The shared row stores the LIVE address; a staging request still gets the staging origin with the stored path.
        => Resolve(SharedRowAuthority(), Request("https", "app-staging.preview.example.test"))
            .Value!.GetLeftPart(UriPartial.Authority).ShouldBe(Staging);

    [Fact]
    [Trait("Priority", "P1")]
    public void TheOlderReaderStillReadsTheSharedRowsRedirectUri()
    {
        // What a build from before FDW-5730 does with the row (RST OidcAuthorityReader.cs:102); it never reads the source or the list.
        Uri.TryCreate(SharedRowRedirectUri, UriKind.Absolute, out var older).ShouldBeTrue();
        older.ShouldBe(new Uri(Live + CallbackPath));
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    [InlineData("//attacker.example.test/callback")]
    [InlineData("/api/v1/auth/oidc/callback?next=x")]
    [InlineData("https://app.example.test//attacker.example.test/callback")]
    [InlineData("https://app.example.test/api/v1/auth/oidc/callback?next=x")]
    [InlineData("https://app.example.test/api/v1/auth/oidc/callback#x")]
    [InlineData("https://app.example.test")]
    public void ARequestHostRowWhoseCallbackPathIsNotUsableIsRefused(string redirectUri)
        => ShouldFailWith(Read("RequestHost", redirectUri, BothOrigins), "AUTHENTICATION-61016");

    [Theory]
    [Trait("Priority", "P1")]
    [InlineData("api/v1/auth/oidc/callback")]
    [InlineData("ftp://app.example.test/callback")]
    [InlineData("")]
    public void ARequestHostRowWhoseRedirectUriIsNeitherAPathNorAnAbsoluteAddressIsRefused(string redirectUri)
        => ShouldFailWith(Read("RequestHost", redirectUri, BothOrigins), "AUTHENTICATION-61019");

    [Theory]
    [Trait("Priority", "P1")]
    [InlineData(CallbackPath)]
    [InlineData("ftp://app.example.test/callback")]
    [InlineData("")]
    public void AFixedAddressRowWhoseRedirectUriIsNotAnAbsoluteHttpAddressIsRefused(string redirectUri)
        => ShouldFailWith(Read("FixedAddress", redirectUri, null), "AUTHENTICATION-61017");

    [Fact]
    [Trait("Priority", "P1")]
    public void AFixedAddressRowThatAlsoListsOriginsIsRefused()
        => ShouldFailWith(Read("FixedAddress", Live + CallbackPath, BothOrigins), "AUTHENTICATION-61018");

    [Fact]
    [Trait("Priority", "P1")]
    public void ARequestHostSignInWithNoRequestInHandIsRefused()
        => ShouldFailWith(Resolve(RequestHostAuthority(), null), "AUTHENTICATION-91290");

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ARequestWithNoHostIsRefused()
        => ShouldFailWith(Resolve(RequestHostAuthority(), Request("https", null)), "AUTHENTICATION-21290");

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ARequestWhoseSchemeIsNotHttpIsRefused()
        => ShouldFailWith(Resolve(RequestHostAuthority(), Request("ws", "app.example.test")), "AUTHENTICATION-21291");

    [Fact]
    [Trait("Priority", "P1")]
    public void TheResolverRefusesAnAuthorityThatNamesNoSource()
        => ShouldFailWith(Resolve(RequestHostAuthority() with { RedirectAddressSource = string.Empty }, Request("https", "app.example.test")), "AUTHENTICATION-61012");

    [Fact]
    [Trait("Priority", "P1")]
    public void TheResolverRefusesAnAuthorityThatNamesAnUnknownSource()
        => ShouldFailWith(Resolve(RequestHostAuthority() with { RedirectAddressSource = "HostHeader" }, Request("https", "app.example.test")), "AUTHENTICATION-61013");

    [Fact]
    [Trait("Priority", "P2")]
    public void NoSourceReadsTheHostHeaderOnItsOwn()
        => OidcRedirectAddressSources.KnownNames().Split(", ").ShouldBe(["FixedAddress", "RequestHost"], ignoreOrder: true);
}
