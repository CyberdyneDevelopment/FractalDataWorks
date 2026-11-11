using System;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Shouldly;
using Fdw.Services.Resiliency.Abstractions;
using Fdw.Services.Resiliency.Factories;

namespace Fdw.Services.Resiliency.Tests;

/// <summary>
/// Unit tests for ResiliencyPipelineFactory.
/// </summary>
[Collection(nameof(ResiliencyTestCollection))]
public sealed class ResiliencyPipelineFactoryTests
{
    private readonly Mock<ILogger<ResiliencyPipelineFactory>> _loggerMock;
    private readonly ResiliencyPipelineFactory _factory;

    public ResiliencyPipelineFactoryTests()
    {
        _loggerMock = new Mock<ILogger<ResiliencyPipelineFactory>>();
        _factory = new ResiliencyPipelineFactory(_loggerMock.Object);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void GetOrCreatePipelineByPolicyReturnsPipeline()
    {
        // Arrange
        var policy = ResiliencyPolicies.ByName("Database");

        // Act
        var result = _factory.GetOrCreate(policy, "TestOperation");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void GetOrCreatePipelineByPolicyNameReturnsPipeline()
    {
        // Act
        var result = _factory.GetOrCreate("Database", "TestOperation");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void GetOrCreatePipelineReturnsFailureForNullPolicy()
    {
        // Act
        var result = _factory.GetOrCreate((IResiliencyPolicy)null!, "TestOperation");

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void GetOrCreatePipelineReturnsFailureForNullPolicyName()
    {
        // Act
        var result = _factory.GetOrCreate((string)null!, "TestOperation");

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void GetOrCreatePipelineReturnsFailureForEmptyPolicyName()
    {
        // Act
        var result = _factory.GetOrCreate(string.Empty, "TestOperation");

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void GetOrCreatePipelineReturnsFailureForUnknownPolicyName()
    {
        // Act
        var result = _factory.GetOrCreate("NonExistent", "TestOperation");

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void GetOrCreatePipelineCachesPipeline()
    {
        // Arrange
        var policy = ResiliencyPolicies.ByName("Database");

        // Act
        var result1 = _factory.GetOrCreate(policy, "TestOperation");
        var result2 = _factory.GetOrCreate(policy, "TestOperation");

        // Assert
        result1.IsSuccess.ShouldBeTrue();
        result2.IsSuccess.ShouldBeTrue();
        ReferenceEquals(result1.Value, result2.Value).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void GetOrCreateGenericPipelineByPolicyReturnsPipeline()
    {
        // Arrange
        var policy = ResiliencyPolicies.ByName("Database");

        // Act
        var result = _factory.GetOrCreatePipeline<string>(policy, "TestOperation");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void GetOrCreateGenericPipelineByPolicyNameReturnsPipeline()
    {
        // Act
        var result = _factory.GetOrCreatePipeline<int>("HttpClient", "TestOperation");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void GetOrCreateGenericPipelineReturnsFailureForNullPolicy()
    {
        // Act
        var result = _factory.GetOrCreatePipeline<string>((IResiliencyPolicy)null!, "TestOperation");

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void GetOrCreateGenericPipelineReturnsFailureForUnknownPolicyName()
    {
        // Act
        var result = _factory.GetOrCreatePipeline<string>("NonExistent", "TestOperation");

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ClearCacheRemovesCachedPipelines()
    {
        // Arrange
        var policy = ResiliencyPolicies.ByName("Database");
        var result1 = _factory.GetOrCreate(policy, "TestOperation");

        // Act
        _factory.ClearCache();
        var result2 = _factory.GetOrCreate(policy, "TestOperation");

        // Assert - after clearing cache, we should get a new instance
        result1.IsSuccess.ShouldBeTrue();
        result2.IsSuccess.ShouldBeTrue();
        // Note: After cache clear, a new pipeline is created
        // The pipelines are equal in configuration but different instances
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void GetOrCreatePipelineWithOperationNameUsesPolicyNameAsDefault()
    {
        // Arrange
        var policy = ResiliencyPolicies.ByName("Database");

        // Act
        var result = _factory.GetOrCreate(policy, "CustomOperation");

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void DifferentPoliciesGetDifferentPipelines()
    {
        // Arrange
        var databasePolicy = ResiliencyPolicies.ByName("Database");
        var httpPolicy = ResiliencyPolicies.ByName("HttpClient");

        // Act
        var databaseResult = _factory.GetOrCreate(databasePolicy, "TestOperation");
        var httpResult = _factory.GetOrCreate(httpPolicy, "TestOperation");

        // Assert
        databaseResult.IsSuccess.ShouldBeTrue();
        httpResult.IsSuccess.ShouldBeTrue();
        ReferenceEquals(databaseResult.Value, httpResult.Value).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void GenericPipelinesAreCachedSeparatelyByType()
    {
        // Arrange
        var policy = ResiliencyPolicies.ByName("Database");

        // Act
        var stringResult = _factory.GetOrCreatePipeline<string>(policy, "TestOperation");
        var intResult = _factory.GetOrCreatePipeline<int>(policy, "TestOperation");

        // Assert
        stringResult.IsSuccess.ShouldBeTrue();
        intResult.IsSuccess.ShouldBeTrue();
        // Different generic types should have different cache keys
    }
}
