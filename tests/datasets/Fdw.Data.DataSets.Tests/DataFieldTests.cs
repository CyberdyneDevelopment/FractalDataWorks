using System;
using Fdw.Data.DataSets.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.DataSets.Tests;

public class DataFieldTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Act
        var field = new DataField("TestField", typeof(string), isKey: false, isNullable: true, maxLength: 100, description: "Test Description");

        // Assert
        field.Name.ShouldBe("TestField");
        field.Type.ShouldBe(typeof(string));
        field.IsKey.ShouldBeFalse();
        field.IsNullable.ShouldBeTrue();
        field.MaxLength.ShouldBe(100);
        field.Description.ShouldBe("Test Description");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Constructor_WithNullName_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentException>(() =>
            new DataField(null!, typeof(string)));

        exception.ParamName.ShouldBe("name");
        exception.Message.ShouldContain("cannot be null or whitespace");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Constructor_WithEmptyName_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentException>(() =>
            new DataField(string.Empty, typeof(string)));

        exception.ParamName.ShouldBe("name");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Constructor_WithWhitespaceName_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentException>(() =>
            new DataField("   ", typeof(string)));

        exception.ParamName.ShouldBe("name");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DisplayName_WithDescription_ReturnsDescription()
    {
        // Arrange
        var field = new DataField("TestField", typeof(string), description: "My Description");

        // Act & Assert
        field.DisplayName.ShouldBe("My Description");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DisplayName_WithoutDescription_ReturnsName()
    {
        // Arrange
        var field = new DataField("TestField", typeof(string));

        // Act & Assert
        field.DisplayName.ShouldBe("TestField");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IsNumeric_ForInt_ReturnsTrue()
    {
        // Arrange
        var field = new DataField("TestField", typeof(int));

        // Act & Assert
        field.IsNumeric.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IsNumeric_ForLong_ReturnsTrue()
    {
        // Arrange
        var field = new DataField("TestField", typeof(long));

        // Act & Assert
        field.IsNumeric.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IsNumeric_ForDecimal_ReturnsTrue()
    {
        // Arrange
        var field = new DataField("TestField", typeof(decimal));

        // Act & Assert
        field.IsNumeric.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IsNumeric_ForDouble_ReturnsTrue()
    {
        // Arrange
        var field = new DataField("TestField", typeof(double));

        // Act & Assert
        field.IsNumeric.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IsNumeric_ForFloat_ReturnsTrue()
    {
        // Arrange
        var field = new DataField("TestField", typeof(float));

        // Act & Assert
        field.IsNumeric.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IsNumeric_ForShort_ReturnsTrue()
    {
        // Arrange
        var field = new DataField("TestField", typeof(short));

        // Act & Assert
        field.IsNumeric.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IsNumeric_ForByte_ReturnsTrue()
    {
        // Arrange
        var field = new DataField("TestField", typeof(byte));

        // Act & Assert
        field.IsNumeric.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IsNumeric_ForSByte_ReturnsTrue()
    {
        // Arrange
        var field = new DataField("TestField", typeof(sbyte));

        // Act & Assert
        field.IsNumeric.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IsNumeric_ForUInt_ReturnsTrue()
    {
        // Arrange
        var field = new DataField("TestField", typeof(uint));

        // Act & Assert
        field.IsNumeric.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IsNumeric_ForULong_ReturnsTrue()
    {
        // Arrange
        var field = new DataField("TestField", typeof(ulong));

        // Act & Assert
        field.IsNumeric.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IsNumeric_ForUShort_ReturnsTrue()
    {
        // Arrange
        var field = new DataField("TestField", typeof(ushort));

        // Act & Assert
        field.IsNumeric.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IsNumeric_ForNullableInt_ReturnsTrue()
    {
        // Arrange
        var field = new DataField("TestField", typeof(int?));

        // Act & Assert
        field.IsNumeric.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IsNumeric_ForString_ReturnsFalse()
    {
        // Arrange
        var field = new DataField("TestField", typeof(string));

        // Act & Assert
        field.IsNumeric.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IsDateTime_ForDateTime_ReturnsTrue()
    {
        // Arrange
        var field = new DataField("TestField", typeof(DateTime));

        // Act & Assert
        field.IsDateTime.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IsDateTime_ForNullableDateTime_ReturnsTrue()
    {
        // Arrange
        var field = new DataField("TestField", typeof(DateTime?));

        // Act & Assert
        field.IsDateTime.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IsDateTime_ForDateTimeOffset_ReturnsTrue()
    {
        // Arrange
        var field = new DataField("TestField", typeof(DateTimeOffset));

        // Act & Assert
        field.IsDateTime.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IsDateTime_ForNullableDateTimeOffset_ReturnsTrue()
    {
        // Arrange
        var field = new DataField("TestField", typeof(DateTimeOffset?));

        // Act & Assert
        field.IsDateTime.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IsDateTime_ForString_ReturnsFalse()
    {
        // Arrange
        var field = new DataField("TestField", typeof(string));

        // Act & Assert
        field.IsDateTime.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IsString_ForString_ReturnsTrue()
    {
        // Arrange
        var field = new DataField("TestField", typeof(string));

        // Act & Assert
        field.IsString.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IsString_ForInt_ReturnsFalse()
    {
        // Arrange
        var field = new DataField("TestField", typeof(int));

        // Act & Assert
        field.IsString.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Equals_WithSameValues_ReturnsTrue()
    {
        // Arrange
        var field1 = new DataField("TestField", typeof(string), isKey: true, isNullable: false, maxLength: 100);
        var field2 = new DataField("TestField", typeof(string), isKey: true, isNullable: false, maxLength: 100);

        // Act & Assert
        field1.Equals(field2).ShouldBeTrue();
        (field1 == field2).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Equals_WithDifferentName_ReturnsFalse()
    {
        // Arrange
        var field1 = new DataField("Field1", typeof(string));
        var field2 = new DataField("Field2", typeof(string));

        // Act & Assert
        field1.Equals(field2).ShouldBeFalse();
        (field1 != field2).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Equals_WithDifferentType_ReturnsFalse()
    {
        // Arrange
        var field1 = new DataField("TestField", typeof(string));
        var field2 = new DataField("TestField", typeof(int));

        // Act & Assert
        field1.Equals(field2).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Equals_WithDifferentIsKey_ReturnsFalse()
    {
        // Arrange
        var field1 = new DataField("TestField", typeof(string), isKey: true);
        var field2 = new DataField("TestField", typeof(string), isKey: false);

        // Act & Assert
        field1.Equals(field2).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Equals_WithDifferentIsNullable_ReturnsFalse()
    {
        // Arrange
        var field1 = new DataField("TestField", typeof(string), isNullable: true);
        var field2 = new DataField("TestField", typeof(string), isNullable: false);

        // Act & Assert
        field1.Equals(field2).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Equals_WithDifferentMaxLength_ReturnsFalse()
    {
        // Arrange
        var field1 = new DataField("TestField", typeof(string), maxLength: 100);
        var field2 = new DataField("TestField", typeof(string), maxLength: 200);

        // Act & Assert
        field1.Equals(field2).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Equals_WithNull_ReturnsFalse()
    {
        // Arrange
        var field = new DataField("TestField", typeof(string));

        // Act & Assert
        field.Equals(null).ShouldBeFalse();
        (field == null).ShouldBeFalse();
        (null == field).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Equals_WithSameReference_ReturnsTrue()
    {
        // Arrange
        var field = new DataField("TestField", typeof(string));

        // Act & Assert
        field.Equals(field).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Equals_WithObject_WorksCorrectly()
    {
        // Arrange
        var field1 = new DataField("TestField", typeof(string));
        var field2 = new DataField("TestField", typeof(string));
        object obj = field2;

        // Act & Assert
        field1.Equals(obj).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Equals_WithDifferentObjectType_ReturnsFalse()
    {
        // Arrange
        var field = new DataField("TestField", typeof(string));
        object obj = "string";

        // Act & Assert
        field.Equals(obj).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetHashCode_WithSameValues_ReturnsSameHashCode()
    {
        // Arrange
        var field1 = new DataField("TestField", typeof(string), isKey: true, isNullable: false, maxLength: 100);
        var field2 = new DataField("TestField", typeof(string), isKey: true, isNullable: false, maxLength: 100);

        // Act & Assert
        field1.GetHashCode().ShouldBe(field2.GetHashCode());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ToString_WithKeyField_IncludesKeyIndicator()
    {
        // Arrange
        var field = new DataField("Id", typeof(int), isKey: true);

        // Act
        var result = field.ToString();

        // Assert
        result.ShouldContain("Id");
        result.ShouldContain("Int32");
        result.ShouldContain("[Key]");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ToString_WithNullableField_IncludesNullableIndicator()
    {
        // Arrange
        var field = new DataField("Name", typeof(string), isNullable: true);

        // Act
        var result = field.ToString();

        // Assert
        result.ShouldContain("Name");
        result.ShouldContain("String");
        result.ShouldContain("?");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ToString_WithMaxLength_IncludesLength()
    {
        // Arrange
        var field = new DataField("Name", typeof(string), maxLength: 50);

        // Act
        var result = field.ToString();

        // Assert
        result.ShouldContain("Name");
        result.ShouldContain("(50)");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ToString_WithAllFeatures_IncludesAllIndicators()
    {
        // Arrange
        var field = new DataField("Id", typeof(int), isKey: true, isNullable: true, maxLength: 10);

        // Act
        var result = field.ToString();

        // Assert
        result.ShouldContain("Id");
        result.ShouldContain("?");
        result.ShouldContain("(10)");
        result.ShouldContain("[Key]");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void OperatorEquality_WithBothNull_ReturnsTrue()
    {
        // Arrange
        DataField? field1 = null;
        DataField? field2 = null;

        // Act & Assert
        (field1 == field2).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void OperatorInequality_WithBothNull_ReturnsFalse()
    {
        // Arrange
        DataField? field1 = null;
        DataField? field2 = null;

        // Act & Assert
        (field1 != field2).ShouldBeFalse();
    }
}
