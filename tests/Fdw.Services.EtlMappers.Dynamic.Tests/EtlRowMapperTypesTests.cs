using Fdw.Services.EtlMappers;
using Fdw.Services.EtlMappers.Abstractions;
using Fdw.Services.EtlMappers.Dynamic;
using Fdw.Services.EtlMappers.Pooled;
using Fdw;
using Fdw.Services;

namespace Fdw.Services.EtlMappers.Dynamic.Tests.Legacy;

/// <summary>
/// Tests for the EtlRowMapperTypes TypeCollection.
/// Note: Pooled and Dynamic types use RestrictToCurrentCompilation = true,
/// so they are NOT auto-registered in the test assembly. These tests verify
/// the type instances directly rather than through the frozen collection.
/// </summary>
public class EtlRowMapperTypesTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ByNameReturnsNotFoundForUnknownType()
    {
        // Act
        var type = EtlRowMapperTypes.ByName("Unknown");

        // Assert
        type.ShouldNotBeNull();
        type.Name.ShouldBe("_Empty");
        type.ShouldBeSameAs(EtlRowMapperTypes.NotFound);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ByNameReturnsNotFoundForNullName()
    {
        // Act
        var type = EtlRowMapperTypes.ByName(null);

        // Assert
        type.ShouldBeSameAs(EtlRowMapperTypes.NotFound);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ByNameReturnsNotFoundForEmptyName()
    {
        // Act
        var type = EtlRowMapperTypes.ByName(string.Empty);

        // Assert
        type.ShouldBeSameAs(EtlRowMapperTypes.NotFound);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void NotFoundHasExpectedSentinelValues()
    {
        // Act
        var notFound = EtlRowMapperTypes.NotFound;

        // Assert
        notFound.ShouldNotBeNull();
        notFound.Name.ShouldBe("_Empty");
        notFound.EstimatedAllocationsPerRow.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void PooledTypeInstanceHasCorrectProperties()
    {
        // Arrange - create instance directly (RestrictToCurrentCompilation prevents auto-registration)
        var pooledType = new PooledDictionaryMapperType();

        // Assert
        pooledType.Name.ShouldBe("Pooled");
        pooledType.DisplayName.ShouldBe("Pooled Dictionary Mapper");
        pooledType.Description.ShouldBe("Zero-allocation mapper using dictionary pooling");
        pooledType.SectionName.ShouldBe("EtlMappers:Pooled");
        pooledType.EstimatedAllocationsPerRow.ShouldBe(0);
        pooledType.ConfigurationType.ShouldBe(typeof(PooledDictionaryMapperConfiguration));
        pooledType.FactoryType.ShouldBe(typeof(PooledDictionaryMapperFactory));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void DynamicTypeInstanceHasCorrectProperties()
    {
        // Arrange - create instance directly (RestrictToCurrentCompilation prevents auto-registration)
        var dynamicType = new DynamicStructMapperType();

        // Assert
        dynamicType.Name.ShouldBe("Dynamic");
        dynamicType.DisplayName.ShouldBe("Dynamic Struct Mapper");
        dynamicType.Description.ShouldBe("Mapper using compiled expression trees for field access");
        dynamicType.SectionName.ShouldBe("EtlMappers:Dynamic");
        dynamicType.EstimatedAllocationsPerRow.ShouldBe(1);
        dynamicType.ConfigurationType.ShouldBe(typeof(DynamicStructMapperConfiguration));
        dynamicType.FactoryType.ShouldBe(typeof(DynamicStructMapperFactory));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void PooledTypeImplementsIEtlRowMapperType()
    {
        // Arrange
        var pooledType = new PooledDictionaryMapperType();

        // Assert
        pooledType.ShouldBeAssignableTo<IEtlRowMapperType>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void DynamicTypeImplementsIEtlRowMapperType()
    {
        // Arrange
        var dynamicType = new DynamicStructMapperType();

        // Assert
        dynamicType.ShouldBeAssignableTo<IEtlRowMapperType>();
    }
}
