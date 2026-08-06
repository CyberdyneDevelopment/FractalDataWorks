using System.Diagnostics.CodeAnalysis;
using Fdw.Data.Abstractions.FieldAccessors;
using Fdw.Results;
using Fdw.Messages;
using Shouldly;
using Xunit;

namespace Fdw.Data.Abstractions.Tests.FieldAccessors;

public sealed class FieldAccessorBaseTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsTypeName()
    {
        // Arrange & Act
        var accessor = new TestFieldAccessor();

        // Assert
        accessor.Id.ShouldBe("TestEntity");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsNameFromTypeName()
    {
        // Arrange & Act
        var accessor = new TestFieldAccessor();

        // Assert
        accessor.Name.ShouldBe("TestEntity");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TargetTypeIsSet()
    {
        // Arrange & Act
        var accessor = new TestFieldAccessor();

        // Assert
        accessor.TargetType.ShouldBe(typeof(TestEntity));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void FieldNamesReturnsImplementationValue()
    {
        // Arrange
        var accessor = new TestFieldAccessor();

        // Act
        var fieldNames = accessor.FieldNames;

        // Assert
        fieldNames.ShouldNotBeNull();
        fieldNames.Count.ShouldBe(2);
        fieldNames.ShouldContain("Id");
        fieldNames.ShouldContain("Name");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void GetValueReturnsSuccessForValidField()
    {
        // Arrange
        var accessor = new TestFieldAccessor();
        var instance = new TestEntity { Id = 42, Name = "Test" };

        // Act
        var result = accessor.GetValue(instance, "Id");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void GetDecimalValueReturnsSuccessForValidField()
    {
        // Arrange
        var accessor = new TestFieldAccessor();
        var instance = new TestEntity { Id = 42, Name = "Test" };

        // Act
        var result = accessor.GetDecimalValue(instance, "Id");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42m);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void InheritsFromTypeOptionBase()
    {
        // Arrange
        var accessor = new TestFieldAccessor();

        // Act & Assert
        accessor.ShouldBeAssignableTo<FieldAccessorBase>();
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestFieldAccessor : FieldAccessorBase
    {
        public TestFieldAccessor()
            : base("TestEntity", typeof(TestEntity))
        {
        }

        public override IReadOnlyList<string> FieldNames => new[] { "Id", "Name" };

        public override IGenericResult<object?> GetValue(object instance, string fieldName)
        {
            if (instance is not TestEntity entity)
                return GenericResult<object?>.Failure(new GenericMessage("Invalid instance type"));

            return fieldName switch
            {
                "Id" => GenericResult<object?>.Success(entity.Id),
                "Name" => GenericResult<object?>.Success(entity.Name),
                _ => GenericResult<object?>.Failure(new GenericMessage($"Unknown field: {fieldName}"))
            };
        }

        public override IGenericResult<decimal> GetDecimalValue(object instance, string fieldName)
        {
            if (instance is not TestEntity entity)
                return GenericResult<decimal>.Failure(new GenericMessage("Invalid instance type"));

            return fieldName switch
            {
                "Id" => GenericResult<decimal>.Success(entity.Id),
                _ => GenericResult<decimal>.Failure(new GenericMessage($"Field not numeric: {fieldName}"))
            };
        }
    }
}
