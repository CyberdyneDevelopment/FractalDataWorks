using Shouldly;
using Xunit;
using Fdw.Data.DataSets.Abstractions;
using FluentValidation.TestHelper;

namespace Fdw.Data.DataSets.Abstractions.Tests;

public sealed class DataFieldConfigurationValidatorTests
{
    private readonly DataFieldConfigurationValidator _validator = new();

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidateFailsWhenNameIsEmpty()
    {
        // Arrange
        var config = new DataFieldConfiguration { Name = string.Empty, TypeName = "System.String" };

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
        var config = new DataFieldConfiguration
        {
            Name = new string('A', 51),
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
    public void ValidatePassesWithValidName()
    {
        // Arrange
        var config = new DataFieldConfiguration
        {
            Name = "ValidFieldName",
            TypeName = "System.String"
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
        var config = new DataFieldConfiguration { Name = "Field1", TypeName = string.Empty };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TypeName);
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("System.String")]
    [InlineData("System.Int32")]
    [InlineData("System.Int64")]
    [InlineData("System.DateTime")]
    [InlineData("System.Decimal")]
    [InlineData("System.Double")]
    [InlineData("System.Boolean")]
    [InlineData("System.Guid")]
    [InlineData("System.Byte[]")]
    public void ValidatePassesWithFullyQualifiedCommonTypeName(string typeName)
    {
        // Arrange
        var config = new DataFieldConfiguration { Name = "Field1", TypeName = typeName };

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
    [InlineData("DateTime")]
    [InlineData("decimal")]
    [InlineData("double")]
    [InlineData("bool")]
    [InlineData("Guid")]
    [InlineData("byte[]")]
    public void ValidatePassesWithShortCommonTypeName(string typeName)
    {
        // Arrange
        var config = new DataFieldConfiguration { Name = "Field1", TypeName = typeName };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TypeName);
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("My.Custom.Type")]
    [InlineData("Namespace.ClassName")]
    [InlineData("Company.Product.Domain.MyEntity")]
    public void ValidatePassesWithQualifiedTypeName(string typeName)
    {
        // Arrange
        var config = new DataFieldConfiguration { Name = "Field1", TypeName = typeName };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TypeName);
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
        var config = new DataFieldConfiguration { Name = "Field1", TypeName = typeName };

        // Act
        var result = _validator.TestValidate(config);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TypeName)
            .WithErrorMessage("Field type name must be a valid .NET type name");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidatePassesWhenMaxLengthIsNull()
    {
        // Arrange
        var config = new DataFieldConfiguration
        {
            Name = "Field1",
            TypeName = "System.String",
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
        var config = new DataFieldConfiguration
        {
            Name = "Field1",
            TypeName = "System.String",
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
        var config = new DataFieldConfiguration
        {
            Name = "Field1",
            TypeName = "System.String",
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
        var config = new DataFieldConfiguration
        {
            Name = "Field1",
            TypeName = "System.String",
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
        var config = new DataFieldConfiguration
        {
            Name = "CustomerId",
            Description = "Customer identifier",
            TypeName = "System.Int32",
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
        var config = new DataFieldConfiguration
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
