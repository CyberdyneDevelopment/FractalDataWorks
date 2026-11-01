using Fdw.Services.EtlMappers.Abstractions;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace Fdw.Services.EtlMappers.Abstractions.Tests;

/// <summary>
/// Tests for IEtlRowMapper interface.
/// </summary>
public class IEtlRowMapperTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IEtlRowMapperInterfaceExists()
    {
        // Act
        var type = typeof(IEtlRowMapper);

        // Assert
        type.ShouldNotBeNull();
        type.IsInterface.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IEtlRowMapperHasInitializeMethod()
    {
        // Act
        var method = typeof(IEtlRowMapper).GetMethod(nameof(IEtlRowMapper.Initialize));

        // Assert
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(void));

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(2);
        parameters[0].ParameterType.ShouldBe(typeof(IDataReader));
        parameters[1].ParameterType.Name.ShouldBe("IStorageContainer");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IEtlRowMapperHasMapRowMethod()
    {
        // Act
        var method = typeof(IEtlRowMapper).GetMethod(nameof(IEtlRowMapper.MapRow));

        // Assert
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(IDictionary<string, object?>));

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(1);
        parameters[0].ParameterType.ShouldBe(typeof(IDataReader));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IEtlRowMapperHasReturnRowMethod()
    {
        // Act
        var method = typeof(IEtlRowMapper).GetMethod(nameof(IEtlRowMapper.ReturnRow));

        // Assert
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(void));

        var parameters = method.GetParameters();
        parameters.Length.ShouldBe(1);
        parameters[0].ParameterType.ShouldBe(typeof(IDictionary<string, object?>));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IEtlRowMapperHasResetMethod()
    {
        // Act
        var method = typeof(IEtlRowMapper).GetMethod(nameof(IEtlRowMapper.Reset));

        // Assert
        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(void));
        method.GetParameters().Length.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IEtlRowMapperHasEstimatedAllocationsPerRowProperty()
    {
        // Act
        var property = typeof(IEtlRowMapper).GetProperty(nameof(IEtlRowMapper.EstimatedAllocationsPerRow));

        // Assert
        property.ShouldNotBeNull();
        property.PropertyType.ShouldBe(typeof(int));
        property.CanRead.ShouldBeTrue();
        property.CanWrite.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IEtlRowMapperHasIsInitializedProperty()
    {
        // Act
        var property = typeof(IEtlRowMapper).GetProperty(nameof(IEtlRowMapper.IsInitialized));

        // Assert
        property.ShouldNotBeNull();
        property.PropertyType.ShouldBe(typeof(bool));
        property.CanRead.ShouldBeTrue();
        property.CanWrite.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IEtlRowMapperHasCorrectNumberOfMembers()
    {
        // Act
        var methods = typeof(IEtlRowMapper).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        var properties = typeof(IEtlRowMapper).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        // Assert - 4 methods + 2 property getters = 6 total
        (methods.Length - properties.Length).ShouldBe(4); // 4 explicit methods
        properties.Length.ShouldBe(2); // 2 properties
    }
}
