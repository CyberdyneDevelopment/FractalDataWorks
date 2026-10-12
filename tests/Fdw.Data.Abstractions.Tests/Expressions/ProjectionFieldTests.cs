using Fdw.Data.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.Abstractions.Tests.Expressions;

public sealed class ProjectionFieldTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorInitializesPropertiesCorrectly()
    {
        // Arrange & Act
        var field = new ProjectionField
        {
            PropertyName = "TestProperty",
            Alias = "TestAlias"
        };

        // Assert
        field.PropertyName.ShouldBe("TestProperty");
        field.Alias.ShouldBe("TestAlias");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void CanCreateWithoutAlias()
    {
        // Arrange & Act
        var field = new ProjectionField
        {
            PropertyName = "TestProperty"
        };

        // Assert
        field.PropertyName.ShouldBe("TestProperty");
        field.Alias.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void CanCreateWithNullAlias()
    {
        // Arrange & Act
        var field = new ProjectionField
        {
            PropertyName = "TestProperty",
            Alias = null
        };

        // Assert
        field.PropertyName.ShouldBe("TestProperty");
        field.Alias.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void RecordEqualityWorksCorrectly()
    {
        // Arrange
        var field1 = new ProjectionField
        {
            PropertyName = "Name",
            Alias = "FullName"
        };

        var field2 = new ProjectionField
        {
            PropertyName = "Name",
            Alias = "FullName"
        };

        // Act & Assert
        field1.ShouldBe(field2);
        (field1 == field2).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void RecordInequalityWorksForDifferentPropertyNames()
    {
        // Arrange
        var field1 = new ProjectionField
        {
            PropertyName = "Name",
            Alias = "FullName"
        };

        var field2 = new ProjectionField
        {
            PropertyName = "DifferentName",
            Alias = "FullName"
        };

        // Act & Assert
        field1.ShouldNotBe(field2);
        (field1 != field2).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void RecordInequalityWorksForDifferentAliases()
    {
        // Arrange
        var field1 = new ProjectionField
        {
            PropertyName = "Name",
            Alias = "FullName"
        };

        var field2 = new ProjectionField
        {
            PropertyName = "Name",
            Alias = "DifferentAlias"
        };

        // Act & Assert
        field1.ShouldNotBe(field2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void RecordEqualityWorksWithNullAliases()
    {
        // Arrange
        var field1 = new ProjectionField
        {
            PropertyName = "Name"
        };

        var field2 = new ProjectionField
        {
            PropertyName = "Name"
        };

        // Act & Assert
        field1.ShouldBe(field2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void GetHashCodeIsConsistent()
    {
        // Arrange
        var field = new ProjectionField
        {
            PropertyName = "Name",
            Alias = "FullName"
        };

        // Act
        var hash1 = field.GetHashCode();
        var hash2 = field.GetHashCode();

        // Assert
        hash1.ShouldBe(hash2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ToStringReturnsValue()
    {
        // Arrange
        var field = new ProjectionField
        {
            PropertyName = "TestProperty",
            Alias = "TestAlias"
        };

        // Act
        var result = field.ToString();

        // Assert
        result.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void EmptyAliasIsNotSameAsNullAlias()
    {
        // Arrange
        var field1 = new ProjectionField
        {
            PropertyName = "Name",
            Alias = ""
        };

        var field2 = new ProjectionField
        {
            PropertyName = "Name",
            Alias = null
        };

        // Act & Assert
        field1.ShouldNotBe(field2);
    }
}
