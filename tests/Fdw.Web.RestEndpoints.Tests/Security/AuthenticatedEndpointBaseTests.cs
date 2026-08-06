using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Web.RestEndpoints.Security;

namespace Fdw.Web.RestEndpoints.Tests.Security;

/// <summary>
/// Tests for AuthenticatedEndpointBase variants.
/// </summary>
[Collection(nameof(RestEndpointsTestCollection))]
public sealed class AuthenticatedEndpointBaseTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EndpointWithoutRequestCanBeInstantiated()
    {
        // Arrange & Act
        var endpoint = new TestAuthenticatedEndpoint();

        // Assert
        endpoint.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EndpointWithoutRequestRouteIsAccessible()
    {
        // Arrange
        var endpoint = new TestAuthenticatedEndpoint();

        // Act
        var route = endpoint.TestRoute;

        // Assert
        route.ShouldBe("/test/authenticated");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EndpointWithoutRequestRateLimitPolicyDefaultsToAuthenticated()
    {
        // Arrange
        var endpoint = new TestAuthenticatedEndpoint();

        // Act
        var policy = endpoint.TestRateLimitPolicy;

        // Assert
        policy.ShouldBe(RateLimitPolicyNames.Authenticated);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EndpointWithoutRequestEndpointSummaryDefaultsToEmpty()
    {
        // Arrange
        var endpoint = new TestAuthenticatedEndpoint();

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
        var endpoint = new TestAuthenticatedEndpoint();

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
        var endpoint = new TestAuthenticatedEndpoint();

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
        var endpoint = new TestAuthenticatedEndpointWithRequest();

        // Assert
        endpoint.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EndpointWithRequestRouteIsAccessible()
    {
        // Arrange
        var endpoint = new TestAuthenticatedEndpointWithRequest();

        // Act
        var route = endpoint.TestRoute;

        // Assert
        route.ShouldBe("/test/authenticated/data");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EndpointWithRequestRateLimitPolicyDefaultsToAuthenticated()
    {
        // Arrange
        var endpoint = new TestAuthenticatedEndpointWithRequest();

        // Act
        var policy = endpoint.TestRateLimitPolicy;

        // Assert
        policy.ShouldBe(RateLimitPolicyNames.Authenticated);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EndpointWithRequestCanOverrideRateLimitPolicy()
    {
        // Arrange
        var endpoint = new TestAuthenticatedEndpointWithCustomRateLimit();

        // Act
        var policy = endpoint.TestRateLimitPolicy;

        // Assert
        policy.ShouldBe(RateLimitPolicyNames.Premium);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EndpointWithRequestCanDisableRateLimitPolicy()
    {
        // Arrange
        var endpoint = new TestAuthenticatedEndpointWithNoRateLimit();

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
        var endpoint = new TestAuthenticatedEndpointWithCustomSummary();

        // Act
        var summary = endpoint.TestEndpointSummary;

        // Assert
        summary.ShouldBe("Authenticated endpoint summary");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EndpointWithRequestCanOverrideEndpointDescription()
    {
        // Arrange
        var endpoint = new TestAuthenticatedEndpointWithCustomDescription();

        // Act
        var description = endpoint.TestEndpointDescription;

        // Assert
        description.ShouldBe("Authenticated endpoint description");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EndpointWithRequestCanOverrideEndpointTag()
    {
        // Arrange
        var endpoint = new TestAuthenticatedEndpointWithCustomTag();

        // Act
        var tag = endpoint.TestEndpointTag;

        // Assert
        tag.ShouldBe("AuthenticatedTag");
    }

    #region Test Implementations

    [ExcludeFromCodeCoverage]
    private sealed class TestAuthenticatedEndpoint : AuthenticatedEndpointBase<TestResponse>
    {
        protected override string Route => "/test/authenticated";

        public string TestRoute => Route;
        public string? TestRateLimitPolicy => RateLimitPolicy;
        public string TestEndpointSummary => EndpointSummary;
        public string TestEndpointDescription => EndpointDescription;
        public string? TestEndpointTag => EndpointTag;

        public override Task HandleAsync(CancellationToken ct) => Task.CompletedTask;
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestAuthenticatedEndpointWithRequest : AuthenticatedEndpointBase<TestRequest, TestResponse>
    {
        protected override string Route => "/test/authenticated/data";

        public string TestRoute => Route;
        public string? TestRateLimitPolicy => RateLimitPolicy;

        public override Task HandleAsync(TestRequest req, CancellationToken ct) => Task.CompletedTask;
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestAuthenticatedEndpointWithCustomRateLimit : AuthenticatedEndpointBase<TestRequest, TestResponse>
    {
        protected override string Route => "/test/authenticated/premium";
        protected override string? RateLimitPolicy => RateLimitPolicyNames.Premium;

        public string? TestRateLimitPolicy => RateLimitPolicy;

        public override Task HandleAsync(TestRequest req, CancellationToken ct) => Task.CompletedTask;
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestAuthenticatedEndpointWithNoRateLimit : AuthenticatedEndpointBase<TestRequest, TestResponse>
    {
        protected override string Route => "/test/authenticated/nolimit";
        protected override string? RateLimitPolicy => null;

        public string? TestRateLimitPolicy => RateLimitPolicy;

        public override Task HandleAsync(TestRequest req, CancellationToken ct) => Task.CompletedTask;
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestAuthenticatedEndpointWithCustomSummary : AuthenticatedEndpointBase<TestRequest, TestResponse>
    {
        protected override string Route => "/test/authenticated/summary";
        protected override string EndpointSummary => "Authenticated endpoint summary";

        public string TestEndpointSummary => EndpointSummary;

        public override Task HandleAsync(TestRequest req, CancellationToken ct) => Task.CompletedTask;
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestAuthenticatedEndpointWithCustomDescription : AuthenticatedEndpointBase<TestRequest, TestResponse>
    {
        protected override string Route => "/test/authenticated/description";
        protected override string EndpointDescription => "Authenticated endpoint description";

        public string TestEndpointDescription => EndpointDescription;

        public override Task HandleAsync(TestRequest req, CancellationToken ct) => Task.CompletedTask;
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestAuthenticatedEndpointWithCustomTag : AuthenticatedEndpointBase<TestRequest, TestResponse>
    {
        protected override string Route => "/test/authenticated/tag";
        protected override string? EndpointTag => "AuthenticatedTag";

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
