using Fdw.Collections;
using Fdw.Data.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.Abstractions.Tests.Expressions;

public sealed class OrderedFieldTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorInitializesPropertiesCorrectly()
    {
        // Arrange & Act
        var field = new OrderedField
        {
            PropertyName = "TestProperty",
            Direction = new TestSortDirection(true)
        };

        // Assert
        field.PropertyName.ShouldBe("TestProperty");
        field.Direction.ShouldNotBeNull();
        field.Direction.IsAscending.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void RecordEqualityWorksCorrectly()
    {
        // Arrange
        var direction = new TestSortDirection(true);
        var field1 = new OrderedField
        {
            PropertyName = "Name",
            Direction = direction
        };

        var field2 = new OrderedField
        {
            PropertyName = "Name",
            Direction = direction
        };

        // Act & Assert
        field1.ShouldBe(field2);
        (field1 == field2).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void RecordInequalityWorksForDifferentPropertyNames()
    {
        // Arrange
        var direction = new TestSortDirection(true);
        var field1 = new OrderedField
        {
            PropertyName = "Name",
            Direction = direction
        };

        var field2 = new OrderedField
        {
            PropertyName = "DifferentName",
            Direction = direction
        };

        // Act & Assert
        field1.ShouldNotBe(field2);
        (field1 != field2).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void RecordInequalityWorksForDifferentDirections()
    {
        // Arrange
        var field1 = new OrderedField
        {
            PropertyName = "Name",
            Direction = new TestSortDirection(true)
        };

        var field2 = new OrderedField
        {
            PropertyName = "Name",
            Direction = new TestSortDirection(false)
        };

        // Act & Assert
        field1.ShouldNotBe(field2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void GetHashCodeIsConsistent()
    {
        // Arrange
        var field = new OrderedField
        {
            PropertyName = "Name",
            Direction = new TestSortDirection(true)
        };

        // Act
        var hash1 = field.GetHashCode();
        var hash2 = field.GetHashCode();

        // Assert
        hash1.ShouldBe(hash2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ToStringReturnsValue()
    {
        // Arrange
        var field = new OrderedField
        {
            PropertyName = "TestProperty",
            Direction = new TestSortDirection(true)
        };

        // Act
        var result = field.ToString();

        // Assert
        result.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ImplementsIOrderedFieldInterface()
    {
        // Arrange
        var field = new OrderedField
        {
            PropertyName = "Test",
            Direction = new TestSortDirection(true)
        };

        // Act & Assert
        field.ShouldBeAssignableTo<IOrderedField>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void CanCreateWithDescendingDirection()
    {
        // Arrange & Act
        var field = new OrderedField
        {
            PropertyName = "Score",
            Direction = new TestSortDirection(false)
        };

        // Assert
        field.PropertyName.ShouldBe("Score");
        field.Direction.IsAscending.ShouldBeFalse();
    }

    private sealed class TestSortDirection : ISortDirection
    {
        public TestSortDirection(bool isAscending)
        {
            IsAscending = isAscending;
        }

        public int Id => IsAscending ? 1 : 2;
        object ITypeOption.Id => Id;
        public string Name => IsAscending ? "Ascending" : "Descending";
        public string Code => IsAscending ? "ASC" : "DESC";
        public string DisplayName => Name;
        public string Description => $"{Name} sort direction";
        public string Category => "Sort";
        public string SqlKeyword => IsAscending ? "ASC" : "DESC";
        public bool IsAscending { get; }
    }
}
