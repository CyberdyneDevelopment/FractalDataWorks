using Fdw.Services.Authentication;

namespace Fdw.Services.Authentication.Tests.Methods;

/// <summary>
/// Data-verification tests for the pure-data TypeOptions under <c>src/Fdw.Services.Authentication/Methods</c>
/// — <see cref="Fdw.Services.Authentication.Abstractions.AuthenticationMethodBase"/>,
/// <see cref="Fdw.Services.Authentication.Abstractions.AuthenticationFlowBase"/>,
/// <see cref="Fdw.Services.Authentication.Abstractions.AuthenticationProtocolBase"/>, and
/// <see cref="Fdw.Services.Authentication.Abstractions.TokenTypeBase"/> implementations. Each option
/// carries its own Id/Name/behavioral flags per the "TypeOption data on the option" convention —
/// these tests lock that data in place so a copy-paste edit (e.g. a duplicated Id) is caught the
/// next time a value changes, even though production Id-uniqueness is NOT asserted here (see the
/// JWT/FormBased Id=2 collision recorded as a defect in the task report).
/// </summary>
public sealed class AuthenticationTypeOptionsTests
{
    // ── AuthenticationMethods ────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Security")]
    public void OAuth2AuthenticationMethodCarriesDeclaredData()
    {
        var sut = new OAuth2AuthenticationMethod();

        sut.Id.ShouldBe(1);
        sut.Name.ShouldBe("OAuth2");
        sut.RequiresUserInteraction.ShouldBeTrue();
        sut.SupportsTokenRefresh.ShouldBeTrue();
        sut.SupportsMultiTenant.ShouldBeTrue();
        sut.AuthenticationScheme.ShouldBe("Bearer");
        sut.Priority.ShouldBe(90);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Security")]
    public void FormBasedAuthenticationMethodCarriesDeclaredData()
    {
        var sut = new FormBasedAuthenticationMethod();

        sut.Id.ShouldBe(2);
        sut.Name.ShouldBe("FormBased");
        sut.RequiresUserInteraction.ShouldBeTrue();
        sut.SupportsTokenRefresh.ShouldBeFalse();
        sut.SupportsMultiTenant.ShouldBeFalse();
        sut.AuthenticationScheme.ShouldBe("Cookies");
        sut.Priority.ShouldBe(30);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Security")]
    public void BearerTokenAuthenticationMethodCarriesDeclaredData()
    {
        var sut = new BearerTokenAuthenticationMethod();

        sut.Id.ShouldBe(3);
        sut.Name.ShouldBe("BearerToken");
        sut.RequiresUserInteraction.ShouldBeFalse();
        sut.SupportsTokenRefresh.ShouldBeTrue();
        sut.SupportsMultiTenant.ShouldBeTrue();
        sut.AuthenticationScheme.ShouldBe("Bearer");
        sut.Priority.ShouldBe(80);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Security")]
    public void ApiKeyAuthenticationMethodCarriesDeclaredData()
    {
        var sut = new ApiKeyAuthenticationMethod();

        sut.Id.ShouldBe(4);
        sut.Name.ShouldBe("ApiKey");
        sut.RequiresUserInteraction.ShouldBeFalse();
        sut.SupportsTokenRefresh.ShouldBeFalse();
        sut.SupportsMultiTenant.ShouldBeFalse();
        sut.AuthenticationScheme.ShouldBe("ApiKey");
        sut.Priority.ShouldBe(70);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Security")]
    public void CertificateAuthenticationMethodCarriesDeclaredData()
    {
        var sut = new CertificateAuthenticationMethod();

        sut.Id.ShouldBe(5);
        sut.Name.ShouldBe("Certificate");
        sut.RequiresUserInteraction.ShouldBeFalse();
        sut.SupportsTokenRefresh.ShouldBeFalse();
        sut.SupportsMultiTenant.ShouldBeTrue();
        sut.AuthenticationScheme.ShouldBe("Certificate");
        sut.Priority.ShouldBe(95);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Security")]
    public void ManagedIdentityAuthenticationMethodCarriesDeclaredData()
    {
        var sut = new ManagedIdentityAuthenticationMethod();

        sut.Id.ShouldBe(6);
        sut.Name.ShouldBe("ManagedIdentity");
        sut.RequiresUserInteraction.ShouldBeFalse();
        sut.SupportsTokenRefresh.ShouldBeTrue();
        sut.SupportsMultiTenant.ShouldBeTrue();
        sut.AuthenticationScheme.ShouldBe("Bearer");
        sut.Priority.ShouldBe(100);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Security")]
    public void OpenIdConnectAuthenticationMethodCarriesDeclaredData()
    {
        var sut = new OpenIDConnectAuthenticationMethod();

        sut.Id.ShouldBe(7);
        sut.Name.ShouldBe("OpenIDConnect");
        sut.RequiresUserInteraction.ShouldBeTrue();
        sut.SupportsTokenRefresh.ShouldBeTrue();
        sut.SupportsMultiTenant.ShouldBeTrue();
        sut.AuthenticationScheme.ShouldBe("Bearer");
        sut.Priority.ShouldBe(90);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Security")]
    public void JwtAuthenticationMethodCarriesDeclaredData()
    {
        var sut = new JwtAuthenticationMethod();

        sut.Id.ShouldBe(2);
        sut.Name.ShouldBe("JWT");
        sut.RequiresUserInteraction.ShouldBeFalse();
        sut.SupportsTokenRefresh.ShouldBeFalse();
        sut.SupportsMultiTenant.ShouldBeTrue();
        sut.AuthenticationScheme.ShouldBe("Bearer");
        sut.Priority.ShouldBe(85);
    }

    // ── AuthenticationFlows ──────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Security")]
    public void AuthorizationCodeFlowCarriesDeclaredData()
    {
        var sut = new AuthorizationCodeFlow();

        sut.Id.ShouldBe(1);
        sut.Name.ShouldBe("AuthorizationCode");
        sut.RequiresUserInteraction.ShouldBeTrue();
        sut.SupportsRefreshTokens.ShouldBeTrue();
        sut.IsServerToServer.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Security")]
    public void ClientCredentialsFlowCarriesDeclaredData()
    {
        var sut = new ClientCredentialsFlow();

        sut.Id.ShouldBe(2);
        sut.Name.ShouldBe("ClientCredentials");
        sut.RequiresUserInteraction.ShouldBeFalse();
        sut.SupportsRefreshTokens.ShouldBeFalse();
        sut.IsServerToServer.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Security")]
    public void InteractiveFlowCarriesDeclaredData()
    {
        var sut = new InteractiveFlow();

        sut.Id.ShouldBe(3);
        sut.Name.ShouldBe("Interactive");
        sut.RequiresUserInteraction.ShouldBeTrue();
        sut.SupportsRefreshTokens.ShouldBeTrue();
        sut.IsServerToServer.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Security")]
    public void DeviceCodeFlowCarriesDeclaredData()
    {
        var sut = new DeviceCodeFlow();

        sut.Id.ShouldBe(4);
        sut.Name.ShouldBe("DeviceCode");
        sut.RequiresUserInteraction.ShouldBeTrue();
        sut.SupportsRefreshTokens.ShouldBeTrue();
        sut.IsServerToServer.ShouldBeFalse();
    }

    // ── AuthenticationProtocols ──────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Security")]
    public void OAuth2ProtocolCarriesDeclaredData()
    {
        var sut = new OAuth2Protocol();

        sut.Id.ShouldBe(1);
        sut.Name.ShouldBe("OAuth2");
        sut.Version.ShouldBe("2.0");
        sut.RequiresSecureTransport.ShouldBeTrue();
        sut.SupportsTokens.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Security")]
    public void OpenIdConnectProtocolCarriesDeclaredData()
    {
        var sut = new OpenIDConnectProtocol();

        sut.Id.ShouldBe(2);
        sut.Name.ShouldBe("OpenIDConnect");
        sut.Version.ShouldBe("1.0");
        sut.RequiresSecureTransport.ShouldBeTrue();
        sut.SupportsTokens.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Security")]
    public void Saml2ProtocolCarriesDeclaredData()
    {
        var sut = new SAML2Protocol();

        sut.Id.ShouldBe(3);
        sut.Name.ShouldBe("SAML2");
        sut.Version.ShouldBe("2.0");
        sut.RequiresSecureTransport.ShouldBeTrue();
        sut.SupportsTokens.ShouldBeFalse();
    }

    // ── TokenTypes ───────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Security")]
    public void AccessTokenCarriesDeclaredData()
    {
        var sut = new AccessToken();

        sut.Id.ShouldBe(1);
        sut.Name.ShouldBe("AccessToken");
        sut.Format.ShouldBe("JWT");
        sut.CanBeRefreshed.ShouldBeFalse();
        sut.ContainsUserIdentity.ShouldBeFalse();
        sut.TypicalLifetimeSeconds.ShouldBe(3600);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Security")]
    public void IdTokenCarriesDeclaredData()
    {
        var sut = new IdToken();

        sut.Id.ShouldBe(2);
        sut.Name.ShouldBe("IdToken");
        sut.Format.ShouldBe("JWT");
        sut.CanBeRefreshed.ShouldBeFalse();
        sut.ContainsUserIdentity.ShouldBeTrue();
        sut.TypicalLifetimeSeconds.ShouldBe(3600);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Security")]
    public void RefreshTokenCarriesDeclaredData()
    {
        var sut = new RefreshToken();

        sut.Id.ShouldBe(3);
        sut.Name.ShouldBe("RefreshToken");
        sut.Format.ShouldBe("Opaque");
        sut.CanBeRefreshed.ShouldBeTrue();
        sut.ContainsUserIdentity.ShouldBeFalse();
        sut.TypicalLifetimeSeconds.ShouldBe(2592000);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Security")]
    public void BearerTokenTypeCarriesDeclaredData()
    {
        var sut = new BearerTokenType();

        sut.Id.ShouldBe(4);
        sut.Name.ShouldBe("BearerToken");
        sut.Format.ShouldBe("JWT");
        sut.CanBeRefreshed.ShouldBeFalse();
        sut.ContainsUserIdentity.ShouldBeTrue();
        sut.TypicalLifetimeSeconds.ShouldBe(3600);
    }
}
