using System.Linq;
using FluentValidation.TestHelper;
using Fdw.Data.DataSets.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.DataSets.Tests;

public class DataFieldConfigurationValidatorTests
{
    private readonly DataFieldConfigurationValidator _validator;

    public DataFieldConfigurationValidatorTests()
    {
        _validator = new DataFieldConfigurationValidator();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Validate_WithValidConfiguration_PassesValidation()
    {
        // Arrange
        var config = new DataFieldConfiguration
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
        var config = new DataFieldConfiguration
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
        var config = new DataFieldConfiguration
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
        var config = new DataFieldConfiguration
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
        var config = new DataFieldConfiguration
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
        var config = new DataFieldConfiguration
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
        var config = new DataFieldConfiguration
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
        var config = new DataFieldConfiguration
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
        var config = new DataFieldConfiguration
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
        var config = new DataFieldConfiguration
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

public class DataSetConfigurationValidatorTests
{
    private readonly DataSetConfigurationValidator _validator;

    public DataSetConfigurationValidatorTests()
    {
        _validator = new DataSetConfigurationValidator();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Validate_WithValidConfiguration_PassesValidation()
    {
        // Arrange
        var config = new DataSetConfiguration
        {
            Name = "TestDataSet",
            Description = "Test Description",
            RecordTypeName = "My.Type",
            Fields = { new DataFieldConfiguration { Name = "Id", TypeName = "System.Int32" } },
            KeyFields = { new DataSetKeyFieldConfiguration { KeyName = "Id", KeyType = "Surrogate", Ordinal = 0 } }
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
        var config = new DataSetConfiguration
        {
            Name = "",
            Description = "Test",
            RecordTypeName = "My.Type"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Dataset name is required");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Validate_WithNameTooLong_FailsValidation()
    {
        // Arrange
        var config = new DataSetConfiguration
        {
            Name = new string('a', 101),
            Description = "Test"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Dataset name must not exceed 100 characters");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Validate_WithEmptyDescription_FailsValidation()
    {
        // Arrange
        var config = new DataSetConfiguration
        {
            Name = "Test",
            Description = "",
            RecordTypeName = "My.Type"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description is required");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Validate_WithDescriptionTooLong_FailsValidation()
    {
        // Arrange
        var config = new DataSetConfiguration
        {
            Name = "Test",
            Description = new string('a', 501),
            RecordTypeName = "My.Type"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description must not exceed 500 characters");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Validate_WithEmptyRecordTypeName_FailsValidation()
    {
        // Arrange
        var config = new DataSetConfiguration
        {
            Name = "Test",
            Description = "Test",
            RecordTypeName = ""
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RecordTypeName)
            .WithErrorMessage("Record type name is required");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Validate_WithInvalidRecordTypeName_FailsValidation()
    {
        // Arrange
        var config = new DataSetConfiguration
        {
            Name = "Test",
            Description = "Test",
            RecordTypeName = "InvalidName"
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RecordTypeName)
            .WithErrorMessage("Record type name must be a valid .NET type name");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Validate_WithNoFields_FailsValidation()
    {
        // Arrange
        var config = new DataSetConfiguration
        {
            Name = "Test",
            Description = "Test",
            RecordTypeName = "My.Type",
            KeyFields = { new DataSetKeyFieldConfiguration { KeyName = "Id", KeyType = "Surrogate", Ordinal = 0 } }
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Fields)
            .WithErrorMessage("At least one field must be defined");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Validate_WithNoKeyFields_FailsValidation()
    {
        // Arrange
        var config = new DataSetConfiguration
        {
            Name = "Test",
            Description = "Test",
            RecordTypeName = "My.Type",
            Fields = { new DataFieldConfiguration { Name = "Id", TypeName = "System.Int32" } }
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.KeyFields)
            .WithErrorMessage("At least one key field must be specified");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Validate_WithKeyFieldNotInFields_PassesValidation()
    {
        // Why: Name-matches-field-list validation was removed in FDW-395 Phase 6.
        // Key field records are resolved by RowId via IDataNode at load time, not by matching
        // KeyName against the Fields collection. A key field referencing a non-existent field
        // name is now valid at the configuration layer.
        var config = new DataSetConfiguration
        {
            Name = "Test",
            Description = "Test",
            RecordTypeName = "My.Type",
            Fields = { new DataFieldConfiguration { Name = "Name", TypeName = "System.String" } },
            KeyFields = { new DataSetKeyFieldConfiguration { KeyName = "Id", KeyType = "Surrogate", Ordinal = 0 } }
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Validate_WithInvalidFieldConfiguration_FailsValidation()
    {
        // Arrange
        var config = new DataSetConfiguration
        {
            Name = "Test",
            Description = "Test",
            RecordTypeName = "My.Type",
            Fields = { new DataFieldConfiguration { Name = "", TypeName = "System.String" } },
            KeyFields = { new DataSetKeyFieldConfiguration { KeyName = "Id", KeyType = "Surrogate", Ordinal = 0 } }
        };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor("Fields[0].Name");
    }
}
