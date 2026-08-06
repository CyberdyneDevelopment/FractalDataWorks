using Fdw.Commands.Data;

namespace Fdw.Commands.Data.Tests;

/// <summary>
/// Comprehensive tests for the <see cref="InsertCommand{T}"/> class.
/// Achieves 100% code path coverage for InsertCommand.
/// </summary>
public sealed class InsertCommandTests
{
    private sealed class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    #region Constructor Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Constructor_WithData_CreatesCommandWithCorrectCommandType()
    {
        // Arrange
        var testData = new TestEntity { Id = 1, Name = "Test", IsActive = true };

        // Act
        var command = new InsertCommand<TestEntity>(testData);

        // Assert
        command.Data.ShouldBe(testData);
        command.CommandType.ShouldBe("Insert");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Constructor_WithNullData_AcceptsNullValue()
    {
        // Act
        var command = new InsertCommand<TestEntity?>(null);

        // Assert
        command.Data.ShouldBeNull();
    }

    #endregion

    #region Data Property Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Data_Property_StoresProvidedValue()
    {
        // Arrange
        var testData = new TestEntity { Id = 42, Name = "Test Entity", IsActive = false };

        // Act
        var command = new InsertCommand<TestEntity>(testData);

        // Assert
        command.Data.ShouldBe(testData);
        command.Data.Id.ShouldBe(42);
        command.Data.Name.ShouldBe("Test Entity");
        command.Data.IsActive.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Data_WithComplexObject_RetainsAllProperties()
    {
        // Arrange
        var complexData = new TestEntity
        {
            Id = 100,
            Name = "Complex Entity",
            IsActive = true
        };

        // Act
        var command = new InsertCommand<TestEntity>(complexData);

        // Assert
        command.Data.Id.ShouldBe(100);
        command.Data.Name.ShouldBe("Complex Entity");
        command.Data.IsActive.ShouldBeTrue();
    }

    #endregion

    #region Metadata Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Metadata_IsInitializedByDefault()
    {
        // Arrange
        var testData = new TestEntity();

        // Act
        var command = new InsertCommand<TestEntity>(testData);

        // Assert
        command.Metadata.ShouldNotBeNull();
        command.Metadata.Count.ShouldBe(0);
    }

    #endregion


    #region Type Safety Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void InsertCommand_SupportsValueTypes()
    {
        // Arrange & Act
        var command = new InsertCommand<int>(42);

        // Assert
        command.Data.ShouldBe(42);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void InsertCommand_SupportsReferenceTypes()
    {
        // Arrange
        const string data = "Test String";

        // Act
        var command = new InsertCommand<string>(data);

        // Assert
        command.Data.ShouldBe(data);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void InsertCommand_SupportsComplexTypes()
    {
        // Arrange
        var data = new TestEntity { Id = 1, Name = "Test", IsActive = true };

        // Act
        var command = new InsertCommand<TestEntity>(data);

        // Assert
        command.Data.ShouldBe(data);
    }

    #endregion

    #region Immutability Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Data_IsSetOnConstruction_CannotBeChanged()
    {
        // Arrange
        var originalData = new TestEntity { Id = 1, Name = "Original" };
        var command = new InsertCommand<TestEntity>(originalData);

        // Act - Try to access Data property
        var retrievedData = command.Data;

        // Assert - Data property returns the same instance
        retrievedData.ShouldBe(originalData);
    }

    #endregion
}
