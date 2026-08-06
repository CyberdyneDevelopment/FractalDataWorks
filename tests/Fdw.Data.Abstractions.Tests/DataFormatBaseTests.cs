using System.Diagnostics.CodeAnalysis;
using Fdw.Data.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.Abstractions.Tests;

public sealed class DataFormatBaseTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsId()
    {
        // Arrange & Act
        var format = new TestDataFormat(1, "TestFormat");

        // Assert
        format.Id.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsName()
    {
        // Arrange & Act
        var format = new TestDataFormat(1, "TestFormat");

        // Assert
        format.Name.ShouldBe("TestFormat");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsMimeType()
    {
        // Arrange & Act
        var format = new TestDataFormat(1, "TestFormat");

        // Assert
        format.MimeType.ShouldBe("application/test");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsFileExtensions()
    {
        // Arrange & Act
        var format = new TestDataFormat(1, "TestFormat");

        // Assert
        format.FileExtensions.ShouldNotBeNull();
        format.FileExtensions.Length.ShouldBe(2);
        format.FileExtensions.ShouldContain(".test");
        format.FileExtensions.ShouldContain(".tst");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsIsBinary()
    {
        // Arrange & Act
        var format = new TestDataFormat(1, "TestFormat", isBinary: true);

        // Assert
        format.IsBinary.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsSupportsSchemaDiscovery()
    {
        // Arrange & Act
        var format = new TestDataFormat(1, "TestFormat", supportsSchemaDiscovery: true);

        // Assert
        format.SupportsSchemaDiscovery.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsSupportsStreaming()
    {
        // Arrange & Act
        var format = new TestDataFormat(1, "TestFormat", supportsStreaming: true);

        // Assert
        format.SupportsStreaming.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsSupportsCompression()
    {
        // Arrange & Act
        var format = new TestDataFormat(1, "TestFormat", supportsCompression: true);

        // Assert
        format.SupportsCompression.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TextFormatExample()
    {
        // Arrange & Act
        var format = new TestDataFormat(
            1,
            "JSON",
            mimeType: "application/json",
            fileExtensions: [".json"],
            isBinary: false,
            supportsSchemaDiscovery: true,
            supportsStreaming: true,
            supportsCompression: true);

        // Assert
        format.Name.ShouldBe("JSON");
        format.MimeType.ShouldBe("application/json");
        format.FileExtensions.ShouldContain(".json");
        format.IsBinary.ShouldBeFalse();
        format.SupportsSchemaDiscovery.ShouldBeTrue();
        format.SupportsStreaming.ShouldBeTrue();
        format.SupportsCompression.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BinaryFormatExample()
    {
        // Arrange & Act
        var format = new TestDataFormat(
            2,
            "Parquet",
            mimeType: "application/parquet",
            fileExtensions: [".parquet"],
            isBinary: true,
            supportsSchemaDiscovery: true,
            supportsStreaming: false,
            supportsCompression: true);

        // Assert
        format.Name.ShouldBe("Parquet");
        format.IsBinary.ShouldBeTrue();
        format.SupportsStreaming.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void InheritsFromTypeOptionBase()
    {
        // Arrange
        var format = new TestDataFormat(1, "TestFormat");

        // Act & Assert
        format.ShouldBeAssignableTo<DataFormatBase>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ImplementsIDataFormat()
    {
        // Arrange
        var format = new TestDataFormat(1, "TestFormat");

        // Act & Assert
        format.ShouldBeAssignableTo<IDataFormat>();
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestDataFormat : DataFormatBase
    {
        public TestDataFormat(
            int id,
            string name,
            string mimeType = "application/test",
            string[]? fileExtensions = null,
            bool isBinary = false,
            bool supportsSchemaDiscovery = false,
            bool supportsStreaming = false,
            bool supportsCompression = false)
            : base(
                id,
                name,
                mimeType,
                fileExtensions ?? [".test", ".tst"],
                isBinary,
                supportsSchemaDiscovery,
                supportsStreaming,
                supportsCompression)
        {
        }
    }
}
