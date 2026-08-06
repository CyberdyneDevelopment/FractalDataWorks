using Fdw.Data.DataSets.Abstractions;

namespace Fdw.Data.Execution.Tests;

/// <summary>
/// Tests for <see cref="DataSetConfigurationValidator"/>.
/// </summary>
public sealed class DataSetConfigurationValidatorTests
{
    private readonly DataSetConfigurationValidator _sut = new();

    private static DataSetConfiguration CreateValidConfiguration() =>
        new DataSetConfiguration
        {
            Name = "TestDataSet",
            Description = "A test dataset description",
            RecordTypeName = "TestNamespace.TestRecord",
            Fields =
            [
                new DataFieldConfiguration { Name = "Id", TypeName = "int" },
                new DataFieldConfiguration { Name = "Name", TypeName = "string" }
            ],
            KeyFields = [new DataSetKeyFieldConfiguration { KeyName = "Id", KeyType = "Surrogate", Ordinal = 0 }]
        };

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidateReturnsValidForCompleteConfiguration()
    {
        // Arrange
        var config = CreateValidConfiguration();

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidateFailsWhenNameIsEmpty()
    {
        // Arrange
        var config = CreateValidConfiguration();
        config.Name = string.Empty;

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(DataSetConfiguration.Name));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidateFailsWhenNameExceedsMaxLength()
    {
        // Arrange
        var config = CreateValidConfiguration();
        config.Name = new string('A', 101);

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(DataSetConfiguration.Name));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidatePassesWhenNameIsExactlyMaxLength()
    {
        // Arrange
        var config = CreateValidConfiguration();
        config.Name = new string('A', 100);

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidateFailsWhenDescriptionIsEmpty()
    {
        // Arrange
        var config = CreateValidConfiguration();
        config.Description = string.Empty;

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(DataSetConfiguration.Description));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidateFailsWhenDescriptionExceedsMaxLength()
    {
        // Arrange
        var config = CreateValidConfiguration();
        config.Description = new string('D', 501);

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(DataSetConfiguration.Description));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidatePassesWhenDescriptionIsExactlyMaxLength()
    {
        // Arrange
        var config = CreateValidConfiguration();
        config.Description = new string('D', 500);

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidateFailsWhenRecordTypeNameIsEmpty()
    {
        // Arrange
        var config = CreateValidConfiguration();
        config.RecordTypeName = string.Empty;

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(DataSetConfiguration.RecordTypeName));
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("NoNamespace")]
    [InlineData("Has Space.Record")]
    [InlineData("   ")]
    public void ValidateFailsWhenRecordTypeNameIsInvalid(string invalidTypeName)
    {
        // Arrange
        var config = CreateValidConfiguration();
        config.RecordTypeName = invalidTypeName;

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(DataSetConfiguration.RecordTypeName));
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("MyNamespace.MyRecord")]
    [InlineData("My.Deep.Namespace.Record")]
    [InlineData("A.B")]
    public void ValidatePassesWhenRecordTypeNameIsValid(string validTypeName)
    {
        // Arrange
        var config = CreateValidConfiguration();
        config.RecordTypeName = validTypeName;

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidateFailsWhenFieldsIsEmpty()
    {
        // Arrange
        var config = CreateValidConfiguration();
        config.Fields = [];

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(DataSetConfiguration.Fields));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidateFailsWhenKeyFieldsIsEmpty()
    {
        // Arrange
        var config = CreateValidConfiguration();
        config.KeyFields = [];

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(DataSetConfiguration.KeyFields));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidatePassesWhenKeyFieldRecordsPresent()
    {
        // Arrange
        var config = CreateValidConfiguration();
        config.KeyFields = [new DataSetKeyFieldConfiguration { KeyName = "NonExistentField", KeyType = "Surrogate", Ordinal = 0 }];

        // Act
        var result = _sut.Validate(config);

        // Why: Name-matches-field-list validation was removed; records are resolved by RowId at load time.
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidatePassesWhenKeyFieldExistsInFieldsCaseInsensitively()
    {
        // Arrange
        var config = CreateValidConfiguration();
        config.Fields = [new DataFieldConfiguration { Name = "Id", TypeName = "int" }];
        config.KeyFields = [new DataSetKeyFieldConfiguration { KeyName = "id", KeyType = "Surrogate", Ordinal = 0 }];

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidateFailsWithMultipleErrorsWhenMultipleFieldsInvalid()
    {
        // Arrange
        var config = new DataSetConfiguration
        {
            Name = string.Empty,
            Description = string.Empty,
            RecordTypeName = string.Empty,
            Fields = [],
            KeyFields = []
        };

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBeGreaterThan(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidatePassesWithMultipleKeyFields()
    {
        // Arrange
        var config = CreateValidConfiguration();
        config.Fields =
        [
            new DataFieldConfiguration { Name = "TenantId", TypeName = "int" },
            new DataFieldConfiguration { Name = "RecordId", TypeName = "int" },
            new DataFieldConfiguration { Name = "Name", TypeName = "string" }
        ];
        config.KeyFields =
        [
            new DataSetKeyFieldConfiguration { KeyName = "TenantId", KeyType = "Surrogate", Ordinal = 0 },
            new DataSetKeyFieldConfiguration { KeyName = "RecordId", KeyType = "Surrogate", Ordinal = 1 }
        ];

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ValidatePassesWithMixedKeyFieldRecords()
    {
        // Arrange
        var config = CreateValidConfiguration();
        config.Fields = [new DataFieldConfiguration { Name = "Id", TypeName = "int" }];
        config.KeyFields =
        [
            new DataSetKeyFieldConfiguration { KeyName = "Id", KeyType = "Surrogate", Ordinal = 0 },
            new DataSetKeyFieldConfiguration { KeyName = "NonExistent", KeyType = "Surrogate", Ordinal = 1 }
        ];

        // Act
        var result = _sut.Validate(config);

        // Why: Name-matches-field-list validation was removed; records resolved by RowId at load time.
        result.IsValid.ShouldBeTrue();
    }
}
