using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Commands.Data;

namespace Fdw.Commands.Data.Tests;

/// <summary>
/// Tests for the <see cref="InsertCommand{T}"/> class: one row or many, held as the rows the command inserts.
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
    public void Constructor_WithOneRow_CreatesCommandWithCorrectCommandType()
    {
        // Arrange
        var testData = new TestEntity { Id = 1, Name = "Test", IsActive = true };

        // Act
        var command = new InsertCommand<TestEntity>(testData);

        // Assert
        command.Data.ShouldBe([testData]);
        command.CommandType.ShouldBe("Insert");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Constructor_WithRows_HoldsTheRowsInOrder()
    {
        // Arrange
        var rows = new[] { new TestEntity { Id = 1 }, new TestEntity { Id = 2 }, new TestEntity { Id = 3 } };

        // Act
        var command = new InsertCommand<TestEntity>(rows);

        // Assert
        command.Data.Select(row => row.Id).ShouldBe([1, 2, 3]);
        command.CommandType.ShouldBe("Insert");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Constructor_WithNoRows_HoldsNoRows()
    {
        // Act
        var command = new InsertCommand<TestEntity>(System.Array.Empty<TestEntity>());

        // Assert
        command.Data.ShouldBeEmpty();
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
        var row = command.Data.Single();
        row.ShouldBe(testData);
        row.Id.ShouldBe(42);
        row.Name.ShouldBe("Test Entity");
        row.IsActive.ShouldBeFalse();
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
        var row = command.Data.Single();
        row.Id.ShouldBe(100);
        row.Name.ShouldBe("Complex Entity");
        row.IsActive.ShouldBeTrue();
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
        command.Data.ShouldBe([42]);
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
        command.Data.ShouldBe([data]);
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
        command.Data.Single().ShouldBe(data);
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
        var retrievedData = command.Data.Single();

        // Assert - Data property returns the same instance
        retrievedData.ShouldBe(originalData);
    }

    #endregion

    #region InBatchesOf Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void InBatchesOf_WithMoreRowsThanABatch_SplitsIntoInsertsOfThatManyRowsInOrder()
    {
        // Arrange
        var command = new InsertCommand<TestEntity>(Enumerable.Range(1, 5).Select(id => new TestEntity { Id = id }));

        // Act
        var batches = command.InBatchesOf(2);

        // Assert
        batches.Count.ShouldBe(3);
        batches.Select(batch => ((InsertCommand<TestEntity>)batch).Data.Select(row => row.Id).ToArray())
            .ToArray()
            .ShouldBe(new[] { new[] { 1, 2 }, new[] { 3, 4 }, new[] { 5 } });
        batches.ShouldAllBe(batch => batch.CommandType == "Insert");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void InBatchesOf_WithFewerRowsThanABatch_ReturnsOneInsertOfAllTheRows()
    {
        // Arrange
        var command = new InsertCommand<TestEntity>(Enumerable.Range(1, 3).Select(id => new TestEntity { Id = id }));

        // Act
        var batches = command.InBatchesOf(10);

        // Assert
        ((InsertCommand<TestEntity>)batches.ShouldHaveSingleItem()).Data.Count().ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void InBatchesOf_WithNoRows_ReturnsNoBatches()
    {
        // Arrange
        var command = new InsertCommand<TestEntity>(Array.Empty<TestEntity>());

        // Act
        var batches = command.InBatchesOf(2);

        // Assert
        batches.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void InBatchesOf_CarriesTheReturnColumnsAndMetadataIntoEveryBatch()
    {
        // Arrange
        var command = new InsertCommand<TestEntity>(Enumerable.Range(1, 3).Select(id => new TestEntity { Id = id })) { ReturnColumns = ["Id"], Metadata = new Dictionary<string, object> { ["source"] = "test" } };

        // Act
        var batches = command.InBatchesOf(2);

        // Assert
        batches.Count.ShouldBe(2);
        batches.ShouldAllBe(batch => ((InsertCommand<TestEntity>)batch).ReturnColumns!.Count == 1 && ((InsertCommand<TestEntity>)batch).Metadata["source"].Equals("test"));
    }

    #endregion
}
