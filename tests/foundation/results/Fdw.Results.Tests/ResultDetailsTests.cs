using Fdw.Results;

namespace Fdw.Results.Tests;

/// <summary>
/// Tests for the ResultDetails class - pooled implementation of IResultDetails.
/// </summary>
public class ResultDetailsTests
{
    #region Create Tests

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void CreateWithNoArgs_ReturnsEmptyInstance()
    {
        // Act
        var details = ResultDetails.Create();

        // Assert
        details.ShouldNotBeNull();
        details.Data.ShouldBeEmpty();
        details.IsPooled.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void CreateWithOneKeyValuePair_StoresData()
    {
        // Act
        var details = ResultDetails.Create("StatusCode", 404);

        // Assert
        details.Data.Count.ShouldBe(1);
        details.Data["StatusCode"].ShouldBe(404);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void CreateWithTwoKeyValuePairs_StoresBothPairs()
    {
        // Act
        var details = ResultDetails.Create("StatusCode", 404, "ReasonPhrase", "Not Found");

        // Assert
        details.Data.Count.ShouldBe(2);
        details.Data["StatusCode"].ShouldBe(404);
        details.Data["ReasonPhrase"].ShouldBe("Not Found");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void CreateWithThreeKeyValuePairs_StoresAllThreePairs()
    {
        // Act
        var details = ResultDetails.Create("Key1", "Value1", "Key2", 42, "Key3", true);

        // Assert
        details.Data.Count.ShouldBe(3);
        details.Data["Key1"].ShouldBe("Value1");
        details.Data["Key2"].ShouldBe(42);
        details.Data["Key3"].ShouldBe(true);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void CreateWithNullValues_StoresNulls()
    {
        // Act
        var details = ResultDetails.Create("NullKey", null);

        // Assert
        details.Data.Count.ShouldBe(1);
        details.Data["NullKey"].ShouldBeNull();
    }

    #endregion

    #region With (Fluent Chaining) Tests

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void With_AddsKeyValuePairAndReturnsInstance()
    {
        // Arrange
        var details = ResultDetails.Create();

        // Act
        var result = details.With("Key", "Value");

        // Assert
        result.ShouldBeSameAs(details);
        details.Data.Count.ShouldBe(1);
        details.Data["Key"].ShouldBe("Value");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void With_CanChainMultipleCalls()
    {
        // Act
        var details = ResultDetails.Create()
            .With("First", 1)
            .With("Second", "two")
            .With("Third", 3.0);

        // Assert
        details.Data.Count.ShouldBe(3);
        details.Data["First"].ShouldBe(1);
        details.Data["Second"].ShouldBe("two");
        details.Data["Third"].ShouldBe(3.0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void With_OverwritesExistingKey()
    {
        // Arrange
        var details = ResultDetails.Create("Key", "Original");

        // Act
        details.With("Key", "Updated");

        // Assert
        details.Data["Key"].ShouldBe("Updated");
        details.Data.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void With_ThrowsObjectDisposedExceptionWhenPooled()
    {
        // Arrange
        var details = ResultDetails.Create();
        details.Dispose();

        // Act & Assert
        Should.Throw<ObjectDisposedException>(() => details.With("Key", "Value"));
    }

    #endregion

    #region GetValue Tests

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void GetValueReturnsTypedValueWhenKeyExistsAndTypeMatches()
    {
        // Arrange
        var details = ResultDetails.Create("StatusCode", 404);

        // Act
        var value = details.GetValue<int>("StatusCode");

        // Assert
        value.ShouldBe(404);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void GetValueReturnsDefaultWhenKeyDoesNotExist()
    {
        // Arrange
        var details = ResultDetails.Create();

        // Act
        var value = details.GetValue<int>("NonExistent");

        // Assert
        value.ShouldBe(default);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void GetValueReturnsDefaultWhenTypeDoesNotMatch()
    {
        // Arrange
        var details = ResultDetails.Create("Key", "StringValue");

        // Act
        var value = details.GetValue<int>("Key");

        // Assert
        value.ShouldBe(default);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void GetValueReturnsNullForReferenceTypeWhenKeyMissing()
    {
        // Arrange
        var details = ResultDetails.Create();

        // Act
        var value = details.GetValue<string>("Missing");

        // Assert
        value.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void GetValueReturnsNullWhenStoredValueIsNull()
    {
        // Arrange
        var details = ResultDetails.Create("NullKey", null);

        // Act
        var value = details.GetValue<string>("NullKey");

        // Assert
        value.ShouldBeNull();
    }

    #endregion

    #region Dispose / Pooling Tests

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Dispose_SetsIsPooledToTrue()
    {
        // Arrange
        var details = ResultDetails.Create("Key", "Value");

        // Act
        details.Dispose();

        // Assert
        details.IsPooled.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Dispose_ClearsData()
    {
        // Arrange
        var details = ResultDetails.Create("Key", "Value");

        // Act
        details.Dispose();

        // Assert
        details.Data.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        // Arrange
        var details = ResultDetails.Create("Key", "Value");

        // Act & Assert - second Dispose should be a no-op
        details.Dispose();
        Should.NotThrow(() => details.Dispose());
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void PooledInstanceCanBeReusedViaCreate()
    {
        // Arrange - dispose to return to pool
        var original = ResultDetails.Create("OldKey", "OldValue");
        original.Dispose();

        // Act - get from pool
        var reused = ResultDetails.Create();

        // Assert - should get a clean instance (data cleared during dispose)
        reused.Data.ShouldBeEmpty();
        reused.IsPooled.ShouldBeFalse();
    }

    #endregion

    #region Data Dictionary Tests

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void DataReturnsReadOnlyView()
    {
        // Arrange
        var details = ResultDetails.Create("Key", "Value");

        // Act & Assert
        details.Data.ShouldBeAssignableTo<IReadOnlyDictionary<string, object?>>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void DataKeyLookupIsOrdinalCaseSensitive()
    {
        // Arrange
        var details = ResultDetails.Create("Key", "Value");

        // Act & Assert
        details.Data.ContainsKey("Key").ShouldBeTrue();
        details.Data.ContainsKey("key").ShouldBeFalse();
        details.Data.ContainsKey("KEY").ShouldBeFalse();
    }

    #endregion
}
