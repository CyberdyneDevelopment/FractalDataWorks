using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Workflows.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Fdw.Services.Workflows.Abstractions.Tests;

/// <summary>
/// Tests for IWorkflowProvider interface contracts.
/// </summary>
public class IWorkflowProviderTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IWorkflowProviderInterfaceExists()
    {
        // Assert
        var type = typeof(IWorkflowProvider);
        type.ShouldNotBeNull();
        type.IsInterface.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IWorkflowProviderHasGetByIdMethod()
    {
        // Assert — Get(Guid, CancellationToken) returns Task<IGenericResult<IGenericConfiguration>>
        var type = typeof(IWorkflowProvider);
        var method = type.GetMethod("Get", new[] { typeof(Guid), typeof(CancellationToken) });
        method.ShouldNotBeNull();
        method!.ReturnType.ShouldBe(typeof(Task<IGenericResult<IGenericConfiguration>>));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IWorkflowProviderHasGetByNameMethod()
    {
        // Assert — Get(string, CancellationToken) returns Task<IGenericResult<IGenericConfiguration>>
        var type = typeof(IWorkflowProvider);
        var method = type.GetMethod("Get", new[] { typeof(string), typeof(CancellationToken) });
        method.ShouldNotBeNull();
        method!.ReturnType.ShouldBe(typeof(Task<IGenericResult<IGenericConfiguration>>));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IWorkflowProviderHasGetAllMethod()
    {
        // Assert — Get(CancellationToken) returns Task<IGenericResult<IReadOnlyList<IGenericConfiguration>>>
        var type = typeof(IWorkflowProvider);
        var method = type.GetMethod("Get", new[] { typeof(CancellationToken) });
        method.ShouldNotBeNull();
        method!.ReturnType.ShouldBe(typeof(Task<IGenericResult<IReadOnlyList<IGenericConfiguration>>>));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task MockProviderCanGetById()
    {
        // Arrange
        var id = Guid.NewGuid();
        var mockConfig = Mock.Of<IGenericConfiguration>();
        var result = GenericResult<IGenericConfiguration>.Success(mockConfig);
        var mockProvider = new Mock<IWorkflowProvider>();
        mockProvider.Setup(p => p.Get(id, It.IsAny<CancellationToken>())).ReturnsAsync(result);

        // Act
        var retrievedResult = await mockProvider.Object.Get(id, TestContext.Current.CancellationToken);

        // Assert
        retrievedResult.IsSuccess.ShouldBeTrue();
        retrievedResult.Value.ShouldBe(mockConfig);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task MockProviderCanGetByName()
    {
        // Arrange
        var mockConfig = Mock.Of<IGenericConfiguration>();
        var result = GenericResult<IGenericConfiguration>.Success(mockConfig);
        var mockProvider = new Mock<IWorkflowProvider>();
        mockProvider.Setup(p => p.Get("TestWorkflow", It.IsAny<CancellationToken>())).ReturnsAsync(result);

        // Act
        var retrievedResult = await mockProvider.Object.Get("TestWorkflow", TestContext.Current.CancellationToken);

        // Assert
        retrievedResult.IsSuccess.ShouldBeTrue();
        retrievedResult.Value.ShouldBe(mockConfig);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task MockProviderCanGetAll()
    {
        // Arrange
        var configs = new List<IGenericConfiguration>
        {
            Mock.Of<IGenericConfiguration>(),
            Mock.Of<IGenericConfiguration>()
        };
        var result = GenericResult<IReadOnlyList<IGenericConfiguration>>.Success(configs);
        var mockProvider = new Mock<IWorkflowProvider>();
        mockProvider.Setup(p => p.Get(It.IsAny<CancellationToken>())).ReturnsAsync(result);

        // Act
        var retrievedResult = await mockProvider.Object.Get(TestContext.Current.CancellationToken);

        // Assert
        retrievedResult.IsSuccess.ShouldBeTrue();
        retrievedResult.Value.ShouldNotBeNull();
        retrievedResult.Value!.Count.ShouldBe(2);
    }
}
