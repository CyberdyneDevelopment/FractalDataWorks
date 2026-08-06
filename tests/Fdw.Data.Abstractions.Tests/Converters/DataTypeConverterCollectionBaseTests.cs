using System.Diagnostics.CodeAnalysis;
using Fdw.Data.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.Abstractions.Tests.Converters;

public sealed class DataTypeConverterCollectionBaseTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsId()
    {
        // Arrange & Act
        var collection = new TestConverterCollection();

        // Assert
        collection.Id.ShouldBe("TestConverters");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsName()
    {
        // Arrange & Act
        var collection = new TestConverterCollection();

        // Assert
        collection.Name.ShouldBe("Test Converters");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AllThrowsNotSupportedException()
    {
        // Arrange
        var collection = new TestConverterCollection();
        IDataTypeConverters converters = collection;

        // Act & Assert
        Should.Throw<NotSupportedException>(() => converters.All());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ByIdThrowsNotSupportedException()
    {
        // Arrange
        var collection = new TestConverterCollection();
        IDataTypeConverters converters = collection;

        // Act & Assert
        Should.Throw<NotSupportedException>(() => converters.ById(1));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ByNameThrowsNotSupportedException()
    {
        // Arrange
        var collection = new TestConverterCollection();
        IDataTypeConverters converters = collection;

        // Act & Assert
        Should.Throw<NotSupportedException>(() => converters.ByName("test"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BySourceTypeThrowsNotSupportedException()
    {
        // Arrange
        var collection = new TestConverterCollection();
        IDataTypeConverters converters = collection;

        // Act & Assert
        Should.Throw<NotSupportedException>(() => converters.BySourceType("int"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void NotFoundThrowsNotSupportedException()
    {
        // Arrange
        var collection = new TestConverterCollection();
        IDataTypeConverters converters = collection;

        // Act & Assert
        Should.Throw<NotSupportedException>(() => _ = converters.NotFound);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ImplementsIDataTypeConverters()
    {
        // Arrange
        var collection = new TestConverterCollection();

        // Act & Assert
        collection.ShouldBeAssignableTo<IDataTypeConverters>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void InheritsFromTypeOptionBase()
    {
        // Arrange
        var collection = new TestConverterCollection();

        // Act & Assert
        collection.ShouldBeAssignableTo<DataTypeConverterCollectionBase>();
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestConverterCollection : DataTypeConverterCollectionBase
    {
        public TestConverterCollection()
            : base("TestConverters", "Test Converters")
        {
        }
    }
}
