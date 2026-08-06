using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Web.RestEndpoints.Security;

namespace Fdw.Web.RestEndpoints.Tests.Security;

/// <summary>
/// Tests for AdminEndpointBase variants.
/// </summary>
[Collection(nameof(RestEndpointsTestCollection))]
public sealed class AdminEndpointBaseTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EndpointWithoutRequestCanBeInstantiated()
    {
        // Arrange & Act
        var endpoint = new TestAdminEndpoint();

        // Assert
        endpoint.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EndpointWithoutRequestRouteIsAccessible()
    {
        // Arrange
        var endpoint = new TestAdminEndpoint();

        // Act
        var route = endpoint.TestRoute;

        // Assert
        route.ShouldBe("/test/admin");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EndpointWithoutRequestAdminPolicyDefaultsToConfigurationsWrite()
    {
        // Arrange
        var endpoint = new TestAdminEndpoint();

        // Act
        var policy = endpoint.TestAdminPolicy;

        // Assert
        policy.ShouldBe("configurations:write");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EndpointWithoutRequestRateLimitPolicyDefaultsToAdmin()
    {
        // Arrange
        var endpoint = new TestAdminEndpoint();

        // Act
        var policy = endpoint.TestRateLimitPolicy;

        // Assert
        policy.ShouldBe(RateLimitPolicyNames.Admin);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EndpointWithoutRequestEndpointSummaryDefaultsToEmpty()
    {
        // Arrange
        var endpoint = new TestAdminEndpoint();

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
        var endpoint = new TestAdminEndpoint();

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
        var endpoint = new TestAdminEndpoint();

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
        var endpoint = new TestAdminEndpointWithRequest();

        // Assert
        endpoint.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EndpointWithRequestRouteIsAccessible()
    {
        // Arrange
        var endpoint = new TestAdminEndpointWithRequest();

        // Act
        var route = endpoint.TestRoute;

        // Assert
        route.ShouldBe("/test/admin/config");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EndpointWithRequestAdminPolicyDefaultsToConfigurationsWrite()
    {
        // Arrange
        var endpoint = new TestAdminEndpointWithRequest();

        // Act
        var policy = endpoint.TestAdminPolicy;

        // Assert
        policy.ShouldBe("configurations:write");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EndpointWithRequestRateLimitPolicyDefaultsToAdmin()
    {
        // Arrange
        var endpoint = new TestAdminEndpointWithRequest();

        // Act
        var policy = endpoint.TestRateLimitPolicy;

        // Assert
        policy.ShouldBe(RateLimitPolicyNames.Admin);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EndpointWithRequestCanOverrideAdminPolicy()
    {
        // Arrange
        var endpoint = new TestAdminEndpointWithCustomPolicy();

        // Act
        var policy = endpoint.TestAdminPolicy;

        // Assert
        policy.ShouldBe("custom:admin:policy");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EndpointWithRequestCanOverrideRateLimitPolicy()
    {
        // Arrange
        var endpoint = new TestAdminEndpointWithCustomRateLimit();

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
        var endpoint = new TestAdminEndpointWithNoRateLimit();

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
        var endpoint = new TestAdminEndpointWithCustomSummary();

        // Act
        var summary = endpoint.TestEndpointSummary;

        // Assert
        summary.ShouldBe("Admin endpoint summary");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EndpointWithRequestCanOverrideEndpointDescription()
    {
        // Arrange
        var endpoint = new TestAdminEndpointWithCustomDescription();

        // Act
        var description = endpoint.TestEndpointDescription;

        // Assert
        description.ShouldBe("Admin endpoint description");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EndpointWithRequestCanOverrideEndpointTag()
    {
        // Arrange
        var endpoint = new TestAdminEndpointWithCustomTag();

        // Act
        var tag = endpoint.TestEndpointTag;

        // Assert
        tag.ShouldBe("AdminTag");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AdminPolicyFormatMatchesFdwConvention()
    {
        // Arrange
        var endpoint = new TestAdminEndpoint();

        // Act
        var policy = endpoint.TestAdminPolicy;

        // Assert
        // Policies are bare "{resource}:{action}" — the framework "fdw:" prefix was
        // removed from the authorization surface; per-tenant branding (OrgPrefix) is
        // applied at the DTO boundary, not in the policy name.
        policy.ShouldNotStartWith("fdw:");
        policy.ShouldEndWith(":write");
        policy.Split(':').Length.ShouldBe(2);
    }

    #region Test Implementations

    [ExcludeFromCodeCoverage]
    private sealed class TestAdminEndpoint : AdminEndpointBase<TestResponse>
    {
        protected override string Route => "/test/admin";

        public string TestRoute => Route;
        public string TestAdminPolicy => AdminPolicy;
        public string? TestRateLimitPolicy => RateLimitPolicy;
        public string TestEndpointSummary => EndpointSummary;
        public string TestEndpointDescription => EndpointDescription;
        public string? TestEndpointTag => EndpointTag;

        public override Task HandleAsync(CancellationToken ct) => Task.CompletedTask;
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestAdminEndpointWithRequest : AdminEndpointBase<TestRequest, TestResponse>
    {
        protected override string Route => "/test/admin/config";

        public string TestRoute => Route;
        public string TestAdminPolicy => AdminPolicy;
        public string? TestRateLimitPolicy => RateLimitPolicy;

        public override Task HandleAsync(TestRequest req, CancellationToken ct) => Task.CompletedTask;
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestAdminEndpointWithCustomPolicy : AdminEndpointBase<TestRequest, TestResponse>
    {
        protected override string Route => "/test/admin/custompolicy";
        protected override string AdminPolicy => "custom:admin:policy";

        public string TestAdminPolicy => AdminPolicy;

        public override Task HandleAsync(TestRequest req, CancellationToken ct) => Task.CompletedTask;
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestAdminEndpointWithCustomRateLimit : AdminEndpointBase<TestRequest, TestResponse>
    {
        protected override string Route => "/test/admin/customrate";
        protected override string? RateLimitPolicy => RateLimitPolicyNames.Premium;

        public string? TestRateLimitPolicy => RateLimitPolicy;

        public override Task HandleAsync(TestRequest req, CancellationToken ct) => Task.CompletedTask;
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestAdminEndpointWithNoRateLimit : AdminEndpointBase<TestRequest, TestResponse>
    {
        protected override string Route => "/test/admin/norate";
        protected override string? RateLimitPolicy => null;

        public string? TestRateLimitPolicy => RateLimitPolicy;

        public override Task HandleAsync(TestRequest req, CancellationToken ct) => Task.CompletedTask;
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestAdminEndpointWithCustomSummary : AdminEndpointBase<TestRequest, TestResponse>
    {
        protected override string Route => "/test/admin/summary";
        protected override string EndpointSummary => "Admin endpoint summary";

        public string TestEndpointSummary => EndpointSummary;

        public override Task HandleAsync(TestRequest req, CancellationToken ct) => Task.CompletedTask;
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestAdminEndpointWithCustomDescription : AdminEndpointBase<TestRequest, TestResponse>
    {
        protected override string Route => "/test/admin/description";
        protected override string EndpointDescription => "Admin endpoint description";

        public string TestEndpointDescription => EndpointDescription;

        public override Task HandleAsync(TestRequest req, CancellationToken ct) => Task.CompletedTask;
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestAdminEndpointWithCustomTag : AdminEndpointBase<TestRequest, TestResponse>
    {
        protected override string Route => "/test/admin/tag";
        protected override string? EndpointTag => "AdminTag";

        public string? TestEndpointTag => EndpointTag;

        public override Task HandleAsync(TestRequest req, CancellationToken ct) => Task.CompletedTask;
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestRequest
    {
        public string Action { get; set; } = string.Empty;
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestResponse
    {
        public string Result { get; set; } = string.Empty;
    }

    #endregion
}
