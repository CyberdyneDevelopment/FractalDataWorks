using Fdw.Web.RestEndpoints.Configuration;

namespace Fdw.Web.RestEndpoints.Tests.Configuration;

public class SwaggerConfigurationTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Constructor_SetsDefaultValues()
    {
        // Arrange & Act
        var config = new SwaggerConfiguration();

        // Assert
        config.Enabled.ShouldBeTrue();
        config.Title.ShouldBe("Fdw Web API");
        config.Description.ShouldBe("API built with Fdw Web Framework");
        config.Version.ShouldBe("v1");
        config.RoutePrefix.ShouldBe("swagger");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Properties_CanBeInitialized()
    {
        // Arrange & Act
        var config = new SwaggerConfiguration
        {
            Enabled = false,
            Title = "My Custom API",
            Description = "Custom API description",
            Version = "v2",
            RoutePrefix = "api-docs"
        };

        // Assert
        config.Enabled.ShouldBeFalse();
        config.Title.ShouldBe("My Custom API");
        config.Description.ShouldBe("Custom API description");
        config.Version.ShouldBe("v2");
        config.RoutePrefix.ShouldBe("api-docs");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Enabled_CanBeSet()
    {
        // Arrange & Act
        var config = new SwaggerConfiguration { Enabled = false };

        // Assert
        config.Enabled.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Title_CanBeSet()
    {
        // Arrange & Act
        var config = new SwaggerConfiguration { Title = "Test API" };

        // Assert
        config.Title.ShouldBe("Test API");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Description_CanBeSet()
    {
        // Arrange & Act
        var config = new SwaggerConfiguration { Description = "Test Description" };

        // Assert
        config.Description.ShouldBe("Test Description");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Version_CanBeSet()
    {
        // Arrange & Act
        var config = new SwaggerConfiguration { Version = "v3" };

        // Assert
        config.Version.ShouldBe("v3");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void RoutePrefix_CanBeSet()
    {
        // Arrange & Act
        var config = new SwaggerConfiguration { RoutePrefix = "docs" };

        // Assert
        config.RoutePrefix.ShouldBe("docs");
    }
}
