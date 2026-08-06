using Fdw.Configuration;
using Fdw.ServiceTypes;
using Fdw.Services.Workflows.Abstractions;
using System;

namespace Fdw.Services.Workflows.Abstractions.Tests;

/// <summary>
/// Tests for IWorkflowType interface contracts.
/// </summary>
public class IWorkflowTypeTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IWorkflowTypeInterfaceExists()
    {
        // Assert
        var type = typeof(IWorkflowType);
        type.ShouldNotBeNull();
        type.IsInterface.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IWorkflowTypeInheritsFromIServiceType()
    {
        // Assert
        var type = typeof(IWorkflowType);
        var baseInterface = type.GetInterface(nameof(IServiceType));
        baseInterface.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IWorkflowTypeHasWorkflowEngineProperty()
    {
        // Assert
        var type = typeof(IWorkflowType);
        var property = type.GetProperty("WorkflowEngine");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(string));
        property.CanRead.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IWorkflowTypeHasExecutorTypeProperty()
    {
        // Assert
        var type = typeof(IWorkflowType);
        var property = type.GetProperty("ExecutorType");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(Type));
        property.CanRead.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IWorkflowTypeHasSupportsCompensationProperty()
    {
        // Assert
        var type = typeof(IWorkflowType);
        var property = type.GetProperty("SupportsCompensation");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(bool));
        property.CanRead.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IWorkflowTypeHasSupportsParallelExecutionProperty()
    {
        // Assert
        var type = typeof(IWorkflowType);
        var property = type.GetProperty("SupportsParallelExecution");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(bool));
        property.CanRead.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IWorkflowTypeHasSupportsPersistenceProperty()
    {
        // Assert
        var type = typeof(IWorkflowType);
        var property = type.GetProperty("SupportsPersistence");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(bool));
        property.CanRead.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IWorkflowTypeHasSupportsConditionalBranchingProperty()
    {
        // Assert
        var type = typeof(IWorkflowType);
        var property = type.GetProperty("SupportsConditionalBranching");
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(bool));
        property.CanRead.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IWorkflowTypeGenericInterfaceExists()
    {
        // Assert
        var type = typeof(IWorkflowType<,,>);
        type.ShouldNotBeNull();
        type.IsInterface.ShouldBeTrue();
        type.IsGenericTypeDefinition.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IWorkflowTypeGenericInheritsFromBaseInterface()
    {
        // Assert
        var type = typeof(IWorkflowType<,,>);
        var baseInterface = type.GetInterface(nameof(IWorkflowType));
        baseInterface.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IWorkflowTypeGenericInheritsFromIServiceType()
    {
        // Assert
        var type = typeof(IWorkflowType<,,>);
        var interfaces = type.GetInterfaces();
        interfaces.ShouldContain(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IServiceType<,,,>));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IWorkflowTypeGenericHasThreeTypeParameters()
    {
        // Assert
        var type = typeof(IWorkflowType<,,>);
        var typeParams = type.GetGenericArguments();
        typeParams.Length.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IWorkflowTypeGenericFirstTypeParameterIsService()
    {
        // Arrange
        var type = typeof(IWorkflowType<,,>);
        var typeParam = type.GetGenericArguments()[0];
        var constraints = typeParam.GetGenericParameterConstraints();

        // Assert
        constraints.ShouldContain(t => t == typeof(IGenericWorkflow));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IWorkflowTypeGenericSecondTypeParameterIsConfiguration()
    {
        // Arrange
        var type = typeof(IWorkflowType<,,>);
        var typeParam = type.GetGenericArguments()[1];
        var constraints = typeParam.GetGenericParameterConstraints();

        // Assert
        constraints.ShouldContain(t => t == typeof(IGenericConfiguration));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IWorkflowTypeGenericThirdTypeParameterIsFactory()
    {
        // Arrange
        var type = typeof(IWorkflowType<,,>);
        var typeParam = type.GetGenericArguments()[2];
        var constraints = typeParam.GetGenericParameterConstraints();

        // Assert
        constraints.ShouldContain(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IWorkflowFactory<,>));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void MockWorkflowTypeCanSetWorkflowEngine()
    {
        // Arrange
        var mockWorkflowType = new Mock<IWorkflowType>();
        mockWorkflowType.Setup(t => t.WorkflowEngine).Returns("Saga");

        // Act
        var engine = mockWorkflowType.Object.WorkflowEngine;

        // Assert
        engine.ShouldBe("Saga");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void MockWorkflowTypeCanSetExecutorType()
    {
        // Arrange
        var executorType = typeof(string);
        var mockWorkflowType = new Mock<IWorkflowType>();
        mockWorkflowType.Setup(t => t.ExecutorType).Returns(executorType);

        // Act
        var result = mockWorkflowType.Object.ExecutorType;

        // Assert
        result.ShouldBe(executorType);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void MockWorkflowTypeCanSetSupportsCompensation()
    {
        // Arrange
        var mockWorkflowType = new Mock<IWorkflowType>();
        mockWorkflowType.Setup(t => t.SupportsCompensation).Returns(true);

        // Act
        var supportsCompensation = mockWorkflowType.Object.SupportsCompensation;

        // Assert
        supportsCompensation.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void MockWorkflowTypeCanSetSupportsParallelExecution()
    {
        // Arrange
        var mockWorkflowType = new Mock<IWorkflowType>();
        mockWorkflowType.Setup(t => t.SupportsParallelExecution).Returns(true);

        // Act
        var supportsParallelExecution = mockWorkflowType.Object.SupportsParallelExecution;

        // Assert
        supportsParallelExecution.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void MockWorkflowTypeCanSetSupportsPersistence()
    {
        // Arrange
        var mockWorkflowType = new Mock<IWorkflowType>();
        mockWorkflowType.Setup(t => t.SupportsPersistence).Returns(true);

        // Act
        var supportsPersistence = mockWorkflowType.Object.SupportsPersistence;

        // Assert
        supportsPersistence.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void MockWorkflowTypeCanSetSupportsConditionalBranching()
    {
        // Arrange
        var mockWorkflowType = new Mock<IWorkflowType>();
        mockWorkflowType.Setup(t => t.SupportsConditionalBranching).Returns(true);

        // Act
        var supportsConditionalBranching = mockWorkflowType.Object.SupportsConditionalBranching;

        // Assert
        supportsConditionalBranching.ShouldBeTrue();
    }
}
