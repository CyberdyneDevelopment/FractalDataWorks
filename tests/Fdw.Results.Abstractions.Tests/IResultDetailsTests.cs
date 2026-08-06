using Fdw.Results.Abstractions;

namespace Fdw.Results.Abstractions.Tests;

/// <summary>
/// Tests for IResultDetails interface contract.
/// </summary>
public sealed class IResultDetailsTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void MockedIResultDetailsCanBeCreated()
    {
        // Arrange
        var mock = new Mock<IResultDetails>();
        var data = new Dictionary<string, object?>
        {
            { "key1", "value1" },
            { "key2", 42 }
        };
        mock.Setup(d => d.Data).Returns(data);
        mock.Setup(d => d.GetValue<string>("key1")).Returns("value1");
        mock.Setup(d => d.GetValue<int>("key2")).Returns(42);
        mock.Setup(d => d.IsPooled).Returns(false);

        // Act
        var resultDetails = mock.Object;

        // Assert
        resultDetails.ShouldNotBeNull();
        resultDetails.Data.ShouldNotBeNull();
        resultDetails.Data.Count.ShouldBe(2);
        resultDetails.GetValue<string>("key1").ShouldBe("value1");
        resultDetails.GetValue<int>("key2").ShouldBe(42);
        resultDetails.IsPooled.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IResultDetailsHasDataProperty()
    {
        // Assert
        typeof(IResultDetails).GetProperty(nameof(IResultDetails.Data)).ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IResultDetailsDataPropertyReturnsReadOnlyDictionary()
    {
        // Assert
        var property = typeof(IResultDetails).GetProperty(nameof(IResultDetails.Data));
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(IReadOnlyDictionary<string, object?>));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IResultDetailsHasGetValueMethod()
    {
        // Assert
        var method = typeof(IResultDetails).GetMethod(nameof(IResultDetails.GetValue));
        method.ShouldNotBeNull();
        method!.IsGenericMethod.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IResultDetailsHasIsPooledProperty()
    {
        // Assert
        typeof(IResultDetails).GetProperty(nameof(IResultDetails.IsPooled)).ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IResultDetailsImplementsIDisposable()
    {
        // Assert
        typeof(IResultDetails).GetInterfaces().ShouldContain(typeof(IDisposable));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void GetValueReturnsCorrectTypeForStringKey()
    {
        // Arrange
        var mock = new Mock<IResultDetails>();
        mock.Setup(d => d.GetValue<string>("name")).Returns("TestValue");

        // Act
        var result = mock.Object.GetValue<string>("name");

        // Assert
        result.ShouldBe("TestValue");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void GetValueReturnsDefaultWhenKeyNotFound()
    {
        // Arrange
        var mock = new Mock<IResultDetails>();
        mock.Setup(d => d.GetValue<string>("missing")).Returns((string?)null);

        // Act
        var result = mock.Object.GetValue<string>("missing");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void GetValueHandlesDifferentTypes()
    {
        // Arrange
        var mock = new Mock<IResultDetails>();
        mock.Setup(d => d.GetValue<int>("count")).Returns(42);
        mock.Setup(d => d.GetValue<bool>("isValid")).Returns(true);
        mock.Setup(d => d.GetValue<DateTime>("timestamp")).Returns(new DateTime(2026, 2, 4));

        // Act & Assert
        mock.Object.GetValue<int>("count").ShouldBe(42);
        mock.Object.GetValue<bool>("isValid").ShouldBeTrue();
        mock.Object.GetValue<DateTime>("timestamp").ShouldBe(new DateTime(2026, 2, 4));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void DataDictionaryCanContainNullValues()
    {
        // Arrange
        var mock = new Mock<IResultDetails>();
        var data = new Dictionary<string, object?>
        {
            { "nullKey", null },
            { "valueKey", "value" }
        };
        mock.Setup(d => d.Data).Returns(data);

        // Act
        var resultData = mock.Object.Data;

        // Assert
        resultData["nullKey"].ShouldBeNull();
        resultData["valueKey"].ShouldBe("value");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IsPooledReturnsTrueWhenDetailsArePooled()
    {
        // Arrange
        var mock = new Mock<IResultDetails>();
        mock.Setup(d => d.IsPooled).Returns(true);

        // Act
        var isPooled = mock.Object.IsPooled;

        // Assert
        isPooled.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IsPooledReturnsFalseWhenDetailsAreNotPooled()
    {
        // Arrange
        var mock = new Mock<IResultDetails>();
        mock.Setup(d => d.IsPooled).Returns(false);

        // Act
        var isPooled = mock.Object.IsPooled;

        // Assert
        isPooled.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void DisposeCanBeCalled()
    {
        // Arrange
        var mock = new Mock<IResultDetails>();

        // Act & Assert
        Should.NotThrow(() => mock.Object.Dispose());
        mock.Verify(d => d.Dispose(), Times.Once);
    }
}
