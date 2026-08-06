using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Web.RestEndpoints.Security;

namespace Fdw.Web.RestEndpoints.Tests.Security;

/// <summary>
/// Tests for PublicEndpointBase variants.
/// </summary>
[Collection(nameof(RestEndpointsTestCollection))]
public sealed class PublicEndpointBaseTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EndpointWithoutRequestCanBeInstantiated()
    {
        // Arrange & Act
        var endpoint = new TestPublicEndpoint();

        // Assert
        endpoint.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EndpointWithoutRequestRouteIsAccessible()
    {
        // Arrange
        var endpoint = new TestPublicEndpoint();

        // Act
        var route = endpoint.TestRoute;

        // Assert
        route.ShouldBe("/test/public");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EndpointWithoutRequestRateLimitPolicyDefaultsToStandard()
    {
        // Arrange
        var endpoint = new TestPublicEndpoint();

        // Act
        var policy = endpoint.TestRateLimitPolicy;

        // Assert
        policy.ShouldBe(RateLimitPolicyNames.Standard);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EndpointWithoutRequestEndpointSummaryDefaultsToEmpty()
    {
        // Arrange
        var endpoint = new TestPublicEndpoint();

        // Act
        var summary = endpoint.TestEndpointSummary;

        // Assert
        summary.ShouldBe(string.Empty);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EndpointWithoutRequestEndpointDescriptionDefaultsToEmpty()
    {
        // Arrange
        var endpoint = new TestPublicEndpoint();

        // Act
        var description = endpoint.TestEndpointDescription;

        // Assert
        description.ShouldBe(string.Empty);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EndpointWithoutRequestEndpointTagDefaultsToNull()
    {
        // Arrange
        var endpoint = new TestPublicEndpoint();

        // Act
        var tag = endpoint.TestEndpointTag;

        // Assert
        tag.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EndpointWithRequestCanBeInstantiated()
    {
        // Arrange & Act
        var endpoint = new TestPublicEndpointWithRequest();

        // Assert
        endpoint.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EndpointWithRequestRouteIsAccessible()
    {
        // Arrange
        var endpoint = new TestPublicEndpointWithRequest();

        // Act
        var route = endpoint.TestRoute;

        // Assert
        route.ShouldBe("/test/public/search");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EndpointWithRequestRateLimitPolicyDefaultsToStandard()
    {
        // Arrange
        var endpoint = new TestPublicEndpointWithRequest();

        // Act
        var policy = endpoint.TestRateLimitPolicy;

        // Assert
        policy.ShouldBe(RateLimitPolicyNames.Standard);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EndpointWithRequestCanOverrideRateLimitPolicy()
    {
        // Arrange
        var endpoint = new TestPublicEndpointWithCustomRateLimit();

        // Act
        var policy = endpoint.TestRateLimitPolicy;

        // Assert
        policy.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EndpointWithRequestCanOverrideEndpointSummary()
    {
        // Arrange
        var endpoint = new TestPublicEndpointWithCustomSummary();

        // Act
        var summary = endpoint.TestEndpointSummary;

        // Assert
        summary.ShouldBe("Custom summary");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EndpointWithRequestCanOverrideEndpointDescription()
    {
        // Arrange
        var endpoint = new TestPublicEndpointWithCustomDescription();

        // Act
        var description = endpoint.TestEndpointDescription;

        // Assert
        description.ShouldBe("Custom description");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EndpointWithRequestCanOverrideEndpointTag()
    {
        // Arrange
        var endpoint = new TestPublicEndpointWithCustomTag();

        // Act
        var tag = endpoint.TestEndpointTag;

        // Assert
        tag.ShouldBe("CustomTag");
    }

    #region Test Implementations

    [ExcludeFromCodeCoverage]
    private sealed class TestPublicEndpoint : PublicEndpointBase<TestResponse>
    {
        protected override string Route => "/test/public";

        public string TestRoute => Route;
        public string? TestRateLimitPolicy => RateLimitPolicy;
        public string TestEndpointSummary => EndpointSummary;
        public string TestEndpointDescription => EndpointDescription;
        public string? TestEndpointTag => EndpointTag;

        public override Task HandleAsync(CancellationToken ct) => Task.CompletedTask;
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestPublicEndpointWithRequest : PublicEndpointBase<TestRequest, TestResponse>
    {
        protected override string Route => "/test/public/search";

        public string TestRoute => Route;
        public string? TestRateLimitPolicy => RateLimitPolicy;

        public override Task HandleAsync(TestRequest req, CancellationToken ct) => Task.CompletedTask;
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestPublicEndpointWithCustomRateLimit : PublicEndpointBase<TestRequest, TestResponse>
    {
        protected override string Route => "/test/public/custom";
        protected override string? RateLimitPolicy => null;

        public string? TestRateLimitPolicy => RateLimitPolicy;

        public override Task HandleAsync(TestRequest req, CancellationToken ct) => Task.CompletedTask;
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestPublicEndpointWithCustomSummary : PublicEndpointBase<TestRequest, TestResponse>
    {
        protected override string Route => "/test/public/summary";
        protected override string EndpointSummary => "Custom summary";

        public string TestEndpointSummary => EndpointSummary;

        public override Task HandleAsync(TestRequest req, CancellationToken ct) => Task.CompletedTask;
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestPublicEndpointWithCustomDescription : PublicEndpointBase<TestRequest, TestResponse>
    {
        protected override string Route => "/test/public/description";
        protected override string EndpointDescription => "Custom description";

        public string TestEndpointDescription => EndpointDescription;

        public override Task HandleAsync(TestRequest req, CancellationToken ct) => Task.CompletedTask;
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestPublicEndpointWithCustomTag : PublicEndpointBase<TestRequest, TestResponse>
    {
        protected override string Route => "/test/public/tag";
        protected override string? EndpointTag => "CustomTag";

        public string? TestEndpointTag => EndpointTag;

        public override Task HandleAsync(TestRequest req, CancellationToken ct) => Task.CompletedTask;
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestRequest
    {
        public string Query { get; set; } = string.Empty;
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestResponse
    {
        public string Result { get; set; } = string.Empty;
    }

    #endregion
}
