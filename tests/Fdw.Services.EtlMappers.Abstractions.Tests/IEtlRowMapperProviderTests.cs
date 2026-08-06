using Fdw.Results;
using Fdw.Services.EtlMappers.Abstractions;
using System.Reflection;

namespace Fdw.Services.EtlMappers.Abstractions.Tests;

/// <summary>
/// Tests for IEtlRowMapperProvider interface.
/// </summary>
public class IEtlRowMapperProviderTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IEtlRowMapperProviderInterfaceExists()
    {
        // Act
        var type = typeof(IEtlRowMapperProvider);

        // Assert
        type.ShouldNotBeNull();
        type.IsInterface.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IEtlRowMapperProviderHasCreateMethod()
    {
        // Act
        var method = typeof(IEtlRowMapperProvider).GetMethod(nameof(IEtlRowMapperProvider.Create));

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
    public void IEtlRowMapperProviderHasRegisterMethod()
    {
        // Act
        var method = typeof(IEtlRowMapperProvider).GetMethod(nameof(IEtlRowMapperProvider.Register));

        // Assert
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(void));

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(2);
        parameters[0].ParameterType.ShouldBe(typeof(string));
        parameters[0].Name.ShouldBe("serviceOptionType");
        parameters[1].ParameterType.ShouldBe(typeof(IEtlRowMapperFactory));
        parameters[1].Name.ShouldBe("factory");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IEtlRowMapperProviderHasDefaultMapperTypeProperty()
    {
        // Act
        var property = typeof(IEtlRowMapperProvider).GetProperty(nameof(IEtlRowMapperProvider.DefaultMapperType));

        // Assert
        property.ShouldNotBeNull();
        property.PropertyType.ShouldBe(typeof(string));
        property.CanRead.ShouldBeTrue();
        property.CanWrite.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IEtlRowMapperProviderHasCorrectNumberOfMembers()
    {
        // Act
        var methods = typeof(IEtlRowMapperProvider).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        var properties = typeof(IEtlRowMapperProvider).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        // Assert - 2 methods + 1 property getter = 3 total
        (methods.Length - properties.Length).ShouldBe(2); // 2 explicit methods
        properties.Length.ShouldBe(1); // 1 property
    }
}
