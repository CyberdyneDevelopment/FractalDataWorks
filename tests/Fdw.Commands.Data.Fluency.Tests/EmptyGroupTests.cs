using Fdw.Commands.Data;
using Fdw.Commands.Data.Extensions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Commands.Data.Extensions.Tests;

/// <summary>
/// FDW-2808: a group that closes holding no conditions is no condition. Adding it as a node left an empty group at the root, which renders as
/// nothing and was still written as a WHERE with nothing after it. The three command builders share the rule.
/// </summary>
public sealed class EmptyGroupTests
{
    private sealed class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private static QueryCommandBuilder<TestEntity> Query() => new("TestDb", "dbo", "Users");

    private static DeleteCommand BuiltDelete(System.Func<DeleteCommandBuilder, DeleteCommandBuilder> shape)
        => (DeleteCommand)shape(Delete.From("Customers").DataStore("CRM").Path("sales")).Build().Command;

    private static UpdateCommand<TestEntity> BuiltUpdate(System.Func<UpdateCommandBuilder<TestEntity>, UpdateCommandBuilder<TestEntity>> shape)
        => (UpdateCommand<TestEntity>)shape(Update.In<TestEntity>("Customers").DataStore("CRM").Path("sales")).Value(new TestEntity { Id = 1 }).Command;

    // ---------------------------------------------------------------- Query

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AQueryWithAnEmptyTopLevelAndGroupHasNoFilter()
        => ((QueryCommand<TestEntity>)Query().BeginAndGroup().EndGroup().Build().Command).Filter.ShouldBeNull();

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AQueryWithAnEmptyTopLevelOrGroupHasNoFilter()
        => ((QueryCommand<TestEntity>)Query().BeginOrGroup().EndGroup().Build().Command).Filter.ShouldBeNull();

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AQueryNestedEmptyGroupAddsNothingToItsParent()
    {
        var query = (QueryCommand<TestEntity>)Query()
            .BeginAndGroup()
                .Where("Name", "A")
                .BeginOrGroup().EndGroup()
            .EndGroup()
            .Build().Command;

        var group = query.Filter!.Root.ShouldBeOfType<FilterGroup>();
        group.Nodes.ShouldHaveSingleItem().ShouldBeOfType<FilterCondition>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AQueryGroupHoldingOnlyEmptyGroupsIsDroppedToo()
        => ((QueryCommand<TestEntity>)Query()
            .BeginAndGroup().BeginOrGroup().EndGroup().BeginAndGroup().EndGroup().EndGroup()
            .Build().Command).Filter.ShouldBeNull();

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AQueryEmptyGroupDoesNotDisturbAConditionWrittenAroundIt()
    {
        var query = (QueryCommand<TestEntity>)Query()
            .Where("Name", "A")
            .BeginAndGroup().EndGroup()
            .Where("Id", 1)
            .Build().Command;

        query.Filter!.Root.ShouldBeOfType<FilterGroup>().Nodes.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AQueryGroupWithAConditionIsStillAddedAndAConditionAfterAnOrGroupIsAndedWithIt()
    {
        var root = ((QueryCommand<TestEntity>)Query()
            .BeginOrGroup().Where("Name", "A").Where("Name", "B").EndGroup()
            .Where("Id", 1)
            .Build().Command).Filter!.Root.ShouldBeOfType<FilterGroup>();

        root.Operator.ShouldBe(LogicalOperator.And);
        root.Nodes.Count.ShouldBe(2);
    }

    // ---------------------------------------------------------------- Delete

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ADeleteWithAnEmptyTopLevelGroupCarriesNoFilter()
        => BuiltDelete(b => b.BeginAndGroup().EndGroup()).Filter.ShouldBeNull();

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ADeleteNestedEmptyGroupAddsNothingToItsParent()
    {
        var cmd = BuiltDelete(b => b.BeginAndGroup().Where("Id", 1).BeginOrGroup().EndGroup().EndGroup());

        cmd.Filter!.Root.ShouldBeOfType<FilterGroup>().Nodes.ShouldHaveSingleItem().ShouldBeOfType<FilterCondition>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ADeleteWithAnEmptyOrGroupAndAConditionKeepsTheCondition()
    {
        var cmd = BuiltDelete(b => b.BeginOrGroup().EndGroup().Where("Id", 1));

        cmd.Filter!.Root.ShouldBeOfType<FilterCondition>();
    }

    // ---------------------------------------------------------------- Update

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AnUpdateWithAnEmptyTopLevelGroupCarriesNoFilter()
        => BuiltUpdate(b => b.BeginAndGroup().EndGroup()).Filter.ShouldBeNull();

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AnUpdateNestedEmptyGroupAddsNothingToItsParent()
    {
        var cmd = BuiltUpdate(b => b.BeginAndGroup().Where("Id", 1).BeginOrGroup().EndGroup().EndGroup());

        cmd.Filter!.Root.ShouldBeOfType<FilterGroup>().Nodes.ShouldHaveSingleItem().ShouldBeOfType<FilterCondition>();
    }
}
