using System;
using Fdw.Data.Importers.Abstractions;
using Fdw.Data.SchemaImporters.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.Importers.Abstractions.Tests;

/// <summary>
/// Tests for <see cref="SchemaImportSyncResult"/> - covers all properties and TotalChanges computation.
/// </summary>
public sealed class SchemaImportSyncResultTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void TotalChangesReturnsSumOfAllChanges()
    {
        // Arrange
        var result = new SchemaImportSyncResult
        {
            DataStoreId = Guid.NewGuid(),
            PathsAdded = 1,
            PathsModified = 2,
            PathsRemoved = 3,
            ContainersAdded = 4,
            ContainersModified = 5,
            ContainersRemoved = 6,
            FieldsAdded = 7,
            FieldsModified = 8,
            FieldsRemoved = 9
        };

        // Act & Assert
        result.TotalChanges.ShouldBe(45); // 1+2+3+4+5+6+7+8+9
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void TotalChangesReturnsZeroWhenAllCountsAreZero()
    {
        // Arrange
        var result = new SchemaImportSyncResult();

        // Act & Assert
        result.TotalChanges.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void DataStoreIdCanBeSetAndRetrieved()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var result = new SchemaImportSyncResult { DataStoreId = id };

        // Assert
        result.DataStoreId.ShouldBe(id);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void NewSchemaHashCanBeSetAndRetrieved()
    {
        // Arrange & Act
        var result = new SchemaImportSyncResult { NewSchemaHash = "abc123" };

        // Assert
        result.NewSchemaHash.ShouldBe("abc123");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void NewSchemaHashDefaultsToNull()
    {
        // Arrange & Act
        var result = new SchemaImportSyncResult();

        // Assert
        result.NewSchemaHash.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void TotalChangesOnlyCountsPathChanges()
    {
        // Arrange
        var result = new SchemaImportSyncResult
        {
            PathsAdded = 3,
            PathsModified = 2,
            PathsRemoved = 1
        };

        // Act & Assert
        result.TotalChanges.ShouldBe(6);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void TotalChangesOnlyCountsContainerChanges()
    {
        // Arrange
        var result = new SchemaImportSyncResult
        {
            ContainersAdded = 10,
            ContainersModified = 5,
            ContainersRemoved = 2
        };

        // Act & Assert
        result.TotalChanges.ShouldBe(17);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void TotalChangesOnlyCountsFieldChanges()
    {
        // Arrange
        var result = new SchemaImportSyncResult
        {
            FieldsAdded = 20,
            FieldsModified = 10,
            FieldsRemoved = 5
        };

        // Act & Assert
        result.TotalChanges.ShouldBe(35);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void DefaultValuesAreZero()
    {
        // Arrange & Act
        var result = new SchemaImportSyncResult();

        // Assert
        result.DataStoreId.ShouldBe(Guid.Empty);
        result.PathsAdded.ShouldBe(0);
        result.PathsModified.ShouldBe(0);
        result.PathsRemoved.ShouldBe(0);
        result.ContainersAdded.ShouldBe(0);
        result.ContainersModified.ShouldBe(0);
        result.ContainersRemoved.ShouldBe(0);
        result.FieldsAdded.ShouldBe(0);
        result.FieldsModified.ShouldBe(0);
        result.FieldsRemoved.ShouldBe(0);
    }
}
