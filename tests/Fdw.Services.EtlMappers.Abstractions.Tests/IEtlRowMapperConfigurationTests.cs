using Fdw.Configuration;
using Fdw.Services.EtlMappers.Abstractions;
using System.Reflection;

namespace Fdw.Services.EtlMappers.Abstractions.Tests;

/// <summary>
/// Tests for EtlRowMapperConfiguration interface.
/// </summary>
public class IEtlRowMapperConfigurationTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IEtlRowMapperConfigurationInterfaceExists()
    {
        var type = typeof(EtlRowMapperConfiguration);
        type.ShouldNotBeNull();
        type.IsClass.ShouldBeTrue();
        typeof(IGenericConfiguration).IsAssignableFrom(type).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IEtlRowMapperConfigurationInheritsFromIGenericConfiguration()
    {
        // Act
        var interfaces = typeof(EtlRowMapperConfiguration).GetInterfaces();

        // Assert
        interfaces.ShouldContain(typeof(IGenericConfiguration));
    }


    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IEtlRowMapperConfigurationHasEnablePoolingProperty()
    {
        // Act
        var property = typeof(EtlRowMapperConfiguration).GetProperty(nameof(EtlRowMapperConfiguration.EnablePooling));

        // Assert
        property.ShouldNotBeNull();
        property.PropertyType.ShouldBe(typeof(bool));
        property.CanRead.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IEtlRowMapperConfigurationHasMaxPoolSizeProperty()
    {
        // Act
        var property = typeof(EtlRowMapperConfiguration).GetProperty(nameof(EtlRowMapperConfiguration.MaxPoolSize));

        // Assert
        property.ShouldNotBeNull();
        property.PropertyType.ShouldBe(typeof(int));
        property.CanRead.ShouldBeTrue();
    }

}
