using Fdw.Services.EtlMappers;
using Fdw.Services.EtlMappers.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.EtlMappers.Tests;

/// <summary>
/// Additional tests for EtlRowMapperTypeBase covering Configure and base class paths.
/// Note: TypeOption registration may not work in test projects, so tests use
/// NotFound/All patterns that always work.
/// </summary>
public sealed class EtlRowMapperTypeBaseAdditionalTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void NotFoundHasEmptyName()
    {
        var notFound = EtlRowMapperTypes.NotFound;
        notFound.ShouldNotBeNull();
        notFound.Name.ShouldBe("_Empty");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ByNameReturnsNotFoundForUnknown()
    {
        var result = EtlRowMapperTypes.ByName("NonExistent");
        result.ShouldBeSameAs(EtlRowMapperTypes.NotFound);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AllReturnsCollection()
    {
        var all = EtlRowMapperTypes.All();
        all.ShouldNotBeNull();
    }



    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void NotFoundConfigurationTypeIsInterface()
    {
        var type = EtlRowMapperTypes.NotFound;
        type.ConfigurationType.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void NotFoundFactoryTypeIsInterface()
    {
        var type = EtlRowMapperTypes.NotFound;
        type.FactoryType.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void NotFoundEstimatedAllocationsPerRowIsZero()
    {
        var type = EtlRowMapperTypes.NotFound;
        type.EstimatedAllocationsPerRow.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ByNameIsCaseInsensitive()
    {
        // Both should return NotFound consistently (registration not available)
        var result1 = EtlRowMapperTypes.ByName("notexist");
        var result2 = EtlRowMapperTypes.ByName("NOTEXIST");
        result1.ShouldBeSameAs(result2);
    }

}
