using Shouldly;
using Xunit;
using Fdw.Data.DataSets.Abstractions;
using FluentValidation.TestHelper;

namespace Fdw.Data.DataSets.Abstractions.Tests;

public sealed class DataSetFieldConfigurationValidatorTests
{
    private readonly DataSetFieldConfigurationValidator _validator = new();

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidateFailsWhenNameIsEmpty()
    {
        // Arrange
        var config = new DataSetFieldConfiguration { Name = string.Empty, TypeName = "String" };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidateFailsWhenNameExceeds50Characters()
    {
        // Arrange
        var config = new DataSetFieldConfiguration
        {
            Name = new string('A', 51),
            TypeName = "String"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Field name must not exceed 50 characters");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidatePassesWithValidName()
    {
        // Arrange
        var config = new DataSetFieldConfiguration
        {
            Name = "ValidFieldName",
            TypeName = "String"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidateFailsWhenTypeNameIsEmpty()
    {
        // Arrange
        var config = new DataSetFieldConfiguration { Name = "Field1", TypeName = string.Empty };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TypeName);
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("String")]
    [InlineData("Int16")]
    [InlineData("Int32")]
    [InlineData("Int64")]
    [InlineData("Date")]
    [InlineData("DateTime")]
    [InlineData("DateTimeOffset")]
    [InlineData("Time")]
    [InlineData("Decimal")]
    [InlineData("Double")]
    [InlineData("Float")]
    [InlineData("Bool")]
    [InlineData("Byte")]
    [InlineData("Guid")]
    [InlineData("Binary")]
    public void ValidatePassesWithADataTypeName(string typeName)
    {
        // Arrange
        var config = new DataSetFieldConfiguration { Name = "Field1", TypeName = typeName };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TypeName);
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("string")]
    [InlineData("int")]
    [InlineData("long")]
    [InlineData("decimal")]
    [InlineData("double")]
    [InlineData("bool")]
    [InlineData("byte[]")]
    [InlineData("INT32")]
    [InlineData("guid")]
    public void ValidateFailsWithAKeywordOrAMiscasedName(string typeName)
    {
        // Arrange
        var config = new DataSetFieldConfiguration { Name = "Field1", TypeName = typeName };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TypeName)
            .WithErrorMessage("Field data type must be the name of a DataTypes option");
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("System.String")]
    [InlineData("System.Int32")]
    [InlineData("System.Byte[]")]
    [InlineData("My.Custom.Type")]
    [InlineData("Company.Product.Domain.MyEntity")]
    public void ValidateFailsWithAQualifiedTypeName(string typeName)
    {
        // Arrange
        var config = new DataSetFieldConfiguration { Name = "Field1", TypeName = typeName };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TypeName)
            .WithErrorMessage("Field data type must be the name of a DataTypes option");
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("invalid type")]  // contains space
    [InlineData("NoNamespace")]   // no dot, not common type
    [InlineData("Has Space.Type")]
    public void ValidateFailsWithInvalidTypeName(string typeName)
    {
        // Arrange
        var config = new DataSetFieldConfiguration { Name = "Field1", TypeName = typeName };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TypeName)
            .WithErrorMessage("Field data type must be the name of a DataTypes option");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidatePassesWhenMaxLengthIsNull()
    {
        // Arrange
        var config = new DataSetFieldConfiguration
        {
            Name = "Field1",
            TypeName = "String",
            MaxLength = null
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.MaxLength);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidateFailsWhenMaxLengthIsZero()
    {
        // Arrange
        var config = new DataSetFieldConfiguration
        {
            Name = "Field1",
            TypeName = "String",
            MaxLength = 0
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MaxLength)
            .WithErrorMessage("Max length must be greater than zero when specified");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidateFailsWhenMaxLengthIsNegative()
    {
        // Arrange
        var config = new DataSetFieldConfiguration
        {
            Name = "Field1",
            TypeName = "String",
            MaxLength = -1
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MaxLength);
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(255)]
    [InlineData(int.MaxValue)]
    public void ValidatePassesWhenMaxLengthIsPositive(int maxLength)
    {
        // Arrange
        var config = new DataSetFieldConfiguration
        {
            Name = "Field1",
            TypeName = "String",
            MaxLength = maxLength
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.MaxLength);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidatePassesWithAllValidProperties()
    {
        // Arrange
        var config = new DataSetFieldConfiguration
        {
            Name = "CustomerId",
            Description = "Customer identifier",
            TypeName = "Int32",
            Role = "Surrogate",
            IsKey = true,
            IsRequired = true,
            IsIndexed = true,
            MaxLength = null
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidateReturnsMultipleErrorsForMultipleInvalidProperties()
    {
        // Arrange
        var config = new DataSetFieldConfiguration
        {
            Name = string.Empty,
            TypeName = "invalid type",
            MaxLength = -5
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBeGreaterThanOrEqualTo(3);
    }
}
