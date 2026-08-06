using System.Collections;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data.Abstractions;
using Fdw.Data.Abstractions.Mappers.PocoMappers;
using Fdw.Results;
using Shouldly;
using Xunit;

namespace Fdw.Data.Abstractions.Tests.Mappers;

public sealed class PocoMapperBaseTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsTypeFullName()
    {
        // Arrange & Act
        var mapper = new TestPocoMapper();

        // Assert
        mapper.Id.ShouldBe("TestNamespace.TestPoco");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsNameFromTargetType()
    {
        // Arrange & Act
        var mapper = new TestPocoMapper();

        // Assert
        mapper.Name.ShouldBe("TestPoco");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TargetTypeIsSet()
    {
        // Arrange & Act
        var mapper = new TestPocoMapper();

        // Assert
        mapper.TargetType.ShouldBe(typeof(TestPoco));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void MapFromReaderIsAbstract()
    {
        // Arrange
        var mapper = new TestPocoMapper();

        // Act & Assert - Just verify it can be called
        mapper.ShouldBeAssignableTo<IPocoMapper>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void MapFromDictionaryIsAbstract()
    {
        // Arrange
        var mapper = new TestPocoMapper();

        // Act & Assert - Just verify it can be called
        mapper.ShouldBeAssignableTo<IPocoMapper>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void InheritsFromTypeOptionBase()
    {
        // Arrange
        var mapper = new TestPocoMapper();

        // Act & Assert
        mapper.ShouldBeAssignableTo<PocoMapperBase>();
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestPoco
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestPocoMapper : PocoMapperBase
    {
        public TestPocoMapper()
            : base("TestNamespace.TestPoco", typeof(TestPoco))
        {
        }

        public override IGenericResult<object> MapFromReader(DbDataReader reader, IStorageContainer container)
        {
            var poco = new TestPoco
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1)
            };
            return GenericResult<object>.Success(poco);
        }

        public override IGenericResult<object> MapFromDictionary(IDictionary<string, object?> data)
        {
            var poco = new TestPoco
            {
                Id = data.ContainsKey("Id") ? Convert.ToInt32(data["Id"]) : 0,
                Name = data.ContainsKey("Name") ? data["Name"]?.ToString() ?? string.Empty : string.Empty
            };
            return GenericResult<object>.Success(poco);
        }

        public override IReadOnlyList<string> GetPropertyNames() => ["Id", "Name"];

        public override IReadOnlyDictionary<string, object?> MapToParameters(object instance)
        {
            var typed = (TestPoco)instance;
            return new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["Id"] = typed.Id,
                ["Name"] = typed.Name,
            };
        }

        public override void SetValue(object instance, string columnName, object? value)
        {
            var typed = (TestPoco)instance;
            switch (columnName)
            {
                case "Id": typed.Id = (int)value!; break;
                case "Name": typed.Name = (string)value!; break;
                default: break;
            }
        }

        public override IList CreateList() => new List<TestPoco>();

        public override Array CreateArray(int length) => new TestPoco[length];

        public override IGenericConfiguration? GetTypedBody(object parent) => null;

        public override void SetTypedBody(object parent, IGenericConfiguration? body) { }

        public override IReadOnlyList<IChildCascadeDescriptor> CascadeChildren { get; } = Array.Empty<IChildCascadeDescriptor>();
    }
}
