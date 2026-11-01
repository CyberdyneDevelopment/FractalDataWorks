using Fdw.Web.RestEndpoints.Configuration;

namespace Fdw.Web.RestEndpoints.Tests.Configuration;

[Collection(nameof(RestEndpointsTestCollection))]
public class EndpointDefaultsTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void DefaultCacheDurationSeconds_IsCorrect()
    {
        // Arrange & Act & Assert
        EndpointDefaults.DefaultCacheDurationSeconds.ShouldBe(300);
    }
}
