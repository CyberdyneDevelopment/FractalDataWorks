using System;
using System.Collections.Generic;
using Fdw.Web.RestEndpoints.Configuration;

namespace Fdw.Web.RestEndpoints.Tests.Configuration;

[Collection(nameof(RestEndpointsTestCollection))]
public class SecurityConfigurationTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Constructor_SetsDefaultValues()
    {
        // Arrange & Act
        var config = new SecurityConfiguration();

        // Assert
        config.SecurityEnabled.ShouldBeTrue();
        config.DefaultSecurityMethod.ShouldBe(string.Empty);
        config.Jwt.ShouldNotBeNull();
        config.ApiKey.ShouldNotBeNull();
        config.OAuth2.ShouldNotBeNull();
        config.Certificate.ShouldNotBeNull();
        config.Cors.ShouldNotBeNull();
        config.SecurityHeaders.ShouldNotBeNull();
        config.SecurityHeaders.Count.ShouldBe(4);
        config.DetailedSecurityErrors.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Constructor_SetsDefaultSecurityHeaders()
    {
        // Arrange & Act
        var config = new SecurityConfiguration();

        // Assert
        config.SecurityHeaders["X-Content-Type-Options"].ShouldBe("nosniff");
        config.SecurityHeaders["X-Frame-Options"].ShouldBe("DENY");
        config.SecurityHeaders["X-XSS-Protection"].ShouldBe("1; mode=block");
        config.SecurityHeaders["Referrer-Policy"].ShouldBe("strict-origin-when-cross-origin");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Properties_CanBeInitialized()
    {
        // Arrange
        var customHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["X-Custom-Header"] = "CustomValue"
        };

        // Act
        var config = new SecurityConfiguration
        {
            SecurityEnabled = false,
            DefaultSecurityMethod = "JWT",
            Jwt = new JwtSecurityConfiguration { Enabled = false },
            ApiKey = new ApiKeySecurityConfiguration { Enabled = false },
            OAuth2 = new OAuth2SecurityConfiguration { Enabled = true },
            Certificate = new CertificateSecurityConfiguration { Enabled = true },
            Cors = new CorsSecurityConfiguration { Enabled = false },
            SecurityHeaders = customHeaders,
            DetailedSecurityErrors = true
        };

        // Assert
        config.SecurityEnabled.ShouldBeFalse();
        config.DefaultSecurityMethod.ShouldBe("JWT");
        config.Jwt.Enabled.ShouldBeFalse();
        config.ApiKey.Enabled.ShouldBeFalse();
        config.OAuth2.Enabled.ShouldBeTrue();
        config.Certificate.Enabled.ShouldBeTrue();
        config.Cors.Enabled.ShouldBeFalse();
        config.SecurityHeaders.ShouldBe(customHeaders);
        config.DetailedSecurityErrors.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void SecurityEnabled_CanBeSet()
    {
        // Arrange & Act
        var config = new SecurityConfiguration { SecurityEnabled = false };

        // Assert
        config.SecurityEnabled.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void DefaultSecurityMethod_CanBeSet()
    {
        // Arrange & Act
        var config = new SecurityConfiguration { DefaultSecurityMethod = "ApiKey" };

        // Assert
        config.DefaultSecurityMethod.ShouldBe("ApiKey");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void DefaultSecurityMethodEnum_ReturnsEmptySecurityMethod_WhenDefaultSecurityMethodIsEmpty()
    {
        // Arrange
        var config = new SecurityConfiguration { DefaultSecurityMethod = string.Empty };

        // Act
        var securityMethod = config.DefaultSecurityMethodEnum;

        // Assert
        securityMethod.ShouldNotBeNull();
        securityMethod.Name.ShouldBe("_Empty");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Jwt_CanBeSet()
    {
        // Arrange
        var jwtConfig = new JwtSecurityConfiguration { Issuer = "test" };

        // Act
        var config = new SecurityConfiguration { Jwt = jwtConfig };

        // Assert
        config.Jwt.ShouldBe(jwtConfig);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ApiKey_CanBeSet()
    {
        // Arrange
        var apiKeyConfig = new ApiKeySecurityConfiguration { HeaderName = "X-Test" };

        // Act
        var config = new SecurityConfiguration { ApiKey = apiKeyConfig };

        // Assert
        config.ApiKey.ShouldBe(apiKeyConfig);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void OAuth2_CanBeSet()
    {
        // Arrange
        var oauth2Config = new OAuth2SecurityConfiguration { Authority = "https://test.com" };

        // Act
        var config = new SecurityConfiguration { OAuth2 = oauth2Config };

        // Assert
        config.OAuth2.ShouldBe(oauth2Config);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Certificate_CanBeSet()
    {
        // Arrange
        var certConfig = new CertificateSecurityConfiguration { Enabled = true };

        // Act
        var config = new SecurityConfiguration { Certificate = certConfig };

        // Assert
        config.Certificate.ShouldBe(certConfig);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Cors_CanBeSet()
    {
        // Arrange
        var corsConfig = new CorsSecurityConfiguration { Enabled = false };

        // Act
        var config = new SecurityConfiguration { Cors = corsConfig };

        // Assert
        config.Cors.ShouldBe(corsConfig);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void SecurityHeaders_CanBeSet()
    {
        // Arrange
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Header1"] = "Value1",
            ["Header2"] = "Value2"
        };

        // Act
        var config = new SecurityConfiguration { SecurityHeaders = headers };

        // Assert
        config.SecurityHeaders.ShouldBe(headers);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void DetailedSecurityErrors_CanBeSet()
    {
        // Arrange & Act
        var config = new SecurityConfiguration { DetailedSecurityErrors = true };

        // Assert
        config.DetailedSecurityErrors.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void IsValid_ReturnsFalse_WhenNoSecurityMethodEnabled()
    {
        // Arrange - default config has SecurityEnabled=true but no methods enabled
        var config = new SecurityConfiguration();

        // Act
        var isValid = config.IsValid();

        // Assert
        isValid.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void IsValid_ReturnsTrue_WhenSecurityDisabled()
    {
        // Arrange
        var config = new SecurityConfiguration { SecurityEnabled = false };

        // Act
        var isValid = config.IsValid();

        // Assert
        isValid.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void IsValid_ReturnsTrue_WhenJwtEnabledWithIssuer()
    {
        // Arrange
        var config = new SecurityConfiguration
        {
            Jwt = new JwtSecurityConfiguration { Enabled = true, Issuer = "https://issuer.example.com" }
        };

        // Act
        var isValid = config.IsValid();

        // Assert
        isValid.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void IsValid_ReturnsFalse_WhenJwtEnabledWithoutIssuer()
    {
        // Arrange
        var config = new SecurityConfiguration
        {
            Jwt = new JwtSecurityConfiguration { Enabled = true, Issuer = string.Empty }
        };

        // Act
        var isValid = config.IsValid();

        // Assert
        isValid.ShouldBeFalse();
    }
}
