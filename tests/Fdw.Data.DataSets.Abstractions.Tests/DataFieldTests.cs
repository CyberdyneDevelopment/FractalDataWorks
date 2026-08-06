using Shouldly;
using Xunit;
using Fdw.Data.DataSets.Abstractions;
using System;
using Moq;
using Fdw.Data.DataContainers.Abstractions;

namespace Fdw.Data.DataSets.Abstractions.Tests;

public class DataFieldTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorInitializesPropertiesWithValidInput()
    {
        // Arrange & Act
        var field = new DataField(
            name: "CustomerId",
            type: typeof(int),
            isKey: true,
            isNullable: false,
            maxLength: null,
            description: "Customer identifier");

        // Assert
        field.Name.ShouldBe("CustomerId");
        field.Type.ShouldBe(typeof(int));
        field.IsKey.ShouldBeTrue();
        field.IsNullable.ShouldBeFalse();
        field.MaxLength.ShouldBeNull();
        field.Description.ShouldBe("Customer identifier");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorThrowsWhenNameIsNull()
    {
        Should.Throw<ArgumentException>(() => new DataField(
            name: null!,
            type: typeof(string)));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorThrowsWhenNameIsEmpty()
    {
        Should.Throw<ArgumentException>(() => new DataField(
            name: string.Empty,
            type: typeof(string)));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorThrowsWhenNameIsWhitespace()
    {
        Should.Throw<ArgumentException>(() => new DataField(
            name: "   ",
            type: typeof(string)));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorThrowsWhenTypeIsNull()
    {
        Should.Throw<ArgumentNullException>(() => new DataField(
            name: "Field1",
            type: null!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DisplayNameReturnsDescriptionWhenProvided()
    {
        // Arrange
        var field = new DataField("Field1", typeof(string), description: "Test Description");

        // Act & Assert
        field.DisplayName.ShouldBe("Test Description");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DisplayNameReturnsNameWhenDescriptionIsNull()
    {
        // Arrange
        var field = new DataField("Field1", typeof(string), description: null);

        // Act & Assert
        field.DisplayName.ShouldBe("Field1");
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    [InlineData(typeof(byte), true)]
    [InlineData(typeof(sbyte), true)]
    [InlineData(typeof(short), true)]
    [InlineData(typeof(ushort), true)]
    [InlineData(typeof(int), true)]
    [InlineData(typeof(uint), true)]
    [InlineData(typeof(long), true)]
    [InlineData(typeof(ulong), true)]
    [InlineData(typeof(float), true)]
    [InlineData(typeof(double), true)]
    [InlineData(typeof(decimal), true)]
    [InlineData(typeof(string), false)]
    [InlineData(typeof(bool), false)]
    [InlineData(typeof(DateTime), false)]
    public void IsNumericReturnsTrueForNumericTypes(Type type, bool expected)
    {
        // Arrange
        var field = new DataField("Field1", type);

        // Act & Assert
        field.IsNumeric.ShouldBe(expected);
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    [InlineData(typeof(int?))]
    [InlineData(typeof(long?))]
    [InlineData(typeof(decimal?))]
    [InlineData(typeof(double?))]
    public void IsNumericReturnsTrueForNullableNumericTypes(Type type)
    {
        // Arrange
        var field = new DataField("Field1", type);

        // Act & Assert
        field.IsNumeric.ShouldBeTrue();
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    [InlineData(typeof(DateTime), true)]
    [InlineData(typeof(DateTime?), true)]
    [InlineData(typeof(DateTimeOffset), true)]
    [InlineData(typeof(DateTimeOffset?), true)]
    [InlineData(typeof(string), false)]
    [InlineData(typeof(int), false)]
    public void IsDateTimeReturnsTrueForDateTimeTypes(Type type, bool expected)
    {
        // Arrange
        var field = new DataField("Field1", type);

        // Act & Assert
        field.IsDateTime.ShouldBe(expected);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IsStringReturnsTrueForStringType()
    {
        // Arrange
        var field = new DataField("Field1", typeof(string));

        // Act & Assert
        field.IsString.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IsStringReturnsFalseForNonStringTypes()
    {
        // Arrange
        var field = new DataField("Field1", typeof(int));

        // Act & Assert
        field.IsString.ShouldBeFalse();
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    [InlineData(typeof(string), null, "NVARCHAR(MAX)")]
    [InlineData(typeof(string), 50, "NVARCHAR(50)")]
    [InlineData(typeof(int), null, "INT")]
    [InlineData(typeof(long), null, "BIGINT")]
    [InlineData(typeof(short), null, "SMALLINT")]
    [InlineData(typeof(byte), null, "TINYINT")]
    [InlineData(typeof(bool), null, "BIT")]
    [InlineData(typeof(DateTime), null, "DATETIME2")]
    [InlineData(typeof(DateTimeOffset), null, "DATETIMEOFFSET")]
    [InlineData(typeof(decimal), null, "DECIMAL(18,2)")]
    [InlineData(typeof(double), null, "FLOAT")]
    [InlineData(typeof(float), null, "REAL")]
    [InlineData(typeof(Guid), null, "UNIQUEIDENTIFIER")]
    public void SqlTypeNameReturnCorrectSqlType(Type type, int? maxLength, string expectedSqlType)
    {
        // Arrange
        var field = new DataField("Field1", type, maxLength: maxLength);

        // Act & Assert
        field.SqlTypeName.ShouldBe(expectedSqlType);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IDataFieldFieldTypeReturnsType()
    {
        // Arrange
        var field = new DataField("Field1", typeof(string));
        IDataField iField = field;

        // Act & Assert
        iField.FieldType.ShouldBe(typeof(string));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IDataFieldIsRequiredReturnsTrueWhenNotNullable()
    {
        // Arrange
        var field = new DataField("Field1", typeof(string), isNullable: false);
        IDataField iField = field;

        // Act & Assert
        iField.IsRequired.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IDataFieldIsRequiredReturnsFalseWhenNullable()
    {
        // Arrange
        var field = new DataField("Field1", typeof(string), isNullable: true);
        IDataField iField = field;

        // Act & Assert
        iField.IsRequired.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IDataFieldDefaultValueReturnsNull()
    {
        // Arrange
        var field = new DataField("Field1", typeof(string));
        IDataField iField = field;

        // Act & Assert
        iField.DefaultValue.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IDataFieldIsCalculatedReturnsFalse()
    {
        // Arrange
        var field = new DataField("Field1", typeof(string));
        IDataField iField = field;

        // Act & Assert
        iField.IsCalculated.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IDataFieldCalculatorReturnsNull()
    {
        // Arrange
        var field = new DataField("Field1", typeof(string));
        IDataField iField = field;

        // Act & Assert
        iField.Calculator.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void EqualsReturnsTrueForIdenticalFields()
    {
        // Arrange
        var field1 = new DataField("Field1", typeof(int), isKey: true, isNullable: false, maxLength: null);
        var field2 = new DataField("Field1", typeof(int), isKey: true, isNullable: false, maxLength: null);

        // Act & Assert
        field1.Equals(field2).ShouldBeTrue();
        (field1 == field2).ShouldBeTrue();
        (field1 != field2).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void EqualsReturnsFalseForDifferentNames()
    {
        // Arrange
        var field1 = new DataField("Field1", typeof(int));
        var field2 = new DataField("Field2", typeof(int));

        // Act & Assert
        field1.Equals(field2).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void EqualsReturnsFalseForDifferentTypes()
    {
        // Arrange
        var field1 = new DataField("Field1", typeof(int));
        var field2 = new DataField("Field1", typeof(string));

        // Act & Assert
        field1.Equals(field2).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void EqualsReturnsFalseForDifferentIsKey()
    {
        // Arrange
        var field1 = new DataField("Field1", typeof(int), isKey: true);
        var field2 = new DataField("Field1", typeof(int), isKey: false);

        // Act & Assert
        field1.Equals(field2).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void EqualsReturnsFalseForDifferentIsNullable()
    {
        // Arrange
        var field1 = new DataField("Field1", typeof(int), isNullable: true);
        var field2 = new DataField("Field1", typeof(int), isNullable: false);

        // Act & Assert
        field1.Equals(field2).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void EqualsReturnsFalseForDifferentMaxLength()
    {
        // Arrange
        var field1 = new DataField("Field1", typeof(string), maxLength: 50);
        var field2 = new DataField("Field1", typeof(string), maxLength: 100);

        // Act & Assert
        field1.Equals(field2).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void EqualsReturnsFalseWhenComparingWithNull()
    {
        // Arrange
        var field1 = new DataField("Field1", typeof(int));

        // Act & Assert
        field1.Equals(null).ShouldBeFalse();
        (field1 == null).ShouldBeFalse();
        (field1 != null).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void EqualsReturnsTrueForSameReference()
    {
        // Arrange
        var field1 = new DataField("Field1", typeof(int));

        // Act & Assert
        field1.Equals(field1).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void EqualsReturnsFalseWhenComparingWithNonDataFieldObject()
    {
        // Arrange
        var field1 = new DataField("Field1", typeof(int));
        var otherObject = new object();

        // Act & Assert
        field1.Equals(otherObject).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void OperatorEqualsReturnsTrueWhenBothAreNull()
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
    public void GetHashCodeReturnsSameValueForEqualFields()
    {
        // Arrange
        var field1 = new DataField("Field1", typeof(int), isKey: true, isNullable: false, maxLength: null);
        var field2 = new DataField("Field1", typeof(int), isKey: true, isNullable: false, maxLength: null);

        // Act & Assert
        field1.GetHashCode().ShouldBe(field2.GetHashCode());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ToStringReturnsFormattedString()
    {
        // Arrange
        var field = new DataField("CustomerId", typeof(int), isKey: true, isNullable: false);

        // Act
        var result = field.ToString();

        // Assert
        result.ShouldContain("CustomerId");
        result.ShouldContain("Int32");
        result.ShouldContain("[Key]");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ToStringIncludesNullableIndicator()
    {
        // Arrange
        var field = new DataField("Name", typeof(string), isNullable: true);

        // Act
        var result = field.ToString();

        // Assert
        result.ShouldContain("?");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ToStringIncludesMaxLengthWhenSpecified()
    {
        // Arrange
        var field = new DataField("Name", typeof(string), maxLength: 50);

        // Act
        var result = field.ToString();

        // Assert
        result.ShouldContain("(50)");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ToStringDoesNotIncludeKeyIndicatorForNonKeyFields()
    {
        // Arrange
        var field = new DataField("Name", typeof(string), isKey: false);

        // Act
        var result = field.ToString();

        // Assert
        result.ShouldNotContain("[Key]");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SqlTypeNameReturnsNVarcharMaxForUnknownTypes()
    {
        // Arrange - Use a custom type not in the mapping
        var field = new DataField("Field1", typeof(DataFieldTests));

        // Act & Assert
        field.SqlTypeName.ShouldBe("NVARCHAR(MAX)");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void EqualsReturnsTrueWhenComparingDataFieldObjectWithItself()
    {
        // Arrange
        var field = new DataField("Field1", typeof(int), isKey: true, isNullable: false);
        object objField = field;

        // Act & Assert
        field.Equals(objField).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IDataFieldFieldTypeAccessedThroughInterface()
    {
        // Arrange
        var field = new DataField("Field1", typeof(int));
        IDataField iField = field;

        // Act
        var fieldType = iField.FieldType;

        // Assert
        fieldType.ShouldBe(typeof(int));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IDataFieldIsRequiredAccessedThroughInterface()
    {
        // Arrange
        var nullableField = new DataField("Field1", typeof(string), isNullable: true);
        var requiredField = new DataField("Field2", typeof(string), isNullable: false);
        IDataField iNullableField = nullableField;
        IDataField iRequiredField = requiredField;

        // Act & Assert
        iNullableField.IsRequired.ShouldBeFalse();
        iRequiredField.IsRequired.ShouldBeTrue();
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void IDataFieldIsRequiredReflectsNullableProperty(bool isNullable, bool expectedRequired)
    {
        // Arrange
        var field = new DataField("Field1", typeof(string), isNullable: isNullable);
        IDataField iField = field;

        // Act
        var isRequired = iField.IsRequired;

        // Assert
        isRequired.ShouldBe(expectedRequired);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ExplicitInterfacePropertiesAccessibleThroughIDataField()
    {
        // Arrange
        var field = new DataField("TestField", typeof(int), isKey: true, isNullable: false);
        IDataField iField = field;

        // Act & Assert - Access all explicit interface properties
        iField.FieldType.ShouldBe(typeof(int));
        iField.IsRequired.ShouldBeTrue();
        iField.DefaultValue.ShouldBeNull();
        iField.IsCalculated.ShouldBeFalse();
        iField.Calculator.ShouldBeNull();
    }
}
