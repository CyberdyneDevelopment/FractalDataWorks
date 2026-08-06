using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;

namespace Fdw.Commands.Data.Tests;

/// <summary>
/// Comprehensive tests for the <see cref="UpdateCommand{T}"/> class.
/// Achieves 100% code path coverage for UpdateCommand.
/// </summary>
public sealed class UpdateCommandTests
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
        var testData = new TestEntity { Id = 1, Name = "Updated", IsActive = false };

        // Act
        var command = new UpdateCommand<TestEntity>(testData);

        // Assert
        command.Data.ShouldBe(testData);
        command.CommandType.ShouldBe("Update");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Constructor_InitializesFilterToNull()
    {
        // Arrange
        var testData = new TestEntity();

        // Act
        var command = new UpdateCommand<TestEntity>(testData);

        // Assert
        command.Filter.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Constructor_WithNullData_AcceptsNullValue()
    {
        // Act
        var command = new UpdateCommand<TestEntity?>(null);

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
        var testData = new TestEntity { Id = 42, Name = "Updated Entity", IsActive = true };

        // Act
        var command = new UpdateCommand<TestEntity>(testData);

        // Assert
        command.Data.ShouldBe(testData);
        command.Data.Id.ShouldBe(42);
        command.Data.Name.ShouldBe("Updated Entity");
        command.Data.IsActive.ShouldBeTrue();
    }

    #endregion

    #region Filter Property Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Filter_CanBeSetViaInitializer()
    {
        // Arrange
        var testData = new TestEntity();
        var filter = new Mock<IFilterExpression>().Object;

        // Act
        var command = new UpdateCommand<TestEntity>(testData)
        {
            Filter = filter
        };

        // Assert
        command.Filter.ShouldBe(filter);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Filter_CanBeNull()
    {
        // Arrange
        var testData = new TestEntity();

        // Act
        var command = new UpdateCommand<TestEntity>(testData)
        {
            Filter = null
        };

        // Assert
        command.Filter.ShouldBeNull();
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
        var command = new UpdateCommand<TestEntity>(testData);

        // Assert
        command.Metadata.ShouldNotBeNull();
        command.Metadata.Count.ShouldBe(0);
    }

    #endregion


    #region Type Safety Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void UpdateCommand_SupportsValueTypes()
    {
        // Arrange & Act
        var command = new UpdateCommand<int>(42);

        // Assert
        command.Data.ShouldBe(42);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void UpdateCommand_SupportsReferenceTypes()
    {
        // Arrange
        const string data = "Updated String";

        // Act
        var command = new UpdateCommand<string>(data);

        // Assert
        command.Data.ShouldBe(data);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void UpdateCommand_SupportsComplexTypes()
    {
        // Arrange
        var data = new TestEntity { Id = 1, Name = "Updated", IsActive = false };

        // Act
        var command = new UpdateCommand<TestEntity>(data);

        // Assert
        command.Data.ShouldBe(data);
    }

    #endregion

    #region Combined Properties Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void UpdateCommand_CanHaveDataAndFilter()
    {
        // Arrange
        var testData = new TestEntity { Id = 1, Name = "Updated" };
        var filter = new Mock<IFilterExpression>().Object;

        // Act
        var command = new UpdateCommand<TestEntity>(testData)
        {
            Filter = filter
        };

        // Assert
        command.Data.ShouldBe(testData);
        command.Filter.ShouldBe(filter);
    }

    #endregion
}
