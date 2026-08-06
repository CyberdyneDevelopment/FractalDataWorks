using Fdw.Configuration;
using Fdw.Abstractions;
using Fdw.Orchestration.Workflows.Abstractions;
using Fdw.Results;
using Fdw.Services.Workflows.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fdw.Services.Workflows.Abstractions.Tests;

/// <summary>
/// Tests for IGenericWorkflow interface contracts.
/// </summary>
public class IGenericWorkflowTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IGenericWorkflowInterfaceExists()
    {
        // Assert
        var type = typeof(IGenericWorkflow);
        type.ShouldNotBeNull();
        type.IsInterface.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IGenericWorkflowInheritsFromIDisposable()
    {
        // Assert
        var type = typeof(IGenericWorkflow);
        var baseInterface = type.GetInterface(nameof(IDisposable));
        baseInterface.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IGenericWorkflowInheritsFromIGenericService()
    {
        // Assert
        var type = typeof(IGenericWorkflow);
        var baseInterface = type.GetInterface(nameof(IGenericService));
        baseInterface.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IGenericWorkflowHasExecuteMethodWithWorkflow()
    {
        // Assert
        var type = typeof(IGenericWorkflow);
        var method = type.GetMethod("Execute", new[] { typeof(IWorkflow), typeof(CancellationToken) });
        method.ShouldNotBeNull();
        method!.ReturnType.ShouldBe(typeof(Task<IGenericResult<IWorkflowExecutionResult>>));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IGenericWorkflowHasExecuteMethodWithContext()
    {
        // Assert
        var type = typeof(IGenericWorkflow);
        var method = type.GetMethod("Execute", new[] { typeof(IWorkflow), typeof(IWorkflowServiceExecutionContext), typeof(CancellationToken) });
        method.ShouldNotBeNull();
        method!.ReturnType.ShouldBe(typeof(Task<IGenericResult<IWorkflowExecutionResult>>));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IGenericWorkflowHasValidateMethod()
    {
        // Assert
        var type = typeof(IGenericWorkflow);
        var method = type.GetMethod("Validate");
        method.ShouldNotBeNull();
        method!.ReturnType.ShouldBe(typeof(Task<IGenericResult>));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IGenericWorkflowHasCompensateMethod()
    {
        // Assert
        var type = typeof(IGenericWorkflow);
        var method = type.GetMethod("Compensate");
        method.ShouldNotBeNull();
        method!.ReturnType.ShouldBe(typeof(Task<IGenericResult>));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task MockWorkflowCanExecuteWithWorkflow()
    {
        // Arrange
        var mockWorkflow = Mock.Of<IWorkflow>();
        var mockExecutionResult = Mock.Of<IWorkflowExecutionResult>();
        var result = GenericResult<IWorkflowExecutionResult>.Success(mockExecutionResult);
        var mockService = new Mock<IGenericWorkflow>();
        mockService.Setup(s => s.Execute(mockWorkflow, It.IsAny<CancellationToken>())).ReturnsAsync(result);

        // Act
        var executionResult = await mockService.Object.Execute(mockWorkflow, CancellationToken.None);

        // Assert
        executionResult.IsSuccess.ShouldBeTrue();
        executionResult.Value.ShouldBe(mockExecutionResult);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task MockWorkflowCanExecuteWithContext()
    {
        // Arrange
        var mockWorkflow = Mock.Of<IWorkflow>();
        var mockContext = Mock.Of<IWorkflowServiceExecutionContext>();
        var mockExecutionResult = Mock.Of<IWorkflowExecutionResult>();
        var result = GenericResult<IWorkflowExecutionResult>.Success(mockExecutionResult);
        var mockService = new Mock<IGenericWorkflow>();
        mockService.Setup(s => s.Execute(mockWorkflow, mockContext, It.IsAny<CancellationToken>())).ReturnsAsync(result);

        // Act
        var executionResult = await mockService.Object.Execute(mockWorkflow, mockContext, CancellationToken.None);

        // Assert
        executionResult.IsSuccess.ShouldBeTrue();
        executionResult.Value.ShouldBe(mockExecutionResult);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task MockWorkflowCanValidate()
    {
        // Arrange
        var mockWorkflow = Mock.Of<IWorkflow>();
        var result = GenericResult.Success();
        var mockService = new Mock<IGenericWorkflow>();
        mockService.Setup(s => s.Validate(mockWorkflow, It.IsAny<CancellationToken>())).ReturnsAsync(result);

        // Act
        var validationResult = await mockService.Object.Validate(mockWorkflow, CancellationToken.None);

        // Assert
        validationResult.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task MockWorkflowCanCompensate()
    {
        // Arrange
        var executionId = "exec-123";
        var result = GenericResult.Success();
        var mockService = new Mock<IGenericWorkflow>();
        mockService.Setup(s => s.Compensate(executionId, It.IsAny<CancellationToken>())).ReturnsAsync(result);

        // Act
        var compensationResult = await mockService.Object.Compensate(executionId, CancellationToken.None);

        // Assert
        compensationResult.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IGenericWorkflowGenericInterfaceExists()
    {
        // Assert
        var type = typeof(IGenericWorkflow<>);
        type.ShouldNotBeNull();
        type.IsInterface.ShouldBeTrue();
        type.IsGenericTypeDefinition.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IGenericWorkflowGenericInheritsFromBase()
    {
        // Assert
        var type = typeof(IGenericWorkflow<>);
        var baseInterface = type.GetInterface(nameof(IGenericWorkflow));
        baseInterface.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IGenericWorkflowGenericHasConfigurationProperty()
    {
        // Assert
        var type = typeof(IGenericWorkflow<>);
        var property = type.GetProperty("Configuration");
        property.ShouldNotBeNull();
        property!.CanRead.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IGenericWorkflowGenericConstrainsTypeParameter()
    {
        // Arrange
        var type = typeof(IGenericWorkflow<>);
        var typeParam = type.GetGenericArguments()[0];
        var constraints = typeParam.GetGenericParameterConstraints();

        // Assert
        constraints.ShouldNotBeEmpty();
        constraints.ShouldContain(t => t == typeof(IGenericConfiguration));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void MockGenericWorkflowCanGetConfiguration()
    {
        // Arrange
        var mockConfig = Mock.Of<IGenericConfiguration>();
        var mockService = new Mock<IGenericWorkflow<IGenericConfiguration>>();
        mockService.Setup(s => s.Configuration).Returns(mockConfig);

        // Act
        var config = mockService.Object.Configuration;

        // Assert
        config.ShouldBe(mockConfig);
    }
}
