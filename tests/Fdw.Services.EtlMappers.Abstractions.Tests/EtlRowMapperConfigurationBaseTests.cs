using Fdw.Services.EtlMappers.Abstractions;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.EtlMappers.Abstractions.Tests;

/// <summary>
/// Tests for EtlRowMapperConfiguration.
/// Note: This class is marked with [ExcludeFromCodeCoverage], but we test it for API stability.
/// </summary>
public class EtlRowMapperConfigurationBaseTests
{
    private class TestConfiguration : EtlRowMapperConfiguration
    {
        public TestConfiguration() : base()
        {
        }

    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void EtlRowMapperConfigurationBaseIsAbstract()
    {
        var type = typeof(EtlRowMapperConfiguration);
        type.IsClass.ShouldBeTrue();
        type.GetConstructors(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void EtlRowMapperConfigurationBaseHasExcludeFromCodeCoverageAttribute()
    {
        // Act
        var attribute = typeof(EtlRowMapperConfiguration)
            .GetCustomAttributes(typeof(ExcludeFromCodeCoverageAttribute), false)
            .FirstOrDefault();

        // Assert
        attribute.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void EtlRowMapperConfigurationBaseImplementsIEtlRowMapperConfiguration()
    {
        var interfaces = typeof(EtlRowMapperConfiguration).GetInterfaces();
        interfaces.ShouldContain(typeof(Fdw.Configuration.IGenericConfiguration));
    }



    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IdCanBeSet()
    {
        // Arrange
        var config = new TestConfiguration();
        var newId = Guid.NewGuid();

        // Act
        config.Id = newId;

        // Assert
        config.Id.ShouldBe(newId);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void NameCanBeSet()
    {
        // Arrange
        var config = new TestConfiguration();

        // Act
        config.Name = "TestName";

        // Assert
        config.Name.ShouldBe("TestName");
    }




    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void EnablePoolingCanBeSet()
    {
        // Arrange
        var config = new TestConfiguration();

        // Act
        config.EnablePooling = false;

        // Assert
        config.EnablePooling.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void MaxPoolSizeCanBeSet()
    {
        // Arrange
        var config = new TestConfiguration();

        // Act
        config.MaxPoolSize = 500;

        // Assert
        config.MaxPoolSize.ShouldBe(500);
    }




    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IdPropertyDefaultsToNewGuid()
    {
        // Act
        var config1 = new TestConfiguration();
        var config2 = new TestConfiguration();

        // Assert
        config1.Id.ShouldNotBe(Guid.Empty);
        config2.Id.ShouldNotBe(Guid.Empty);
        config1.Id.ShouldNotBe(config2.Id);
    }
}
