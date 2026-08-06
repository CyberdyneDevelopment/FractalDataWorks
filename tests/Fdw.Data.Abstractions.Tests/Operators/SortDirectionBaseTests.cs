using System.Diagnostics.CodeAnalysis;
using Fdw.Data.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.Abstractions.Tests.Operators;

public sealed class SortDirectionBaseTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsId()
    {
        // Arrange & Act
        var direction = new TestSortDirection(1, "Ascending");

        // Assert
        direction.Id.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsName()
    {
        // Arrange & Act
        var direction = new TestSortDirection(1, "Ascending");

        // Assert
        direction.Name.ShouldBe("Ascending");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsSqlKeyword()
    {
        // Arrange & Act
        var direction = new TestSortDirection(1, "Ascending");

        // Assert
        direction.SqlKeyword.ShouldBe("ASC");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsIsAscending()
    {
        // Arrange & Act
        var direction = new TestSortDirection(1, "Ascending", isAscending: true);

        // Assert
        direction.IsAscending.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsDescription()
    {
        // Arrange & Act
        var direction = new TestSortDirection(1, "Ascending");

        // Assert
        direction.Description.ShouldBe("Sort in ascending order");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsConfigurationKey()
    {
        // Arrange & Act
        var direction = new TestSortDirection(1, "Ascending");

        // Assert
        direction.ConfigurationKey.ShouldBe("Data:Operators:Sort:Ascending");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsDisplayName()
    {
        // Arrange & Act
        var direction = new TestSortDirection(1, "Ascending");

        // Assert
        direction.DisplayName.ShouldBe("Ascending Sort Direction");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void CategoryDefaultsToSort()
    {
        // Arrange & Act
        var direction = new TestSortDirection(1, "Ascending");

        // Assert
        direction.Category.ShouldBe("Sort");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void CategoryCanBeCustomized()
    {
        // Arrange & Act
        var direction = new TestSortDirection(2, "Custom", category: "CustomSort");

        // Assert
        direction.Category.ShouldBe("CustomSort");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void DescendingDirectionConfiguration()
    {
        // Arrange & Act
        var direction = new TestSortDirection(
            2,
            "Descending",
            "Sort in descending order",
            isAscending: false,
            sqlKeyword: "DESC");

        // Assert
        direction.Name.ShouldBe("Descending");
        direction.IsAscending.ShouldBeFalse();
        direction.SqlKeyword.ShouldBe("DESC");
        direction.Description.ShouldBe("Sort in descending order");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ImplementsISortDirection()
    {
        // Arrange
        var direction = new TestSortDirection(1, "Ascending");

        // Act & Assert
        direction.ShouldBeAssignableTo<ISortDirection>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void InheritsFromTypeOptionBase()
    {
        // Arrange
        var direction = new TestSortDirection(1, "Ascending");

        // Act & Assert
        direction.ShouldBeAssignableTo<SortDirectionBase>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void MultipleSortDirectionsWithDifferentSettings()
    {
        // Arrange
        var asc = new TestSortDirection(1, "Ascending", isAscending: true, sqlKeyword: "ASC");
        var desc = new TestSortDirection(2, "Descending", isAscending: false, sqlKeyword: "DESC");

        // Act & Assert
        asc.IsAscending.ShouldBeTrue();
        desc.IsAscending.ShouldBeFalse();
        asc.SqlKeyword.ShouldBe("ASC");
        desc.SqlKeyword.ShouldBe("DESC");
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestSortDirection : SortDirectionBase
    {
        public TestSortDirection(
            int id,
            string name,
            string? description = null,
            bool isAscending = true,
            string? sqlKeyword = null,
            string? category = null)
            : base(
                id,
                name,
                description ?? (isAscending ? "Sort in ascending order" : "Sort in descending order"),
                isAscending,
                sqlKeyword ?? (isAscending ? "ASC" : "DESC"),
                category)
        {
        }
    }
}
