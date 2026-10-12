using Fdw.Data.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.DataContainers.Abstractions.Tests;

public class DataRowTests
{
    private readonly IDataSchema _testSchema;

    public DataRowTests()
    {
        _testSchema = DataSchema.FromFields([
            new SchemaField("Id", typeof(int), 0),
            new SchemaField("Name", typeof(string), 1),
            new SchemaField("Amount", typeof(decimal), 2),
            new SchemaField("IsActive", typeof(bool), 3)
        ]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorThrowsWhenValueCountDoesNotMatchSchema()
    {
        // Arrange
        var values = new object?[] { 1, "Test" }; // Only 2 values, schema has 4

        // Act & Assert
        var ex = Should.Throw<ArgumentException>(() => new DataRow(_testSchema, values));
        ex.Message.ShouldContain("Values array length (2) does not match schema field count (4)");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void FieldCountReturnsCorrectCount()
    {
        // Arrange
        var values = new object?[] { 1, "Test", 100m, true };
        var row = new DataRow(_testSchema, values);

        // Act & Assert
        row.FieldCount.ShouldBe(4);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void FieldNamesReturnsAllFieldNames()
    {
        // Arrange
        var values = new object?[] { 1, "Test", 100m, true };
        var row = new DataRow(_testSchema, values);

        // Act
        var names = row.FieldNames;

        // Assert
        names.Count.ShouldBe(4);
        names.ShouldContain("Id");
        names.ShouldContain("Name");
        names.ShouldContain("Amount");
        names.ShouldContain("IsActive");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void GetValueByNameReturnsCorrectValue()
    {
        // Arrange
        var values = new object?[] { 42, "TestName", 123.45m, true };
        var row = new DataRow(_testSchema, values);

        // Act & Assert
        row.GetValue<int>("Id").ShouldBe(42);
        row.GetValue<string>("Name").ShouldBe("TestName");
        row.GetValue<decimal>("Amount").ShouldBe(123.45m);
        row.GetValue<bool>("IsActive").ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void GetValueByOrdinalReturnsCorrectValue()
    {
        // Arrange
        var values = new object?[] { 42, "TestName", 123.45m, true };
        var row = new DataRow(_testSchema, values);

        // Act & Assert
        row.GetValue<int>(0).ShouldBe(42);
        row.GetValue<string>(1).ShouldBe("TestName");
        row.GetValue<decimal>(2).ShouldBe(123.45m);
        row.GetValue<bool>(3).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void GetValueByOrdinalThrowsForNegativeOrdinal()
    {
        // Arrange
        var values = new object?[] { 1, "Test", 100m, true };
        var row = new DataRow(_testSchema, values);

        // Act & Assert
        var ex = Should.Throw<ArgumentOutOfRangeException>(() => row.GetValue<int>(-1));
        ex.Message.ShouldContain("Ordinal -1 out of range");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void GetValueByOrdinalThrowsForOrdinalTooLarge()
    {
        // Arrange
        var values = new object?[] { 1, "Test", 100m, true };
        var row = new DataRow(_testSchema, values);

        // Act & Assert
        var ex = Should.Throw<ArgumentOutOfRangeException>(() => row.GetValue<int>(10));
        ex.Message.ShouldContain("Ordinal 10 out of range");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void GetValueThrowsWhenCastingNullToNonNullableType()
    {
        // Arrange
        var values = new object?[] { 1, null, 100m, true };
        var row = new DataRow(_testSchema, values);

        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() => row.GetValue<int>("Name"));
        ex.Message.ShouldContain("Cannot cast null to non-nullable type");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void GetValueReturnsDefaultForNullToNullableType()
    {
        // Arrange
        var values = new object?[] { 1, null, 100m, true };
        var row = new DataRow(_testSchema, values);

        // Act
        var result = row.GetValue<int?>("Name");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void GetValueConvertsCompatibleTypes()
    {
        // Arrange - Store int, retrieve as long
        var values = new object?[] { 42, "Test", 100m, true };
        var row = new DataRow(_testSchema, values);

        // Act
        var result = row.GetValue<long>("Id");

        // Assert
        result.ShouldBe(42L);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void GetValueThrowsInvalidCastExceptionForIncompatibleTypes()
    {
        // Arrange
        var values = new object?[] { 1, "NotANumber", 100m, true };
        var row = new DataRow(_testSchema, values);

        // Act & Assert
        var ex = Should.Throw<InvalidCastException>(() => row.GetValue<int>("Name"));
        ex.Message.ShouldContain("Cannot cast field value of type String to Int32");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TryGetValueByNameReturnsSuccessForExistingField()
    {
        // Arrange
        var values = new object?[] { 42, "Test", 100m, true };
        var row = new DataRow(_testSchema, values);

        // Act
        var success = row.TryGetValue<int>("Id", out var result);

        // Assert
        success.ShouldBeTrue();
        result.ShouldBe(42);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TryGetValueByNameReturnsFailureForNonExistentField()
    {
        // Arrange
        var values = new object?[] { 42, "Test", 100m, true };
        var row = new DataRow(_testSchema, values);

        // Act
        var success = row.TryGetValue<int>("NonExistent", out var result);

        // Assert
        success.ShouldBeFalse();
        result.ShouldBe(default(int));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TryGetValueByOrdinalReturnsSuccessForValidOrdinal()
    {
        // Arrange
        var values = new object?[] { 42, "Test", 100m, true };
        var row = new DataRow(_testSchema, values);

        // Act
        var success = row.TryGetValue<int>(0, out var result);

        // Assert
        success.ShouldBeTrue();
        result.ShouldBe(42);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TryGetValueByOrdinalReturnsFailureForInvalidOrdinal()
    {
        // Arrange
        var values = new object?[] { 42, "Test", 100m, true };
        var row = new DataRow(_testSchema, values);

        // Act
        var success = row.TryGetValue<int>(-1, out var result);

        // Assert
        success.ShouldBeFalse();
        result.ShouldBe(default(int));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TryGetValueByOrdinalReturnsFailureForOrdinalTooLarge()
    {
        // Arrange
        var values = new object?[] { 42, "Test", 100m, true };
        var row = new DataRow(_testSchema, values);

        // Act
        var success = row.TryGetValue<int>(10, out var result);

        // Assert
        success.ShouldBeFalse();
        result.ShouldBe(default(int));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TryGetValueReturnsSuccessForNullToNullableType()
    {
        // Arrange
        var values = new object?[] { 1, null, 100m, true };
        var row = new DataRow(_testSchema, values);

        // Act
        var success = row.TryGetValue<string?>(1, out var result);

        // Assert
        success.ShouldBeTrue();
        result.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TryGetValueReturnsFailureForIncompatibleTypeConversion()
    {
        // Arrange
        var values = new object?[] { 1, "NotANumber", 100m, true };
        var row = new DataRow(_testSchema, values);

        // Act
        var success = row.TryGetValue<int>("Name", out var result);

        // Assert
        success.ShouldBeFalse();
        result.ShouldBe(default(int));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void GetValueUntypedByNameReturnsCorrectValue()
    {
        // Arrange
        var values = new object?[] { 42, "Test", 100m, true };
        var row = new DataRow(_testSchema, values);

        // Act
        var result = row.GetValue("Name");

        // Assert
        result.ShouldBe("Test");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void GetValueUntypedByOrdinalReturnsCorrectValue()
    {
        // Arrange
        var values = new object?[] { 42, "Test", 100m, true };
        var row = new DataRow(_testSchema, values);

        // Act
        var result = row.GetValue(2);

        // Assert
        result.ShouldBe(100m);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void GetValueUntypedByOrdinalThrowsForInvalidOrdinal()
    {
        // Arrange
        var values = new object?[] { 42, "Test", 100m, true };
        var row = new DataRow(_testSchema, values);

        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => row.GetValue(10));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void GetValueUntypedByOrdinalThrowsForNegativeOrdinal()
    {
        // Arrange
        var values = new object?[] { 42, "Test", 100m, true };
        var row = new DataRow(_testSchema, values);

        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => row.GetValue(-1));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void HasFieldReturnsTrueForExistingField()
    {
        // Arrange
        var values = new object?[] { 1, "Test", 100m, true };
        var row = new DataRow(_testSchema, values);

        // Act & Assert
        row.HasField("Id").ShouldBeTrue();
        row.HasField("Name").ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void HasFieldReturnsFalseForNonExistentField()
    {
        // Arrange
        var values = new object?[] { 1, "Test", 100m, true };
        var row = new DataRow(_testSchema, values);

        // Act & Assert
        row.HasField("NonExistent").ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AsDictionaryReturnsCorrectDictionary()
    {
        // Arrange
        var values = new object?[] { 42, "Test", 100m, true };
        var row = new DataRow(_testSchema, values);

        // Act
        var dict = row.AsDictionary();

        // Assert
        dict.Count.ShouldBe(4);
        dict["Id"].ShouldBe(42);
        dict["Name"].ShouldBe("Test");
        dict["Amount"].ShouldBe(100m);
        dict["IsActive"].ShouldBe(true);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AsDictionaryCachesResult()
    {
        // Arrange
        var values = new object?[] { 42, "Test", 100m, true };
        var row = new DataRow(_testSchema, values);

        // Act
        var dict1 = row.AsDictionary();
        var dict2 = row.AsDictionary();

        // Assert - Same reference means cached
        ReferenceEquals(dict1, dict2).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void FromDictionaryCreatesRowWithAllFields()
    {
        // Arrange
        var dict = new Dictionary<string, object?>
        {
            ["Id"] = 42,
            ["Name"] = "Test",
            ["Amount"] = 100m,
            ["IsActive"] = true
        };

        // Act
        var row = DataRow.FromDictionary(_testSchema, dict);

        // Assert
        row.GetValue<int>("Id").ShouldBe(42);
        row.GetValue<string>("Name").ShouldBe("Test");
        row.GetValue<decimal>("Amount").ShouldBe(100m);
        row.GetValue<bool>("IsActive").ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void FromDictionaryUsesNullForMissingFields()
    {
        // Arrange
        var dict = new Dictionary<string, object?>
        {
            ["Id"] = 42,
            ["Name"] = "Test"
            // Amount and IsActive missing
        };

        // Act
        var row = DataRow.FromDictionary(_testSchema, dict);

        // Assert
        row.GetValue<int>("Id").ShouldBe(42);
        row.GetValue<string>("Name").ShouldBe("Test");
        row.GetValue(2).ShouldBeNull(); // Amount
        row.GetValue(3).ShouldBeNull(); // IsActive
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void SingleFieldCreatesRowWithOneField()
    {
        // Act
        var row = DataRow.SingleField("Value", 42);

        // Assert
        row.FieldCount.ShouldBe(1);
        row.GetValue<int>("Value").ShouldBe(42);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void SingleFieldHandlesNullValue()
    {
        // Act
        var row = DataRow.SingleField("Value", null);

        // Assert
        row.FieldCount.ShouldBe(1);
        row.GetValue("Value").ShouldBeNull();
    }
}
