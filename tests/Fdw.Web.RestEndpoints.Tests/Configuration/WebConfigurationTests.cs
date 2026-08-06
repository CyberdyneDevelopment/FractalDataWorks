using Fdw.Web.RestEndpoints.Configuration;

namespace Fdw.Web.RestEndpoints.Tests.Configuration;

public class WebConfigurationTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Constructor_SetsDefaultValues()
    {
        // Arrange & Act
        var config = new WebConfiguration();

        // Assert
        config.Host.ShouldBe("localhost");
        config.Port.ShouldBe(5000);
        config.ForceHttps.ShouldBeTrue();
        config.SslCertificatePath.ShouldBeNull();
        config.SslCertificatePassword.ShouldBeNull();
        config.Authentication.ShouldNotBeNull();
        config.Cors.ShouldNotBeNull();
        config.Swagger.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void SectionName_ReturnsCorrectValue()
    {
        // Arrange
        var config = new WebConfiguration();

        // Act
        var sectionName = config.SectionName;

        // Assert
        sectionName.ShouldBe("FdwWeb");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Properties_CanBeInitialized()
    {
        // Arrange
        var authConfig = new AuthenticationConfiguration();
        var corsConfig = new CorsConfiguration();
        var swaggerConfig = new SwaggerConfiguration();

        // Act
        var config = new WebConfiguration
        {
            Host = "0.0.0.0",
            Port = 8080,
            ForceHttps = false,
            SslCertificatePath = "/path/to/cert.pfx",
            SslCertificatePassword = "password123",
            Authentication = authConfig,
            Cors = corsConfig,
            Swagger = swaggerConfig
        };

        // Assert
        config.Host.ShouldBe("0.0.0.0");
        config.Port.ShouldBe(8080);
        config.ForceHttps.ShouldBeFalse();
        config.SslCertificatePath.ShouldBe("/path/to/cert.pfx");
        config.SslCertificatePassword.ShouldBe("password123");
        config.Authentication.ShouldBe(authConfig);
        config.Cors.ShouldBe(corsConfig);
        config.Swagger.ShouldBe(swaggerConfig);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Host_CanBeSet()
    {
        // Arrange & Act
        var config = new WebConfiguration { Host = "127.0.0.1" };

        // Assert
        config.Host.ShouldBe("127.0.0.1");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Port_CanBeSet()
    {
        // Arrange & Act
        var config = new WebConfiguration { Port = 443 };

        // Assert
        config.Port.ShouldBe(443);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ForceHttps_CanBeSet()
    {
        // Arrange & Act
        var config = new WebConfiguration { ForceHttps = false };

        // Assert
        config.ForceHttps.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void SslCertificatePath_CanBeSet()
    {
        // Arrange & Act
        var config = new WebConfiguration { SslCertificatePath = "/etc/ssl/cert.pfx" };

        // Assert
        config.SslCertificatePath.ShouldBe("/etc/ssl/cert.pfx");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void SslCertificatePassword_CanBeSet()
    {
        // Arrange & Act
        var config = new WebConfiguration { SslCertificatePassword = "secret" };

        // Assert
        config.SslCertificatePassword.ShouldBe("secret");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Authentication_CanBeSet()
    {
        // Arrange
        var authConfig = new AuthenticationConfiguration
        {
            Jwt = new JwtConfiguration { Issuer = "test" }
        };

        // Act
        var config = new WebConfiguration { Authentication = authConfig };

        // Assert
        config.Authentication.ShouldBe(authConfig);
        config.Authentication.Jwt.Issuer.ShouldBe("test");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Cors_CanBeSet()
    {
        // Arrange
        var corsConfig = new CorsConfiguration { Enabled = false };

        // Act
        var config = new WebConfiguration { Cors = corsConfig };

        // Assert
        config.Cors.ShouldBe(corsConfig);
        config.Cors.Enabled.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Swagger_CanBeSet()
    {
        // Arrange
        var swaggerConfig = new SwaggerConfiguration { Title = "Test API" };

        // Act
        var config = new WebConfiguration { Swagger = swaggerConfig };

        // Assert
        config.Swagger.ShouldBe(swaggerConfig);
        config.Swagger.Title.ShouldBe("Test API");
    }
}
