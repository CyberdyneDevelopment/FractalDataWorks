using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Web.RestEndpoints.Security;

namespace Fdw.Web.RestEndpoints.Tests.Security;

/// <summary>
/// Tests for ProtectedEndpointBase variants.
/// </summary>
[Collection(nameof(RestEndpointsTestCollection))]
public sealed class ProtectedEndpointBaseTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EndpointWithoutRequestCanBeInstantiated()
    {
        // Arrange & Act
        var endpoint = new TestProtectedEndpoint();

        // Assert
        endpoint.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EndpointWithoutRequestRouteIsAccessible()
    {
        // Arrange
        var endpoint = new TestProtectedEndpoint();

        // Act
        var route = endpoint.TestRoute;

        // Assert
        route.ShouldBe("/test/protected");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EndpointWithoutRequestResourceNameIsAccessible()
    {
        // Arrange
        var endpoint = new TestProtectedEndpoint();

        // Act
        var resourceName = endpoint.TestResourceName;

        // Assert
        resourceName.ShouldBe("connections");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EndpointWithoutRequestReadPolicyDerivedFromResourceName()
    {
        // Arrange
        var endpoint = new TestProtectedEndpoint();

        // Act
        var policy = endpoint.TestReadPolicy;

        // Assert
        policy.ShouldBe("connections:read");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EndpointWithoutRequestEndpointSummaryDefaultsToEmpty()
    {
        // Arrange
        var endpoint = new TestProtectedEndpoint();

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
        var endpoint = new TestProtectedEndpoint();

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
        var endpoint = new TestProtectedEndpoint();

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
        var endpoint = new TestProtectedEndpointWithRequest();

        // Assert
        endpoint.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EndpointWithRequestRouteIsAccessible()
    {
        // Arrange
        var endpoint = new TestProtectedEndpointWithRequest();

        // Act
        var route = endpoint.TestRoute;

        // Assert
        route.ShouldBe("/test/protected/data");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EndpointWithRequestResourceNameIsAccessible()
    {
        // Arrange
        var endpoint = new TestProtectedEndpointWithRequest();

        // Act
        var resourceName = endpoint.TestResourceName;

        // Assert
        resourceName.ShouldBe("datastores");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EndpointWithRequestReadPolicyDerivedFromResourceName()
    {
        // Arrange
        var endpoint = new TestProtectedEndpointWithRequest();

        // Act
        var policy = endpoint.TestReadPolicy;

        // Assert
        policy.ShouldBe("datastores:read");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EndpointWithRequestCanOverrideReadPolicy()
    {
        // Arrange
        var endpoint = new TestProtectedEndpointWithCustomPolicy();

        // Act
        var policy = endpoint.TestReadPolicy;

        // Assert
        policy.ShouldBe("custom:policy:name");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EndpointWithRequestCanOverrideEndpointSummary()
    {
        // Arrange
        var endpoint = new TestProtectedEndpointWithCustomSummary();

        // Act
        var summary = endpoint.TestEndpointSummary;

        // Assert
        summary.ShouldBe("Protected endpoint summary");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EndpointWithRequestCanOverrideEndpointDescription()
    {
        // Arrange
        var endpoint = new TestProtectedEndpointWithCustomDescription();

        // Act
        var description = endpoint.TestEndpointDescription;

        // Assert
        description.ShouldBe("Protected endpoint description");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EndpointWithRequestCanOverrideEndpointTag()
    {
        // Arrange
        var endpoint = new TestProtectedEndpointWithCustomTag();

        // Act
        var tag = endpoint.TestEndpointTag;

        // Assert
        tag.ShouldBe("ProtectedTag");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ReadPolicyFormatMatchesFdwConvention()
    {
        // Arrange
        var endpoint = new TestProtectedEndpoint();

        // Act
        var policy = endpoint.TestReadPolicy;

        // Assert — policies are bare "{resource}:{action}"; the "fdw:" prefix was removed from
        // the authorization surface (per-tenant OrgPrefix is applied at the DTO boundary instead).
        policy.ShouldNotStartWith("fdw:");
        policy.ShouldEndWith(":read");
        policy.Split(':').Length.ShouldBe(2);
    }

    #region Test Implementations

    [ExcludeFromCodeCoverage]
    private sealed class TestProtectedEndpoint : ProtectedEndpointBase<TestResponse>
    {
        protected override string Route => "/test/protected";
        protected override string ResourceName => "connections";

        public string TestRoute => Route;
        public string TestResourceName => ResourceName;
        public string TestReadPolicy => ReadPolicy;
        public string TestEndpointSummary => EndpointSummary;
        public string TestEndpointDescription => EndpointDescription;
        public string? TestEndpointTag => EndpointTag;

        public override Task HandleAsync(CancellationToken ct) => Task.CompletedTask;
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestProtectedEndpointWithRequest : ProtectedEndpointBase<TestRequest, TestResponse>
    {
        protected override string Route => "/test/protected/data";
        protected override string ResourceName => "datastores";

        public string TestRoute => Route;
        public string TestResourceName => ResourceName;
        public string TestReadPolicy => ReadPolicy;

        public override Task HandleAsync(TestRequest req, CancellationToken ct) => Task.CompletedTask;
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestProtectedEndpointWithCustomPolicy : ProtectedEndpointBase<TestRequest, TestResponse>
    {
        protected override string Route => "/test/protected/custom";
        protected override string ResourceName => "custom";
        protected override string ReadPolicy => "custom:policy:name";

        public string TestReadPolicy => ReadPolicy;

        public override Task HandleAsync(TestRequest req, CancellationToken ct) => Task.CompletedTask;
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestProtectedEndpointWithCustomSummary : ProtectedEndpointBase<TestRequest, TestResponse>
    {
        protected override string Route => "/test/protected/summary";
        protected override string ResourceName => "resources";
        protected override string EndpointSummary => "Protected endpoint summary";

        public string TestEndpointSummary => EndpointSummary;

        public override Task HandleAsync(TestRequest req, CancellationToken ct) => Task.CompletedTask;
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestProtectedEndpointWithCustomDescription : ProtectedEndpointBase<TestRequest, TestResponse>
    {
        protected override string Route => "/test/protected/description";
        protected override string ResourceName => "resources";
        protected override string EndpointDescription => "Protected endpoint description";

        public string TestEndpointDescription => EndpointDescription;

        public override Task HandleAsync(TestRequest req, CancellationToken ct) => Task.CompletedTask;
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestProtectedEndpointWithCustomTag : ProtectedEndpointBase<TestRequest, TestResponse>
    {
        protected override string Route => "/test/protected/tag";
        protected override string ResourceName => "resources";
        protected override string? EndpointTag => "ProtectedTag";

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
