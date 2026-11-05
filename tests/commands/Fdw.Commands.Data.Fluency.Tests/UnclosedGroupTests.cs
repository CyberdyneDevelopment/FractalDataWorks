using System;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Extensions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Commands.Data.Extensions.Tests;

/// <summary>
/// FDW-3598: a condition written inside a group reaches the filter only when EndGroup() closes the group. Building with a group still open
/// dropped its conditions, so a Delete went out with no filter and emptied the container, an Update changed every row and a Query returned
/// every row. The three command builders now refuse to build while a group is open, naming how many are open.
/// </summary>
public sealed class UnclosedGroupTests
{
    private sealed class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private static QueryCommandBuilder<TestEntity> QueryUsers() => new("TestDb", "dbo", "Users");

    private static DeleteCommandBuilder DeleteCustomers() => Delete.From("Customers").DataStore("CRM").Path("sales");

    private static UpdateCommandBuilder<TestEntity> UpdateCustomers() => Update.In<TestEntity>("Customers").DataStore("CRM").Path("sales");

    // ---------------------------------------------------------------- Delete

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ADeleteWithAnUnclosedGroupIsRefused()
    {
        var builder = DeleteCustomers().BeginAndGroup().Where("Id", 42);

        Should.Throw<InvalidOperationException>(() => builder.Build()).Message.ShouldStartWith("1 filter group(s) still open.");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ADeleteWithTwoUnclosedGroupsNamesBoth()
    {
        var builder = DeleteCustomers().BeginAndGroup().Where("Id", 42).BeginOrGroup().Where("Name", "A").Where("Name", "B");

        Should.Throw<InvalidOperationException>(() => builder.Build()).Message.ShouldStartWith("2 filter group(s) still open.");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ADeleteWithItsGroupClosedBuildsTheGroupAsItsFilter()
    {
        var command = (DeleteCommand)DeleteCustomers().BeginOrGroup().Where("Name", "A").Where("Name", "B").EndGroup().Build().Command;

        var group = command.Filter!.Root.ShouldBeOfType<FilterGroup>();
        group.Operator.ShouldBe(LogicalOperator.Or);
        group.Nodes.Count.ShouldBe(2);
    }

    // ---------------------------------------------------------------- Query

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AQueryWithAnUnclosedGroupIsRefused()
    {
        var builder = QueryUsers().BeginOrGroup().Where("Name", "A");

        Should.Throw<InvalidOperationException>(() => builder.Build()).Message.ShouldStartWith("1 filter group(s) still open.");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AQueryWithOnlyTheInnerOfTwoGroupsClosedIsRefused()
    {
        var builder = QueryUsers().BeginAndGroup().Where("Id", 1).BeginOrGroup().Where("Name", "A").Where("Name", "B").EndGroup();

        Should.Throw<InvalidOperationException>(() => builder.Build()).Message.ShouldStartWith("1 filter group(s) still open.");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AQueryWithItsGroupsClosedBuildsTheNestedFilter()
    {
        var query = (QueryCommand<TestEntity>)QueryUsers()
            .BeginAndGroup()
                .Where("Id", 1)
                .BeginOrGroup()
                    .Where("Name", "A")
                    .Where("Name", "B")
                .EndGroup()
            .EndGroup()
            .Build().Command;

        var group = query.Filter!.Root.ShouldBeOfType<FilterGroup>();
        group.Operator.ShouldBe(LogicalOperator.And);
        group.Nodes.Count.ShouldBe(2);
        group.Nodes[1].ShouldBeOfType<FilterGroup>().Operator.ShouldBe(LogicalOperator.Or);
    }

    // ---------------------------------------------------------------- Update

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AnUpdateWithAnUnclosedGroupIsRefused()
    {
        var builder = UpdateCustomers().BeginAndGroup().Where("Id", 1);

        Should.Throw<InvalidOperationException>(() => builder.Value(new TestEntity { Id = 1 })).Message.ShouldStartWith("1 filter group(s) still open.");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AnUpdateWithItsGroupClosedBuildsTheGroupAsItsFilter()
    {
        var command = (UpdateCommand<TestEntity>)UpdateCustomers().BeginOrGroup().Where("Name", "A").Where("Name", "B").EndGroup().Value(new TestEntity { Id = 1 }).Command;

        var group = command.Filter!.Root.ShouldBeOfType<FilterGroup>();
        group.Operator.ShouldBe(LogicalOperator.Or);
        group.Nodes.Count.ShouldBe(2);
    }
}
