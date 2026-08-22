using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Commands.Data.Extensions.Tests;

[Collection(nameof(DataTestCollection))]
/// <summary>
/// Tests for QueryCommandBuilder with hierarchical filter groups.
/// </summary>
public sealed class QueryCommandBuilderTests
{
    private const string TestDataStore = "TestDb";
    private const string TestPath = "dbo";
    private const string TestContainer = "Users";

    private sealed class TestEntity
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public bool IsActive { get; set; }
        public string? Status { get; set; }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BuilderShouldCreateBasicQuery()
    {
        // Act
        var call = new QueryCommandBuilder<TestEntity>(TestDataStore, TestPath, TestContainer)
            .Build();

        // Assert
        call.Target.Container.ShouldBe(TestContainer);
        call.Target.DataStore.ShouldBe(TestDataStore);
        call.Target.Path.ShouldBe(TestPath);
        ((QueryCommand<TestEntity>)call.Command).Filter.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BuilderShouldCreateSingleConditionFilter()
    {
        // Act
        var call = new QueryCommandBuilder<TestEntity>(TestDataStore, TestPath, TestContainer)
            .Where(nameof(TestEntity.Name), "John")
            .Build();

        var query = (QueryCommand<TestEntity>)call.Command;

        // Assert
        query.Filter.ShouldNotBeNull();
        query.Filter!.Root.ShouldNotBeNull();
        query.Filter.Root.ShouldBeOfType<FilterCondition>();
        var condition = (FilterCondition)query.Filter.Root;
        condition.PropertyName.ShouldBe("Name");
        condition.Value.ShouldBe("John");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BuilderShouldCreateMultipleConditionsAsAndGroup()
    {
        // Act
        var call = new QueryCommandBuilder<TestEntity>(TestDataStore, TestPath, TestContainer)
            .Where(nameof(TestEntity.Name), "John")
            .Where(nameof(TestEntity.IsActive), true)
            .Build();

        var query = (QueryCommand<TestEntity>)call.Command;

        // Assert
        query.Filter.ShouldNotBeNull();
        query.Filter!.Root.ShouldNotBeNull();
        query.Filter.Root.ShouldBeOfType<FilterGroup>();
        var group = (FilterGroup)query.Filter.Root;
        group.Operator.ShouldBe(LogicalOperator.And);
        group.Nodes.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BuilderShouldSupportExplicitAndGroup()
    {
        // Act
        var call = new QueryCommandBuilder<TestEntity>(TestDataStore, TestPath, TestContainer)
            .BeginAndGroup()
                .Where(nameof(TestEntity.Name), "John")
                .Where(nameof(TestEntity.IsActive), true)
            .EndGroup()
            .Build();

        var query = (QueryCommand<TestEntity>)call.Command;

        // Assert
        query.Filter.ShouldNotBeNull();
        query.Filter!.Root.ShouldNotBeNull();
        query.Filter.Root.ShouldBeOfType<FilterGroup>();
        var group = (FilterGroup)query.Filter.Root;
        group.Operator.ShouldBe(LogicalOperator.And);
        group.Nodes.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BuilderShouldSupportOrGroup()
    {
        // Act
        var call = new QueryCommandBuilder<TestEntity>(TestDataStore, TestPath, TestContainer)
            .BeginOrGroup()
                .Where(nameof(TestEntity.Name), "John")
                .Where(nameof(TestEntity.Name), "Jane")
            .EndGroup()
            .Build();

        var query = (QueryCommand<TestEntity>)call.Command;

        // Assert
        query.Filter.ShouldNotBeNull();
        query.Filter!.Root.ShouldNotBeNull();
        query.Filter.Root.ShouldBeOfType<FilterGroup>();
        var group = (FilterGroup)query.Filter.Root;
        group.Operator.ShouldBe(LogicalOperator.Or);
        group.Nodes.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BuilderShouldSupportNestedGroups()
    {
        // Arrange & Act - ((Name = 'John' OR Name = 'Jane') AND IsActive = true)
        var call = new QueryCommandBuilder<TestEntity>(TestDataStore, TestPath, TestContainer)
            .BeginAndGroup()
                .BeginOrGroup()
                    .Where(nameof(TestEntity.Name), "John")
                    .Where(nameof(TestEntity.Name), "Jane")
                .EndGroup()
                .Where(nameof(TestEntity.IsActive), true)
            .EndGroup()
            .Build();

        var query = (QueryCommand<TestEntity>)call.Command;

        // Assert
        query.Filter.ShouldNotBeNull();
        query.Filter!.Root.ShouldNotBeNull();
        query.Filter.Root.ShouldBeOfType<FilterGroup>();
        var rootGroup = (FilterGroup)query.Filter.Root;
        rootGroup.Operator.ShouldBe(LogicalOperator.And);
        rootGroup.Nodes.Count.ShouldBe(2);
        rootGroup.Nodes[0].ShouldBeOfType<FilterGroup>();
        rootGroup.Nodes[1].ShouldBeOfType<FilterCondition>();

        var orGroup = (FilterGroup)rootGroup.Nodes[0];
        orGroup.Operator.ShouldBe(LogicalOperator.Or);
        orGroup.Nodes.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BuilderShouldSupportComplexNestedGroups()
    {
        // Arrange & Act - (Name = 'John' AND ((Status = 'A' OR Status = 'B') AND IsActive = true))
        var call = new QueryCommandBuilder<TestEntity>(TestDataStore, TestPath, TestContainer)
            .Where(nameof(TestEntity.Name), "John")
            .BeginAndGroup()
                .BeginOrGroup()
                    .Where(nameof(TestEntity.Status), "A")
                    .Where(nameof(TestEntity.Status), "B")
                .EndGroup()
                .Where(nameof(TestEntity.IsActive), true)
            .EndGroup()
            .Build();

        var query = (QueryCommand<TestEntity>)call.Command;

        // Assert
        query.Filter.ShouldNotBeNull();
        query.Filter!.Root.ShouldNotBeNull();
        query.Filter.Root.ShouldBeOfType<FilterGroup>();
        var rootGroup = (FilterGroup)query.Filter.Root;
        rootGroup.Nodes.Count.ShouldBe(2);
        rootGroup.Nodes[0].ShouldBeOfType<FilterCondition>();
        rootGroup.Nodes[1].ShouldBeOfType<FilterGroup>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BuilderShouldSupportOrderBy()
    {
        // Act
        var call = new QueryCommandBuilder<TestEntity>(TestDataStore, TestPath, TestContainer)
            .OrderBy(nameof(TestEntity.Name))
            .Build();

        var query = (QueryCommand<TestEntity>)call.Command;

        // Assert
        query.Ordering.ShouldNotBeNull();
        query.Ordering!.OrderedFields.Count.ShouldBe(1);
        query.Ordering.OrderedFields[0].PropertyName.ShouldBe("Name");
        query.Ordering.OrderedFields[0].Direction.IsAscending.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BuilderShouldSupportOrderByDescending()
    {
        // Act
        var call = new QueryCommandBuilder<TestEntity>(TestDataStore, TestPath, TestContainer)
            .OrderByDescending(nameof(TestEntity.Name))
            .Build();

        var query = (QueryCommand<TestEntity>)call.Command;

        // Assert
        query.Ordering.ShouldNotBeNull();
        query.Ordering!.OrderedFields.Count.ShouldBe(1);
        query.Ordering.OrderedFields[0].Direction.IsAscending.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BuilderShouldSupportMultipleOrderByFields()
    {
        // Act
        var call = new QueryCommandBuilder<TestEntity>(TestDataStore, TestPath, TestContainer)
            .OrderBy(nameof(TestEntity.Name))
            .OrderByDescending(nameof(TestEntity.Id))
            .Build();

        var query = (QueryCommand<TestEntity>)call.Command;

        // Assert
        query.Ordering.ShouldNotBeNull();
        query.Ordering!.OrderedFields.Count.ShouldBe(2);
        query.Ordering.OrderedFields[0].PropertyName.ShouldBe("Name");
        query.Ordering.OrderedFields[1].PropertyName.ShouldBe("Id");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BuilderShouldSupportPaging()
    {
        // Act
        var call = new QueryCommandBuilder<TestEntity>(TestDataStore, TestPath, TestContainer)
            .Paging(skip: 10, take: 25)
            .Build();

        var query = (QueryCommand<TestEntity>)call.Command;

        // Assert
        query.Paging.ShouldNotBeNull();
        query.Paging!.Skip.ShouldBe(10);
        query.Paging.Take.ShouldBe(25);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BuilderShouldSupportCompleteQuery()
    {
        // Act
        var call = new QueryCommandBuilder<TestEntity>(TestDataStore, TestPath, TestContainer)
            .Where(nameof(TestEntity.IsActive), true)
            .BeginOrGroup()
                .Where(nameof(TestEntity.Status), "A")
                .Where(nameof(TestEntity.Status), "B")
            .EndGroup()
            .OrderBy(nameof(TestEntity.Name))
            .Paging(0, 50)
            .Build();

        var query = (QueryCommand<TestEntity>)call.Command;

        // Assert
        call.Target.Container.ShouldBe(TestContainer);
        call.Target.DataStore.ShouldBe(TestDataStore);
        call.Target.Path.ShouldBe(TestPath);
        query.Filter.ShouldNotBeNull();
        query.Ordering.ShouldNotBeNull();
        query.Paging.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DataStoresForShouldCreateBuilderWithFullPath()
    {
        // Act
        var call = DataStores.For("CustomDb")
            .Path("sales")
            .Container<TestEntity>("Customers")
            .Where(nameof(TestEntity.Name), "John")
            .Build();

        // Assert
        call.Target.DataStore.ShouldBe("CustomDb");
        call.Target.Path.ShouldBe("sales");
        call.Target.Container.ShouldBe("Customers");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void QueryFromShouldCreateBuilderDirectly()
    {
        // Act
        var call = Query.From<TestEntity>("CustomDb", "sales", "Customers")
            .Where(nameof(TestEntity.Name), "John")
            .Build();

        // Assert
        call.Target.DataStore.ShouldBe("CustomDb");
        call.Target.Path.ShouldBe("sales");
        call.Target.Container.ShouldBe("Customers");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DataQueryFromShouldCreateBuilderDirectly()
    {
        // Act
        var call = DataQuery.From<TestEntity>("CustomDb", "sales", "Customers")
            .Where(nameof(TestEntity.Name), "John")
            .Build();

        // Assert
        call.Target.DataStore.ShouldBe("CustomDb");
        call.Target.Path.ShouldBe("sales");
        call.Target.Container.ShouldBe("Customers");
    }
}
