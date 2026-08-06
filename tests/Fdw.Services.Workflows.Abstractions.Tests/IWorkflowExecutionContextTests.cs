using Fdw.Services.Workflows.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Fdw.Services.Workflows.Abstractions.Tests;

/// <summary>
/// Tests for IWorkflowServiceExecutionContext interface contracts.
/// </summary>
public class IWorkflowServiceExecutionContextTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IWorkflowServiceExecutionContextInterfaceExists()
    {
        // Assert
        var type = typeof(IWorkflowServiceExecutionContext);
        type.ShouldNotBeNull();
        type.IsInterface.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IWorkflowServiceExecutionContextHasExecutionIdProperty()
    {
        // Assert
        var type = typeof(IWorkflowServiceExecutionContext);
        var property = type.GetProperty("ExecutionId");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(string));
        property.CanRead.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IWorkflowServiceExecutionContextHasCorrelationIdProperty()
    {
        // Assert
        var type = typeof(IWorkflowServiceExecutionContext);
        var property = type.GetProperty("CorrelationId");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(string));
        property.CanRead.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IWorkflowServiceExecutionContextHasStartedAtProperty()
    {
        // Assert
        var type = typeof(IWorkflowServiceExecutionContext);
        var property = type.GetProperty("StartedAt");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(DateTimeOffset));
        property.CanRead.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IWorkflowServiceExecutionContextHasCancellationTokenProperty()
    {
        // Assert
        var type = typeof(IWorkflowServiceExecutionContext);
        var property = type.GetProperty("CancellationToken");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(CancellationToken));
        property.CanRead.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IWorkflowServiceExecutionContextHasMetadataProperty()
    {
        // Assert
        var type = typeof(IWorkflowServiceExecutionContext);
        var property = type.GetProperty("Metadata");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(IReadOnlyDictionary<string, object?>));
        property.CanRead.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IWorkflowServiceExecutionContextHasCurrentStepProperty()
    {
        // Assert
        var type = typeof(IWorkflowServiceExecutionContext);
        var property = type.GetProperty("CurrentStep");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(IWorkflowStepContext));
        property.CanRead.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void MockContextCanSetExecutionId()
    {
        // Arrange
        var mockContext = new Mock<IWorkflowServiceExecutionContext>();
        mockContext.Setup(c => c.ExecutionId).Returns("exec-123");

        // Act
        var executionId = mockContext.Object.ExecutionId;

        // Assert
        executionId.ShouldBe("exec-123");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void MockContextCanSetCorrelationId()
    {
        // Arrange
        var mockContext = new Mock<IWorkflowServiceExecutionContext>();
        mockContext.Setup(c => c.CorrelationId).Returns("corr-456");

        // Act
        var correlationId = mockContext.Object.CorrelationId;

        // Assert
        correlationId.ShouldBe("corr-456");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void MockContextCorrelationIdCanBeNull()
    {
        // Arrange
        var mockContext = new Mock<IWorkflowServiceExecutionContext>();
        mockContext.Setup(c => c.CorrelationId).Returns((string?)null);

        // Act
        var correlationId = mockContext.Object.CorrelationId;

        // Assert
        correlationId.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void MockContextCanSetStartedAt()
    {
        // Arrange
        var startedAt = DateTimeOffset.UtcNow;
        var mockContext = new Mock<IWorkflowServiceExecutionContext>();
        mockContext.Setup(c => c.StartedAt).Returns(startedAt);

        // Act
        var result = mockContext.Object.StartedAt;

        // Assert
        result.ShouldBe(startedAt);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void MockContextCanSetCancellationToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var mockContext = new Mock<IWorkflowServiceExecutionContext>();
        mockContext.Setup(c => c.CancellationToken).Returns(cts.Token);

        // Act
        var token = mockContext.Object.CancellationToken;

        // Assert
        token.ShouldBe(cts.Token);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void MockContextCanSetMetadata()
    {
        // Arrange
        var metadata = new Dictionary<string, object?>
        {
            ["key1"] = "value1",
            ["key2"] = 42
        };
        var mockContext = new Mock<IWorkflowServiceExecutionContext>();
        mockContext.Setup(c => c.Metadata).Returns(metadata);

        // Act
        var result = mockContext.Object.Metadata;

        // Assert
        result.ShouldBe(metadata);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void MockContextCanSetCurrentStep()
    {
        // Arrange
        var mockStep = Mock.Of<IWorkflowStepContext>();
        var mockContext = new Mock<IWorkflowServiceExecutionContext>();
        mockContext.Setup(c => c.CurrentStep).Returns(mockStep);

        // Act
        var currentStep = mockContext.Object.CurrentStep;

        // Assert
        currentStep.ShouldBe(mockStep);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void MockContextCurrentStepCanBeNull()
    {
        // Arrange
        var mockContext = new Mock<IWorkflowServiceExecutionContext>();
        mockContext.Setup(c => c.CurrentStep).Returns((IWorkflowStepContext?)null);

        // Act
        var currentStep = mockContext.Object.CurrentStep;

        // Assert
        currentStep.ShouldBeNull();
    }
}
