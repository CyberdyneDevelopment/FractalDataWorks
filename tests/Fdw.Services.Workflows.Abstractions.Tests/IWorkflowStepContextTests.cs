using Fdw.Services.Workflows.Abstractions;
using System;

namespace Fdw.Services.Workflows.Abstractions.Tests;

/// <summary>
/// Tests for IWorkflowStepContext interface contracts.
/// </summary>
public class IWorkflowStepContextTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IWorkflowStepContextInterfaceExists()
    {
        // Assert
        var type = typeof(IWorkflowStepContext);
        type.ShouldNotBeNull();
        type.IsInterface.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IWorkflowStepContextHasStepIdProperty()
    {
        // Assert
        var type = typeof(IWorkflowStepContext);
        var property = type.GetProperty("StepId");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(string));
        property.CanRead.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IWorkflowStepContextHasStepNameProperty()
    {
        // Assert
        var type = typeof(IWorkflowStepContext);
        var property = type.GetProperty("StepName");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(string));
        property.CanRead.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IWorkflowStepContextHasStartedAtProperty()
    {
        // Assert
        var type = typeof(IWorkflowStepContext);
        var property = type.GetProperty("StartedAt");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(DateTimeOffset));
        property.CanRead.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IWorkflowStepContextHasRetryCountProperty()
    {
        // Assert
        var type = typeof(IWorkflowStepContext);
        var property = type.GetProperty("RetryCount");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(int));
        property.CanRead.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IWorkflowStepContextHasIsCompensationProperty()
    {
        // Assert
        var type = typeof(IWorkflowStepContext);
        var property = type.GetProperty("IsCompensation");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(bool));
        property.CanRead.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void MockStepContextCanSetStepId()
    {
        // Arrange
        var mockStepContext = new Mock<IWorkflowStepContext>();
        mockStepContext.Setup(c => c.StepId).Returns("step-1");

        // Act
        var stepId = mockStepContext.Object.StepId;

        // Assert
        stepId.ShouldBe("step-1");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void MockStepContextCanSetStepName()
    {
        // Arrange
        var mockStepContext = new Mock<IWorkflowStepContext>();
        mockStepContext.Setup(c => c.StepName).Returns("ProcessData");

        // Act
        var stepName = mockStepContext.Object.StepName;

        // Assert
        stepName.ShouldBe("ProcessData");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void MockStepContextCanSetStartedAt()
    {
        // Arrange
        var startedAt = DateTimeOffset.UtcNow;
        var mockStepContext = new Mock<IWorkflowStepContext>();
        mockStepContext.Setup(c => c.StartedAt).Returns(startedAt);

        // Act
        var result = mockStepContext.Object.StartedAt;

        // Assert
        result.ShouldBe(startedAt);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void MockStepContextCanSetRetryCount()
    {
        // Arrange
        var mockStepContext = new Mock<IWorkflowStepContext>();
        mockStepContext.Setup(c => c.RetryCount).Returns(3);

        // Act
        var retryCount = mockStepContext.Object.RetryCount;

        // Assert
        retryCount.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void MockStepContextCanSetIsCompensation()
    {
        // Arrange
        var mockStepContext = new Mock<IWorkflowStepContext>();
        mockStepContext.Setup(c => c.IsCompensation).Returns(true);

        // Act
        var isCompensation = mockStepContext.Object.IsCompensation;

        // Assert
        isCompensation.ShouldBeTrue();
    }
}
