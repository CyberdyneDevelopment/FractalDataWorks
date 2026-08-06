using System;
using Fdw.Commands.Data.Abstractions;
using Fdw.Results;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Shouldly;
using Xunit;

namespace Fdw.Data.DataSets.Abstractions.Tests;

public class DataSetTypeBaseTests
{
    private sealed class TestRecord
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestDataSetType : DataSetTypeBase
    {
        public TestDataSetType(
            int id,
            string name,
            string description,
            Type recordType,
            IReadOnlyCollection<IDataField> fields,
            string? category = null)
            : base(id, name, description, recordType, fields, category)
        {
        }

        public override IDataQuery CreateQuery()
        {
            return new DataQueryBuilder<TestRecord>(Name);
        }

        public override Task<IGenericResult<T>> Execute<T>(
            IDataSetExecutionContext context, IDataCommand command, CancellationToken cancellationToken = default)
            => Task.FromResult(GenericResult<T>.Success(default!));
    }

    private static IReadOnlyCollection<IDataField> CreateTestFields()
    {
        return new List<IDataField>
        {
            new DataField("Id", typeof(int), isKey: true, isNullable: false),
            new DataField("Name", typeof(string), isKey: false, isNullable: false),
            new DataField("Age", typeof(int), isKey: false, isNullable: true)
        };
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorInitializesPropertiesCorrectly()
    {
        // Arrange
        var fields = CreateTestFields();

        // Act
        var sut = new TestDataSetType(1, "TestDataSet", "Test description", typeof(TestRecord), fields);

        // Assert
        sut.Id.ShouldBe(1);
        sut.Name.ShouldBe("TestDataSet");
        sut.Description.ShouldBe("Test description");
        sut.RecordType.ShouldBe(typeof(TestRecord));
        sut.Fields.ShouldBe(fields);
        sut.Category.ShouldBe("Dataset");
        sut.ConfigurationSection.ShouldBe("DataSets:TestDataSet");
        sut.Version.ShouldBe("1.0");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorUsesProvidedCategory()
    {
        // Arrange
        var fields = CreateTestFields();

        // Act
        var sut = new TestDataSetType(1, "TestDataSet", "Test description", typeof(TestRecord), fields, "CustomCategory");

        // Assert
        sut.Category.ShouldBe("CustomCategory");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorWithNullFieldsUsesEmptyCollection()
    {
        // Arrange & Act
        var sut = new TestDataSetType(1, "TestDataSet", "Test description", typeof(TestRecord), null!);

        // Assert
        sut.Fields.ShouldNotBeNull();
        sut.Fields.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void KeyFieldsReturnsOnlyKeyFields()
    {
        // Arrange
        var fields = CreateTestFields();
        var sut = new TestDataSetType(1, "TestDataSet", "Test description", typeof(TestRecord), fields);

        // Act
        var keyFields = sut.KeyFields;

        // Assert
        keyFields.Count.ShouldBe(1);
        keyFields.ShouldContain("Id");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetReturnsFieldByCaseSensitiveName()
    {
        // Arrange
        var fields = CreateTestFields();
        var sut = new TestDataSetType(1, "TestDataSet", "Test description", typeof(TestRecord), fields);

        // Act
        var field = sut.Get("Name");

        // Assert
        field.ShouldNotBeNull();
        field.Name.ShouldBe("Name");
        field.FieldType.ShouldBe(typeof(string));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetIsCaseInsensitive()
    {
        // Arrange
        var fields = CreateTestFields();
        var sut = new TestDataSetType(1, "TestDataSet", "Test description", typeof(TestRecord), fields);

        // Act
        var field = sut.Get("name");

        // Assert
        field.ShouldNotBeNull();
        field.Name.ShouldBe("Name");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetThrowsWhenFieldNotFound()
    {
        // Arrange
        var fields = CreateTestFields();
        var sut = new TestDataSetType(1, "TestDataSet", "Test description", typeof(TestRecord), fields);

        // Act & Assert
        var ex = Should.Throw<ArgumentException>(() => sut.Get("NonExistent"));
        ex.Message.ShouldContain("Field 'NonExistent' not found");
        ex.ParamName.ShouldBe("fieldName");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void HasFieldReturnsTrueForExistingField()
    {
        // Arrange
        var fields = CreateTestFields();
        var sut = new TestDataSetType(1, "TestDataSet", "Test description", typeof(TestRecord), fields);

        // Act
        var result = sut.HasField("Name");

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void HasFieldReturnsFalseForNonExistingField()
    {
        // Arrange
        var fields = CreateTestFields();
        var sut = new TestDataSetType(1, "TestDataSet", "Test description", typeof(TestRecord), fields);

        // Act
        var result = sut.HasField("NonExistent");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void HasFieldIsCaseInsensitive()
    {
        // Arrange
        var fields = CreateTestFields();
        var sut = new TestDataSetType(1, "TestDataSet", "Test description", typeof(TestRecord), fields);

        // Act
        var result = sut.HasField("name");

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void HasFieldReturnsFalseForNull()
    {
        // Arrange
        var fields = CreateTestFields();
        var sut = new TestDataSetType(1, "TestDataSet", "Test description", typeof(TestRecord), fields);

        // Act
        var result = sut.HasField(null!);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetReturnsMatchingFields()
    {
        // Arrange
        var fields = CreateTestFields();
        var sut = new TestDataSetType(1, "TestDataSet", "Test description", typeof(TestRecord), fields);

        // Act
        var intFields = sut.Get(typeof(int)).ToList();

        // Assert
        intFields.Count.ShouldBe(2);
        intFields.ShouldContain(f => f.Name == "Id");
        intFields.ShouldContain(f => f.Name == "Age");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetReturnsEmptyForNoMatches()
    {
        // Arrange
        var fields = CreateTestFields();
        var sut = new TestDataSetType(1, "TestDataSet", "Test description", typeof(TestRecord), fields);

        // Act
        var decimalFields = sut.Get(typeof(decimal)).ToList();

        // Assert
        decimalFields.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetThrowsForNullType()
    {
        // Arrange
        var fields = CreateTestFields();
        var sut = new TestDataSetType(1, "TestDataSet", "Test description", typeof(TestRecord), fields);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => sut.Get((Type)null!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetKeyFieldDefinitionsReturnsOnlyKeyFields()
    {
        // Arrange
        var fields = CreateTestFields();
        var sut = new TestDataSetType(1, "TestDataSet", "Test description", typeof(TestRecord), fields);

        // Act
        var keyFieldDefs = sut.GetKeyFieldDefinitions().ToList();

        // Assert
        keyFieldDefs.Count.ShouldBe(1);
        keyFieldDefs[0].Name.ShouldBe("Id");
        keyFieldDefs[0].IsKey.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetKeyFieldDefinitionsReturnsEmptyWhenNoKeyFields()
    {
        // Arrange
        var fields = new List<IDataField>
        {
            new DataField("Name", typeof(string), isKey: false, isNullable: false)
        };
        var sut = new TestDataSetType(1, "TestDataSet", "Test description", typeof(TestRecord), fields);

        // Act
        var keyFieldDefs = sut.GetKeyFieldDefinitions().ToList();

        // Assert
        keyFieldDefs.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CreateQueryGenericReturnsDataQueryBuilder()
    {
        // Arrange
        var fields = CreateTestFields();
        var sut = new TestDataSetType(1, "TestDataSet", "Test description", typeof(TestRecord), fields);

        // Act
        var query = sut.CreateQuery<TestRecord>();

        // Assert
        query.ShouldNotBeNull();
        query.ShouldBeOfType<DataQueryBuilder<TestRecord>>();
        query.DataSetName.ShouldBe("TestDataSet");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CreateQueryReturnsDataQueryBuilder()
    {
        // Arrange
        var fields = CreateTestFields();
        var sut = new TestDataSetType(1, "TestDataSet", "Test description", typeof(TestRecord), fields);

        // Act
        var query = sut.CreateQuery();

        // Assert
        query.ShouldNotBeNull();
        query.DataSetName.ShouldBe("TestDataSet");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ToStringReturnsDescriptiveFormat()
    {
        // Arrange
        var fields = CreateTestFields();
        var sut = new TestDataSetType(1, "TestDataSet", "Test description", typeof(TestRecord), fields);

        // Act
        var result = sut.ToString();

        // Assert
        result.ShouldBe("DataSet: TestDataSet - Test description (3 fields)");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ToStringShowsZeroFieldsWhenEmpty()
    {
        // Arrange
        var sut = new TestDataSetType(1, "TestDataSet", "Test description", typeof(TestRecord), Array.Empty<IDataField>());

        // Act
        var result = sut.ToString();

        // Assert
        result.ShouldContain("(0 fields)");
    }
}
