using Fdw.Web.Http.Abstractions.Security;

namespace Fdw.Web.Http.Abstractions.Tests;

/// <summary>
/// Tests for <see cref="SecurityMethodBase"/> properties and constructor behavior.
/// Concrete types exercise the base class constructor and property assignments.
/// </summary>
public sealed class SecurityMethodBaseTests
{
    // --- None ---

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void NoneHasCorrectId()
    {
        var sut = new None();

        sut.Id.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void NoneHasCorrectName()
    {
        var sut = new None();

        sut.Name.ShouldBe("None");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void NoneDoesNotRequireAuthentication()
    {
        var sut = new None();

        sut.RequiresAuthentication.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void NoneHasNullAuthenticationScheme()
    {
        var sut = new None();

        sut.AuthenticationScheme.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void NoneDoesNotSupportTokenRefresh()
    {
        var sut = new None();

        sut.SupportsTokenRefresh.ShouldBeFalse();
    }

    // --- JWT ---

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void JwtHasCorrectId()
    {
        var sut = new JWT();

        sut.Id.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void JwtHasCorrectName()
    {
        var sut = new JWT();

        sut.Name.ShouldBe("JWT");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void JwtRequiresAuthentication()
    {
        var sut = new JWT();

        sut.RequiresAuthentication.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void JwtHasBearerScheme()
    {
        var sut = new JWT();

        sut.AuthenticationScheme.ShouldBe("Bearer");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void JwtSupportsTokenRefresh()
    {
        var sut = new JWT();

        sut.SupportsTokenRefresh.ShouldBeTrue();
    }

    // --- ApiKey ---

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ApiKeyHasCorrectId()
    {
        var sut = new ApiKey();

        sut.Id.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ApiKeyHasCorrectName()
    {
        var sut = new ApiKey();

        sut.Name.ShouldBe("ApiKey");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ApiKeyRequiresAuthentication()
    {
        var sut = new ApiKey();

        sut.RequiresAuthentication.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ApiKeyHasApiKeyScheme()
    {
        var sut = new ApiKey();

        sut.AuthenticationScheme.ShouldBe("ApiKey");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ApiKeyDoesNotSupportTokenRefresh()
    {
        var sut = new ApiKey();

        sut.SupportsTokenRefresh.ShouldBeFalse();
    }

    // --- OAuth2 ---

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void OAuth2HasCorrectId()
    {
        var sut = new OAuth2();

        sut.Id.ShouldBe(4);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void OAuth2HasCorrectName()
    {
        var sut = new OAuth2();

        sut.Name.ShouldBe("OAuth2");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void OAuth2RequiresAuthentication()
    {
        var sut = new OAuth2();

        sut.RequiresAuthentication.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void OAuth2HasBearerScheme()
    {
        var sut = new OAuth2();

        sut.AuthenticationScheme.ShouldBe("Bearer");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void OAuth2SupportsTokenRefresh()
    {
        var sut = new OAuth2();

        sut.SupportsTokenRefresh.ShouldBeTrue();
    }

    // --- Certificate ---

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void CertificateHasCorrectId()
    {
        var sut = new Certificate();

        sut.Id.ShouldBe(5);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void CertificateHasCorrectName()
    {
        var sut = new Certificate();

        sut.Name.ShouldBe("Certificate");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void CertificateRequiresAuthentication()
    {
        var sut = new Certificate();

        sut.RequiresAuthentication.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void CertificateHasCertificateScheme()
    {
        var sut = new Certificate();

        sut.AuthenticationScheme.ShouldBe("Certificate");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void CertificateDoesNotSupportTokenRefresh()
    {
        var sut = new Certificate();

        sut.SupportsTokenRefresh.ShouldBeFalse();
    }
}
