using System.Linq;
using FluentValidation.TestHelper;
using Fdw.Data.DataSets.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.DataSets.Tests;

public class DataSetFieldConfigurationValidatorTests
{
    private readonly DataSetFieldConfigurationValidator _validator;

    public DataSetFieldConfigurationValidatorTests()
    {
        _validator = new DataSetFieldConfigurationValidator();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Validate_WithValidConfiguration_PassesValidation()
    {
        // Arrange
        var config = new DataSetFieldConfiguration
        {
            Name = "TestField",
            TypeName = "System.String"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Validate_WithEmptyName_FailsValidation()
    {
        // Arrange
        var config = new DataSetFieldConfiguration
        {
            Name = "",
            TypeName = "System.String"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Field name is required");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Validate_WithNameTooLong_FailsValidation()
    {
        // Arrange
        var config = new DataSetFieldConfiguration
        {
            Name = new string('a', 51),
            TypeName = "System.String"
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
    public void Validate_WithEmptyTypeName_FailsValidation()
    {
        // Arrange
        var config = new DataSetFieldConfiguration
        {
            Name = "TestField",
            TypeName = ""
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TypeName)
            .WithErrorMessage("Field type name is required");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Validate_WithInvalidTypeName_FailsValidation()
    {
        // Arrange
        var config = new DataSetFieldConfiguration
        {
            Name = "TestField",
            TypeName = "Invalid Type Name"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TypeName)
            .WithErrorMessage("Field type name must be a valid .NET type name");
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("System.String")]
    [InlineData("System.Int32")]
    [InlineData("System.DateTime")]
    [InlineData("string")]
    [InlineData("int")]
    [InlineData("DateTime")]
    [InlineData("My.Custom.Type")]
    public void Validate_WithValidTypeName_PassesValidation(string typeName)
    {
        // Arrange
        var config = new DataSetFieldConfiguration
        {
            Name = "TestField",
            TypeName = typeName
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TypeName);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Validate_WithNegativeMaxLength_FailsValidation()
    {
        // Arrange
        var config = new DataSetFieldConfiguration
        {
            Name = "TestField",
            TypeName = "System.String",
            MaxLength = -1
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
    public void Validate_WithZeroMaxLength_FailsValidation()
    {
        // Arrange
        var config = new DataSetFieldConfiguration
        {
            Name = "TestField",
            TypeName = "System.String",
            MaxLength = 0
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MaxLength);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Validate_WithValidMaxLength_PassesValidation()
    {
        // Arrange
        var config = new DataSetFieldConfiguration
        {
            Name = "TestField",
            TypeName = "System.String",
            MaxLength = 100
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.MaxLength);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Validate_WithNullMaxLength_PassesValidation()
    {
        // Arrange
        var config = new DataSetFieldConfiguration
        {
            Name = "TestField",
            TypeName = "System.String",
            MaxLength = null
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.MaxLength);
    }
}

