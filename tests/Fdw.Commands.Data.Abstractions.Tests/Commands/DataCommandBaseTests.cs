using System;
using System.Collections.Generic;
using Fdw.Commands.Data.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Commands.Data.Abstractions.Tests.Commands;

/// <summary>
/// Tests for DataCommandBase and its generic variants.
/// Addressing (ContainerName, ConnectionName, DataStoreName, PathName) was stripped from
/// IDataCommand in the target-typed-gateway refactor; it now lives in DataStoreTarget only.
/// </summary>
public sealed class DataCommandBaseTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsCommandTypeCorrectly()
    {
        // Arrange & Act
        var command = new TestDataCommand("TestCommand");

        // Assert
        command.CommandType.ShouldBe("TestCommand");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DefaultCategoryIsData()
    {
        // Arrange & Act
        var command = new TestDataCommand("TestCommand");

        // Assert
        command.Category.ShouldBe("Data");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorWithCustomCategorySetsCategory()
    {
        // Arrange & Act
        var command = new TestDataCommand("TestCommand", "CustomCategory");

        // Assert
        command.Category.ShouldBe("CustomCategory");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorThrowsWhenCommandTypeIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new TestDataCommand(null!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void NullCategoryFallsBackToData()
    {
        // Arrange & Act
        var command = new TestDataCommand("TestCommand", null!);

        // Assert
        command.Category.ShouldBe("Data");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CommandIdIsNotEmpty()
    {
        var command = new TestDataCommand("TestCommand");

        command.CommandId.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CreatedAtIsNearNow()
    {
        var command = new TestDataCommand("TestCommand");

        command.CreatedAt.ShouldBeInRange(DateTime.UtcNow.AddSeconds(-5), DateTime.UtcNow.AddSeconds(5));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void MetadataIsNotNullAndIsEmptyByDefault()
    {
        var command = new TestDataCommand("TestCommand");

        command.Metadata.ShouldNotBeNull();
        command.Metadata.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void MetadataCanBeOverriddenViaInit()
    {
        // Arrange
        var metadata = new Dictionary<string, object>
        {
            ["Key1"] = "Value1",
            ["Key2"] = 42
        };

        // Act
        var command = new TestDataCommand("TestCommand") { Metadata = metadata };

        // Assert
        command.Metadata.ShouldBe(metadata);
        command.Metadata.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenericDataCommandBaseWithResultInitializesCorrectly()
    {
        // Arrange & Act
        var command = new TestDataCommandWithResult("TestCommand");

        // Assert
        command.CommandType.ShouldBe("TestCommand");
        command.Category.ShouldBe("Data");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GenericDataCommandBaseWithInputAndResultInitializesCorrectly()
    {
        // Arrange
        var testData = new TestInputData { Value = "TestValue" };

        // Act
        var command = new TestDataCommandWithInputAndResult("TestCommand", testData);

        // Assert
        command.CommandType.ShouldBe("TestCommand");
        command.Data.ShouldBe(testData);
        command.Data.Value.ShouldBe("TestValue");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void InputDataReturnsDataAsObject()
    {
        // Arrange
        var testData = new TestInputData { Value = "TestValue" };
        var command = new TestDataCommandWithInputAndResult("TestCommand", testData);

        // Act
        var inputData = ((IDataCommandWithInput)command).InputData;

        // Assert
        inputData.ShouldBe(testData);
    }

    // Test doubles
    private sealed class TestDataCommand : DataCommandBase
    {
        public TestDataCommand(string commandType, string? category = null)
            : base(commandType, category ?? "Data")
        {
        }
    }

    private sealed class TestDataCommandWithResult : DataCommandBase<string>
    {
        public TestDataCommandWithResult(string commandType)
            : base(commandType)
        {
        }
    }

    private sealed class TestDataCommandWithInputAndResult : DataCommandBase<string, TestInputData>
    {
        public TestDataCommandWithInputAndResult(string commandType, TestInputData data)
            : base(commandType, data)
        {
        }
    }

    private sealed class TestInputData
    {
        public string Value { get; set; } = string.Empty;
    }
}
