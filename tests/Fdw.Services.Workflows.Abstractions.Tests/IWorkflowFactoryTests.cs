using Fdw.Configuration;
using Fdw.Abstractions;
using Fdw.Services.Workflows.Abstractions;

namespace Fdw.Services.Workflows.Abstractions.Tests;

/// <summary>
/// Tests for IWorkflowFactory interface contracts.
/// </summary>
public class IWorkflowFactoryTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IWorkflowFactoryInterfaceExists()
    {
        // Assert
        var type = typeof(IWorkflowFactory);
        type.ShouldNotBeNull();
        type.IsInterface.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IWorkflowFactoryIsMarkerInterface()
    {
        // Assert
        var type = typeof(IWorkflowFactory);
        var methods = type.GetMethods();
        var properties = type.GetProperties();

        // Marker interface has no members
        methods.Length.ShouldBe(0);
        properties.Length.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IWorkflowFactoryGenericInterfaceExists()
    {
        // Assert
        var type = typeof(IWorkflowFactory<,>);
        type.ShouldNotBeNull();
        type.IsInterface.ShouldBeTrue();
        type.IsGenericTypeDefinition.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IWorkflowFactoryGenericInheritsFromMarkerInterface()
    {
        // Assert
        var type = typeof(IWorkflowFactory<,>);
        var baseInterface = type.GetInterface(nameof(IWorkflowFactory));
        baseInterface.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IWorkflowFactoryGenericInheritsFromIServiceFactory()
    {
        // Assert
        var type = typeof(IWorkflowFactory<,>);
        var interfaces = type.GetInterfaces();
        interfaces.ShouldContain(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IServiceFactory<,>));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IWorkflowFactoryGenericHasTwoTypeParameters()
    {
        // Assert
        var type = typeof(IWorkflowFactory<,>);
        var typeParams = type.GetGenericArguments();
        typeParams.Length.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IWorkflowFactoryGenericFirstTypeParameterIsService()
    {
        // Arrange
        var type = typeof(IWorkflowFactory<,>);
        var typeParam = type.GetGenericArguments()[0];
        var constraints = typeParam.GetGenericParameterConstraints();

        // Assert
        constraints.ShouldContain(t => t == typeof(IGenericWorkflow));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IWorkflowFactoryGenericSecondTypeParameterIsConfiguration()
    {
        // Arrange
        var type = typeof(IWorkflowFactory<,>);
        var typeParam = type.GetGenericArguments()[1];
        var constraints = typeParam.GetGenericParameterConstraints();

        // Assert
        constraints.ShouldContain(t => t == typeof(IGenericConfiguration));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void MockWorkflowFactoryCanBeCreated()
    {
        // Arrange & Act
        var mockFactory = new Mock<IWorkflowFactory>();

        // Assert
        mockFactory.Object.ShouldNotBeNull();
        mockFactory.Object.ShouldBeAssignableTo<IWorkflowFactory>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void MockGenericWorkflowFactoryCanBeCreated()
    {
        // Arrange & Act
        var mockFactory = new Mock<IWorkflowFactory<IGenericWorkflow, IGenericConfiguration>>();

        // Assert
        mockFactory.Object.ShouldNotBeNull();
        mockFactory.Object.ShouldBeAssignableTo<IWorkflowFactory<IGenericWorkflow, IGenericConfiguration>>();
        mockFactory.Object.ShouldBeAssignableTo<IWorkflowFactory>();
    }
}
