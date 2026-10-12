using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Moq;
using System;

namespace Fdw.Abstractions.Tests;

/// <summary>
/// Tests for IServiceFactory interface contracts.
/// </summary>
public class IServiceFactoryTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IServiceFactoryInterfaceExists()
    {
        // Assert
        var type = typeof(IServiceFactory);
        type.ShouldNotBeNull();
        type.IsInterface.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IServiceFactoryHasCreateGenericMethod()
    {
        // Assert
        var type = typeof(IServiceFactory);
        var methods = type.GetMethods();
        var genericCreateMethod = Array.Find(methods, m =>
            m.Name == "Create" &&
            m.IsGenericMethod &&
            m.GetParameters().Length == 1);

        genericCreateMethod.ShouldNotBeNull();
        genericCreateMethod!.IsGenericMethod.ShouldBeTrue();
        genericCreateMethod.ReturnType.IsGenericType.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IServiceFactoryHasCreateNonGenericMethod()
    {
        // Assert
        var type = typeof(IServiceFactory);
        var methods = type.GetMethods();
        var createMethod = Array.Find(methods, m =>
            m.Name == "Create" &&
            !m.IsGenericMethod &&
            m.GetParameters().Length == 1 &&
            m.GetParameters()[0].ParameterType == typeof(IGenericConfiguration));

        createMethod.ShouldNotBeNull();
        createMethod!.ReturnType.ShouldBe(typeof(IGenericResult<IGenericService>));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IServiceFactoryGenericInterfaceExists()
    {
        // Assert
        var type = typeof(IServiceFactory<>);
        type.ShouldNotBeNull();
        type.IsInterface.ShouldBeTrue();
        type.IsGenericTypeDefinition.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IServiceFactoryGenericInheritsFromBase()
    {
        // Assert
        var type = typeof(IServiceFactory<>);
        var baseInterface = type.GetInterface("IServiceFactory");
        baseInterface.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IServiceFactoryGenericHasCreateMethod()
    {
        // Assert
        var type = typeof(IServiceFactory<>);
        var method = type.GetMethod("Create", new[] { typeof(IGenericConfiguration) });

        method.ShouldNotBeNull();
        method!.ReturnType.IsGenericType.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IServiceFactoryGenericIsCovariant()
    {
        // Assert
        var type = typeof(IServiceFactory<>);
        var typeParam = type.GetGenericArguments()[0];
        var attributes = typeParam.GenericParameterAttributes;

        (attributes & System.Reflection.GenericParameterAttributes.Covariant).ShouldNotBe(System.Reflection.GenericParameterAttributes.None);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IServiceFactoryWithConfigurationInterfaceExists()
    {
        // Assert
        var type = typeof(IServiceFactory<,>);
        type.ShouldNotBeNull();
        type.IsInterface.ShouldBeTrue();
        type.IsGenericTypeDefinition.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IServiceFactoryWithConfigurationInheritsFromGeneric()
    {
        // Assert
        var type = typeof(IServiceFactory<,>);
        var interfaces = type.GetInterfaces();
        var hasGenericBase = Array.Exists(interfaces, i =>
            i.IsGenericType &&
            i.GetGenericTypeDefinition() == typeof(IServiceFactory<>));

        hasGenericBase.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IServiceFactoryWithConfigurationHasCreateMethod()
    {
        // Assert
        var type = typeof(IServiceFactory<,>);
        var method = type.GetMethod("Create");

        method.ShouldNotBeNull();
        method!.ReturnType.IsGenericType.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IServiceFactoryWithConfigurationServiceTypeIsCovariant()
    {
        // Assert
        var type = typeof(IServiceFactory<,>);
        var typeParams = type.GetGenericArguments();
        var serviceTypeParam = typeParams[0];
        var attributes = serviceTypeParam.GenericParameterAttributes;

        (attributes & System.Reflection.GenericParameterAttributes.Covariant).ShouldNotBe(System.Reflection.GenericParameterAttributes.None);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IServiceFactoryWithConfigurationConfigTypeIsContravariant()
    {
        // Assert
        var type = typeof(IServiceFactory<,>);
        var typeParams = type.GetGenericArguments();
        var configTypeParam = typeParams[1];
        var attributes = configTypeParam.GenericParameterAttributes;

        (attributes & System.Reflection.GenericParameterAttributes.Contravariant).ShouldNotBe(System.Reflection.GenericParameterAttributes.None);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IServiceFactoryWithConfigurationHasIGenericConfigurationConstraint()
    {
        // Assert
        var type = typeof(IServiceFactory<,>);
        var typeParams = type.GetGenericArguments();
        var configTypeParam = typeParams[1];
        var constraints = configTypeParam.GetGenericParameterConstraints();

        constraints.ShouldNotBeEmpty();
        Array.Exists(constraints, c => c == typeof(IGenericConfiguration)).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void MockFactoryCanCreateGenericService()
    {
        // Arrange
        var mockFactory = new Mock<IServiceFactory>();
        var mockConfig = Mock.Of<IGenericConfiguration>();
        var mockService = Mock.Of<IGenericService>();
        var mockResult = Mock.Of<IGenericResult<IGenericService>>(r => r.Value == mockService);

        mockFactory
            .Setup(f => f.Create<IGenericService>(It.IsAny<IGenericConfiguration>()))
            .Returns(mockResult);

        // Act
        var result = mockFactory.Object.Create<IGenericService>(mockConfig);

        // Assert
        result.ShouldBe(mockResult);
        result.Value.ShouldBe(mockService);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void MockFactoryCanCreateNonGenericService()
    {
        // Arrange
        var mockFactory = new Mock<IServiceFactory>();
        var mockConfig = Mock.Of<IGenericConfiguration>();
        var mockService = Mock.Of<IGenericService>();
        var mockResult = Mock.Of<IGenericResult<IGenericService>>(r => r.Value == mockService);

        mockFactory
            .Setup(f => f.Create(It.IsAny<IGenericConfiguration>()))
            .Returns(mockResult);

        // Act
        var result = mockFactory.Object.Create(mockConfig);

        // Assert
        result.ShouldBe(mockResult);
        result.Value.ShouldBe(mockService);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void MockGenericFactoryCanCreateService()
    {
        // Arrange
        var mockFactory = new Mock<IServiceFactory<IGenericService>>();
        var mockConfig = Mock.Of<IGenericConfiguration>();
        var mockService = Mock.Of<IGenericService>();
        var mockResult = Mock.Of<IGenericResult<IGenericService>>(r => r.Value == mockService);

        mockFactory
            .Setup(f => f.Create(It.IsAny<IGenericConfiguration>()))
            .Returns(mockResult);

        // Act
        var result = mockFactory.Object.Create(mockConfig);

        // Assert
        result.ShouldBe(mockResult);
        result.Value.ShouldBe(mockService);
    }
}
