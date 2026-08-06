using Fdw.Collections;
using Fdw.Services.EtlMappers.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;

namespace Fdw.Services.EtlMappers.Abstractions.Tests;

/// <summary>
/// Tests for IEtlRowMapperType interfaces.
/// </summary>
public class IEtlRowMapperTypeTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IEtlRowMapperTypeInterfaceExists()
    {
        // Act
        var type = typeof(IEtlRowMapperType);

        // Assert
        type.ShouldNotBeNull();
        type.IsInterface.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IEtlRowMapperTypeInheritsFromITypeOption()
    {
        // Act
        var interfaces = typeof(IEtlRowMapperType).GetInterfaces();

        // Assert
        interfaces.ShouldContain(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ITypeOption<,>));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IEtlRowMapperTypeHasCorrectTypeOptionParameters()
    {
        // Act
        var baseInterface = typeof(IEtlRowMapperType).GetInterfaces()
            .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ITypeOption<,>));
        var typeArgs = baseInterface.GetGenericArguments();

        // Assert
        typeArgs.Length.ShouldBe(2);
        typeArgs[0].ShouldBe(typeof(Guid)); // TId is Guid
        typeArgs[1].ShouldBe(typeof(IEtlRowMapperType)); // TSelf is IEtlRowMapperType
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IEtlRowMapperTypeHasSectionNameProperty()
    {
        // Act
        var property = typeof(IEtlRowMapperType).GetProperty(nameof(IEtlRowMapperType.SectionName));

        // Assert
        property.ShouldNotBeNull();
        property.PropertyType.ShouldBe(typeof(string));
        property.CanRead.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IEtlRowMapperTypeHasDisplayNameProperty()
    {
        // Act
        var property = typeof(IEtlRowMapperType).GetProperty(nameof(IEtlRowMapperType.DisplayName));

        // Assert
        property.ShouldNotBeNull();
        property.PropertyType.ShouldBe(typeof(string));
        property.CanRead.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IEtlRowMapperTypeHasDescriptionProperty()
    {
        // Act
        var property = typeof(IEtlRowMapperType).GetProperty(nameof(IEtlRowMapperType.Description));

        // Assert
        property.ShouldNotBeNull();
        property.PropertyType.ShouldBe(typeof(string));
        property.CanRead.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IEtlRowMapperTypeHasConfigurationTypeProperty()
    {
        // Act
        var property = typeof(IEtlRowMapperType).GetProperty(nameof(IEtlRowMapperType.ConfigurationType));

        // Assert
        property.ShouldNotBeNull();
        property.PropertyType.ShouldBe(typeof(Type));
        property.CanRead.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IEtlRowMapperTypeHasFactoryTypeProperty()
    {
        // Act
        var property = typeof(IEtlRowMapperType).GetProperty(nameof(IEtlRowMapperType.FactoryType));

        // Assert
        property.ShouldNotBeNull();
        property.PropertyType.ShouldBe(typeof(Type));
        property.CanRead.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IEtlRowMapperTypeHasEstimatedAllocationsPerRowProperty()
    {
        // Act
        var property = typeof(IEtlRowMapperType).GetProperty(nameof(IEtlRowMapperType.EstimatedAllocationsPerRow));

        // Assert
        property.ShouldNotBeNull();
        property.PropertyType.ShouldBe(typeof(int));
        property.CanRead.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IEtlRowMapperTypeHasConfigureMethod()
    {
        // Act
        var method = typeof(IEtlRowMapperType).GetMethod(nameof(IEtlRowMapperType.Configure));

        // Assert
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(void));

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(3);
        parameters[0].ParameterType.ShouldBe(typeof(IServiceCollection));
        parameters[1].ParameterType.ShouldBe(typeof(IConfiguration));
        parameters[2].ParameterType.ShouldBe(typeof(ILoggerFactory));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IEtlRowMapperTypeHasRegisterMethod()
    {
        // Act
        var method = typeof(IEtlRowMapperType).GetMethod(nameof(IEtlRowMapperType.Register));

        // Assert
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(IServiceCollection));

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(2);
        parameters[0].ParameterType.ShouldBe(typeof(IServiceCollection));
        parameters[1].ParameterType.ShouldBe(typeof(ILoggerFactory));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IEtlRowMapperTypeHasRegisterFactoryMethod()
    {
        // Act
        var method = typeof(IEtlRowMapperType).GetMethod(nameof(IEtlRowMapperType.RegisterFactory));

        // Assert
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(void));

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(2);
        parameters[0].ParameterType.ShouldBe(typeof(IEtlRowMapperProvider));
        parameters[1].ParameterType.ShouldBe(typeof(IServiceProvider));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IEtlRowMapperTypeGenericInterfaceExists()
    {
        // Act
        var type = typeof(IEtlRowMapperType<,,>);

        // Assert
        type.ShouldNotBeNull();
        type.IsInterface.ShouldBeTrue();
        type.IsGenericTypeDefinition.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IEtlRowMapperTypeGenericInheritsFromNonGeneric()
    {
        // Act
        var interfaces = typeof(IEtlRowMapperType<,,>).GetInterfaces();

        // Assert
        interfaces.ShouldContain(typeof(IEtlRowMapperType));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IEtlRowMapperTypeGenericHasCorrectTypeParameters()
    {
        // Act
        var typeParameters = typeof(IEtlRowMapperType<,,>).GetGenericArguments();

        // Assert
        typeParameters.Length.ShouldBe(3);
        typeParameters[0].Name.ShouldBe("TMapper");
        typeParameters[1].Name.ShouldBe("TFactory");
        typeParameters[2].Name.ShouldBe("TConfiguration");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IEtlRowMapperTypeGenericHasCorrectConstraints()
    {
        // Act
        var typeParameters = typeof(IEtlRowMapperType<,,>).GetGenericArguments();
        var mapperParam = typeParameters[0];
        var factoryParam = typeParameters[1];
        var configParam = typeParameters[2];

        // Assert
        var mapperConstraints = mapperParam.GetGenericParameterConstraints();
        mapperConstraints.Length.ShouldBe(1);
        mapperConstraints[0].ShouldBe(typeof(IEtlRowMapper));

        var configConstraints = configParam.GetGenericParameterConstraints();
        configConstraints.Length.ShouldBe(1);
        configConstraints[0].ShouldBe(typeof(EtlRowMapperConfiguration));

        // Why: TConfiguration is constrained to EtlRowMapperConfiguration (a class),
        // which is a base-type constraint — it does NOT set the `class`
        // ReferenceTypeConstraint flag (that flag is only set for `where T : class`).
        // The base type itself enforces reference-type semantics.

        var factoryConstraints = factoryParam.GetGenericParameterConstraints();
        factoryConstraints.Length.ShouldBe(1);
        factoryConstraints[0].IsGenericType.ShouldBeTrue();
        factoryConstraints[0].GetGenericTypeDefinition().ShouldBe(typeof(IEtlRowMapperFactory<,>));
    }
}
