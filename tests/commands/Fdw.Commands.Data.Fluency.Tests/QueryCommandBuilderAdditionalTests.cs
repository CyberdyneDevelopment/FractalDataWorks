using System;
using Fdw.Commands.Data;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Commands.Data.Extensions.Tests;

/// <summary>
/// Additional tests for QueryCommandBuilder covering uncovered branches.
/// </summary>
public sealed class QueryCommandBuilderAdditionalTests
{
    private const string TestDataStore = "TestDb";
    private const string TestPath = "dbo";
    private const string TestContainer = "Users";

    private sealed class TestEntity
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void EndGroupThrowsWhenNoGroupStarted()
    {
        var builder = new QueryCommandBuilder<TestEntity>(TestDataStore, TestPath, TestContainer);

        Should.Throw<InvalidOperationException>(() => builder.EndGroup());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WhereWithExplicitOperatorCreatesCondition()
    {
        var call = new QueryCommandBuilder<TestEntity>(TestDataStore, TestPath, TestContainer)
            .Where("Name", new GreaterThanOperator(), "A")
            .Build();

        var query = (QueryCommand<TestEntity>)call.Command;
        query.Filter.ShouldNotBeNull();
        var condition = query.Filter!.Root.ShouldBeOfType<FilterCondition>();
        condition.Operator.ShouldBeOfType<GreaterThanOperator>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TypeSafeOrderByCreatesAscendingOrder()
    {
        var call = new QueryCommandBuilder<TestEntity>(TestDataStore, TestPath, TestContainer)
            .OrderBy(e => e.Name)
            .Build();

        var query = (QueryCommand<TestEntity>)call.Command;
        query.Ordering.ShouldNotBeNull();
        query.Ordering!.OrderedFields.Count.ShouldBe(1);
        query.Ordering.OrderedFields[0].PropertyName.ShouldBe("Name");
        query.Ordering.OrderedFields[0].Direction.IsAscending.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TypeSafeOrderByDescendingCreatesDescendingOrder()
    {
        var call = new QueryCommandBuilder<TestEntity>(TestDataStore, TestPath, TestContainer)
            .OrderByDescending(e => e.CreatedDate)
            .Build();

        var query = (QueryCommand<TestEntity>)call.Command;
        query.Ordering.ShouldNotBeNull();
        query.Ordering!.OrderedFields.Count.ShouldBe(1);
        query.Ordering.OrderedFields[0].PropertyName.ShouldBe("CreatedDate");
        query.Ordering.OrderedFields[0].Direction.IsAscending.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void PagingWithFiltersAndOrderingPreservesAll()
    {
        var call = new QueryCommandBuilder<TestEntity>(TestDataStore, TestPath, TestContainer)
            .Where("IsActive", true)
            .OrderBy("Name")
            .Paging(skip: 20, take: 10)
            .Build();

        var query = (QueryCommand<TestEntity>)call.Command;
        query.Filter.ShouldNotBeNull();
        query.Ordering.ShouldNotBeNull();
        query.Paging.ShouldNotBeNull();
        query.Paging!.Skip.ShouldBe(20);
        query.Paging.Take.ShouldBe(10);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void PagingDefaultTakeIsThousand()
    {
        var call = new QueryCommandBuilder<TestEntity>(TestDataStore, TestPath, TestContainer)
            .Paging()
            .Build();

        var query = (QueryCommand<TestEntity>)call.Command;
        query.Paging.ShouldNotBeNull();
        query.Paging!.Skip.ShouldBe(0);
        query.Paging.Take.ShouldBe(1000);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void NestedGroupEndGroupAddsToParentGroup()
    {
        var call = new QueryCommandBuilder<TestEntity>(TestDataStore, TestPath, TestContainer)
            .BeginAndGroup()
                .BeginOrGroup()
                    .Where("Name", "A")
                    .Where("Name", "B")
                .EndGroup()
                .BeginOrGroup()
                    .Where("Name", "C")
                    .Where("Name", "D")
                .EndGroup()
            .EndGroup()
            .Build();

        var query = (QueryCommand<TestEntity>)call.Command;
        query.Filter.ShouldNotBeNull();
        var rootGroup = query.Filter!.Root.ShouldBeOfType<FilterGroup>();
        rootGroup.Operator.ShouldBe(LogicalOperator.And);
        rootGroup.Nodes.Count.ShouldBe(2);
        rootGroup.Nodes[0].ShouldBeOfType<FilterGroup>();
        rootGroup.Nodes[1].ShouldBeOfType<FilterGroup>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void EndGroupAddsToExistingRootGroup()
    {
        // First EndGroup sets rootGroup, second EndGroup adds to existing rootGroup
        var call = new QueryCommandBuilder<TestEntity>(TestDataStore, TestPath, TestContainer)
            .Where("Name", "Existing")
            .BeginOrGroup()
                .Where("Status", "A")
                .Where("Status", "B")
            .EndGroup()
            .Build();

        var query = (QueryCommand<TestEntity>)call.Command;
        query.Filter.ShouldNotBeNull();
        var rootGroup = query.Filter!.Root.ShouldBeOfType<FilterGroup>();
        rootGroup.Nodes.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BuildWithNoFilterReturnsNullFilter()
    {
        var call = new QueryCommandBuilder<TestEntity>(TestDataStore, TestPath, TestContainer)
            .Build();

        ((QueryCommand<TestEntity>)call.Command).Filter.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DataStoreNameIsStoredInTarget()
    {
        var call = new QueryCommandBuilder<TestEntity>(TestDataStore, TestPath, TestContainer)
            .Build();

        call.Target.DataStore.ShouldBe(TestDataStore);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TypeSafeWhereWithOrderByAndPaging()
    {
        var call = new QueryCommandBuilder<TestEntity>(TestDataStore, TestPath, TestContainer)
            .Where(e => e.IsActive).Equal(true)
            .OrderBy(e => e.Name)
            .OrderByDescending(e => e.CreatedDate)
            .Paging(0, 50)
            .Build();

        var query = (QueryCommand<TestEntity>)call.Command;
        query.Filter.ShouldNotBeNull();
        query.Ordering.ShouldNotBeNull();
        query.Ordering!.OrderedFields.Count.ShouldBe(2);
        query.Paging.ShouldNotBeNull();
    }

    /// <summary>FDW-1307 — a condition written after an OR group is ANDed with the group, not added to it as one more alternative.</summary>
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AnOrGroupClosedFirstIsAndedWithAConditionWrittenAfterIt()
    {
        var root = ((QueryCommand<TestEntity>)(new QueryCommandBuilder<TestEntity>(TestDataStore, TestPath, TestContainer)
            .BeginOrGroup().Where("Name", "A").Where("Name", "B").EndGroup()
            .Where("Active", true)
            .Build()).Command).Filter!.Root.ShouldBeOfType<FilterGroup>();

        root.Operator.ShouldBe(LogicalOperator.And);
        root.Nodes.Count.ShouldBe(2);
        var alternatives = root.Nodes[0].ShouldBeOfType<FilterGroup>();
        alternatives.Operator.ShouldBe(LogicalOperator.Or);
        alternatives.Nodes.Count.ShouldBe(2);
        root.Nodes[1].ShouldBeOfType<FilterCondition>().PropertyName.ShouldBe("Active");
    }

    /// <summary>FDW-1307 — two groups written one after the other are ANDed.</summary>
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TwoGroupsWrittenInARowAreAnded()
    {
        var root = ((QueryCommand<TestEntity>)(new QueryCommandBuilder<TestEntity>(TestDataStore, TestPath, TestContainer)
            .BeginOrGroup().Where("Name", "A").Where("Name", "B").EndGroup()
            .BeginOrGroup().Where("Status", "C").Where("Status", "D").EndGroup()
            .Build()).Command).Filter!.Root.ShouldBeOfType<FilterGroup>();

        root.Operator.ShouldBe(LogicalOperator.And);
        root.Nodes.Count.ShouldBe(2);
        root.Nodes[0].ShouldBeOfType<FilterGroup>().Operator.ShouldBe(LogicalOperator.Or);
        root.Nodes[1].ShouldBeOfType<FilterGroup>().Operator.ShouldBe(LogicalOperator.Or);
    }
}
