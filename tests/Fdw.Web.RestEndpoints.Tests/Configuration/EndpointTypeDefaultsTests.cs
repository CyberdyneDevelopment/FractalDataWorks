using Fdw.Web.Http.Abstractions.EndPoints;
using Fdw.Web.RestEndpoints.Configuration;

namespace Fdw.Web.RestEndpoints.Tests.Configuration;

[Collection(nameof(RestEndpointsTestCollection))]
public class EndpointTypeDefaultsTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void CrudEndpointHasCorrectDefaults()
    {
        // Arrange & Act
        var sut = new CrudEndpoint();

        // Assert
        sut.SecurityMethodName.ShouldBe("JWT");
        sut.RateLimitPolicyName.ShouldBe("FixedWindow");
        sut.TimeoutMs.ShouldBe(30000);
        sut.MaxBodySize.ShouldBe(10485760);
        sut.RequiresAuthentication.ShouldBeTrue();
        sut.AllowedRoles.ShouldBe(new[] { "User", "Admin" });
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void QueryEndpointHasCorrectDefaults()
    {
        // Arrange & Act
        var sut = new QueryEndpoint();

        // Assert
        sut.SecurityMethodName.ShouldBe("ApiKey");
        sut.RateLimitPolicyName.ShouldBe("SlidingWindow");
        sut.TimeoutMs.ShouldBe(15000);
        sut.MaxBodySize.ShouldBe(1048576);
        sut.RequiresAuthentication.ShouldBeFalse();
        sut.AllowedRoles.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void CommandEndpointHasCorrectDefaults()
    {
        // Arrange & Act
        var sut = new CommandEndpoint();

        // Assert
        sut.SecurityMethodName.ShouldBe("JWT");
        sut.RateLimitPolicyName.ShouldBe("TokenBucket");
        sut.TimeoutMs.ShouldBe(60000);
        sut.MaxBodySize.ShouldBe(10485760);
        sut.RequiresAuthentication.ShouldBeTrue();
        sut.AllowedRoles.ShouldBe(new[] { "Admin" });
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void HealthEndpointHasCorrectDefaults()
    {
        // Arrange & Act
        var sut = new HealthEndpoint();

        // Assert
        sut.SecurityMethodName.ShouldBe("None");
        sut.RateLimitPolicyName.ShouldBe("Concurrency");
        sut.TimeoutMs.ShouldBe(5000);
        sut.MaxBodySize.ShouldBe(1024);
        sut.RequiresAuthentication.ShouldBeFalse();
        sut.AllowedRoles.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void FileEndpointHasCorrectDefaults()
    {
        // Arrange & Act
        var sut = new FileEndpoint();

        // Assert
        sut.SecurityMethodName.ShouldBe("JWT");
        sut.RateLimitPolicyName.ShouldBe("Concurrency");
        sut.TimeoutMs.ShouldBe(180000);
        sut.MaxBodySize.ShouldBe(104857600);
        sut.RequiresAuthentication.ShouldBeTrue();
        sut.AllowedRoles.ShouldBe(new[] { "User", "Admin" });
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EventEndpointHasCorrectDefaults()
    {
        // Arrange & Act
        var sut = new EventEndpoint();

        // Assert
        sut.SecurityMethodName.ShouldBe("ApiKey");
        sut.RateLimitPolicyName.ShouldBe("TokenBucket");
        sut.TimeoutMs.ShouldBe(30000);
        sut.MaxBodySize.ShouldBe(10485760);
        sut.RequiresAuthentication.ShouldBeTrue();
        sut.AllowedRoles.ShouldBe(new[] { "System", "Admin" });
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void GetDefaultsForTypeReturnsSettingsFromEndpointType()
    {
        // Arrange
        var endpointType = new CrudEndpoint();

        // Act
        var settings = EndpointDefaults.GetDefaultsForType(endpointType);

        // Assert
        settings.SecurityMethodName.ShouldBe(endpointType.SecurityMethodName);
        settings.RateLimitPolicyName.ShouldBe(endpointType.RateLimitPolicyName);
        settings.TimeoutMs.ShouldBe(endpointType.TimeoutMs);
        settings.MaxBodySize.ShouldBe(endpointType.MaxBodySize);
        settings.RequireAuthentication.ShouldBe(endpointType.RequiresAuthentication);
        settings.AllowedRoles.ShouldBe(endpointType.AllowedRoles);
    }
}
