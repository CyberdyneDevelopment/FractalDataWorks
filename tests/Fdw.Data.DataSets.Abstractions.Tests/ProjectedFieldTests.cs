using System;
using Shouldly;
using Xunit;

namespace Fdw.Data.DataSets.Abstractions.Tests;

public class ProjectedFieldTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorInitializesWithDefaults()
    {
        // Arrange & Act
        var sut = new ProjectedField();

        // Assert
        sut.SourceField.ShouldBe(string.Empty);
        sut.Alias.ShouldBe(string.Empty);
        sut.FieldType.ShouldBe(typeof(object));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CanSetSourceField()
    {
        // Arrange & Act
        var sut = new ProjectedField { SourceField = "CustomerName" };

        // Assert
        sut.SourceField.ShouldBe("CustomerName");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CanSetAlias()
    {
        // Arrange & Act
        var sut = new ProjectedField { Alias = "Name" };

        // Assert
        sut.Alias.ShouldBe("Name");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CanSetFieldType()
    {
        // Arrange & Act
        var sut = new ProjectedField { FieldType = typeof(int) };

        // Assert
        sut.FieldType.ShouldBe(typeof(int));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CanInitializeAllPropertiesTogether()
    {
        // Arrange & Act
        var sut = new ProjectedField
        {
            SourceField = "Id",
            Alias = "CustomerId",
            FieldType = typeof(Guid)
        };

        // Assert
        sut.SourceField.ShouldBe("Id");
        sut.Alias.ShouldBe("CustomerId");
        sut.FieldType.ShouldBe(typeof(Guid));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SupportsValueTypes()
    {
        // Arrange & Act
        var sut = new ProjectedField { FieldType = typeof(decimal) };

        // Assert
        sut.FieldType.ShouldBe(typeof(decimal));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SupportsReferenceTypes()
    {
        // Arrange & Act
        var sut = new ProjectedField { FieldType = typeof(string) };

        // Assert
        sut.FieldType.ShouldBe(typeof(string));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SupportsNullableTypes()
    {
        // Arrange & Act
        var sut = new ProjectedField { FieldType = typeof(int?) };

        // Assert
        sut.FieldType.ShouldBe(typeof(int?));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CanHaveSameSourceFieldAndAlias()
    {
        // Arrange & Act
        var sut = new ProjectedField
        {
            SourceField = "Name",
            Alias = "Name",
            FieldType = typeof(string)
        };

        // Assert
        sut.SourceField.ShouldBe("Name");
        sut.Alias.ShouldBe("Name");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CanHaveDifferentSourceFieldAndAlias()
    {
        // Arrange & Act
        var sut = new ProjectedField
        {
            SourceField = "customer_name",
            Alias = "CustomerName",
            FieldType = typeof(string)
        };

        // Assert
        sut.SourceField.ShouldBe("customer_name");
        sut.Alias.ShouldBe("CustomerName");
    }
}
