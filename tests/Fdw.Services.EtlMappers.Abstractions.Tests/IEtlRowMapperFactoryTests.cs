using Fdw.Results;
using Fdw.Services.EtlMappers.Abstractions;
using System.Reflection;

namespace Fdw.Services.EtlMappers.Abstractions.Tests;

/// <summary>
/// Tests for IEtlRowMapperFactory interfaces.
/// </summary>
public class IEtlRowMapperFactoryTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IEtlRowMapperFactoryGenericInterfaceExists()
    {
        // Act
        var type = typeof(IEtlRowMapperFactory<,>);

        // Assert
        type.ShouldNotBeNull();
        type.IsInterface.ShouldBeTrue();
        type.IsGenericTypeDefinition.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IEtlRowMapperFactoryGenericHasCorrectTypeParameters()
    {
        // Act
        var typeParameters = typeof(IEtlRowMapperFactory<,>).GetGenericArguments();

        // Assert
        typeParameters.Length.ShouldBe(2);
        typeParameters[0].Name.ShouldBe("TMapper");
        typeParameters[1].Name.ShouldBe("TConfiguration");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IEtlRowMapperFactoryGenericHasCreateMethod()
    {
        // Act
        var method = typeof(IEtlRowMapperFactory<,>).GetMethod(nameof(IEtlRowMapperFactory<IEtlRowMapper, EtlRowMapperConfiguration>.Create));

        // Assert
        method.ShouldNotBeNull();
        method.ReturnType.IsGenericType.ShouldBeTrue();
        method.ReturnType.GetGenericTypeDefinition().ShouldBe(typeof(IGenericResult<>));

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(1);
        parameters[0].ParameterType.Name.ShouldBe("TConfiguration");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IEtlRowMapperFactoryNonGenericInterfaceExists()
    {
        // Act
        var type = typeof(IEtlRowMapperFactory);

        // Assert
        type.ShouldNotBeNull();
        type.IsInterface.ShouldBeTrue();
        type.IsGenericTypeDefinition.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IEtlRowMapperFactoryNonGenericHasCreateMethod()
    {
        // Act
        var method = typeof(IEtlRowMapperFactory).GetMethod(nameof(IEtlRowMapperFactory.Create));

        // Assert
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(IGenericResult<IEtlRowMapper>));

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(1);
        parameters[0].ParameterType.ShouldBe(typeof(EtlRowMapperConfiguration));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IEtlRowMapperFactoryGenericHasCorrectConstraints()
    {
        // Act
        var typeParameters = typeof(IEtlRowMapperFactory<,>).GetGenericArguments();
        var mapperParam = typeParameters[0];
        var configParam = typeParameters[1];

        // Assert
        var mapperConstraints = mapperParam.GetGenericParameterConstraints();
        mapperConstraints.Length.ShouldBe(1);
        mapperConstraints[0].ShouldBe(typeof(IEtlRowMapper));

        var configConstraints = configParam.GetGenericParameterConstraints();
        configConstraints.Length.ShouldBe(1);
        configConstraints[0].ShouldBe(typeof(EtlRowMapperConfiguration));
    }
}
