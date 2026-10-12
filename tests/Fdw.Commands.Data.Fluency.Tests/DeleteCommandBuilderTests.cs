using System;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Extensions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Commands.Data.Extensions.Tests;

public sealed class DeleteCommandBuilderTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BuildSimpleDeleteWithEqualityFilter()
    {
        var call = Delete.From("Customers")
            .DataStore("CRM")
            .Path("sales")
            .Where("Id", 42)
            .Build();

        var cmd = (DeleteCommand)call.Command;

        call.Target.Container.ShouldBe("Customers");
        call.Target.DataStore.ShouldBe("CRM");
        call.Target.Path.ShouldBe("sales");
        cmd.Filter.ShouldNotBeNull();
        cmd.Filter!.Root.ShouldBeOfType<FilterCondition>();
        var condition = (FilterCondition)cmd.Filter.Root;
        condition.PropertyName.ShouldBe("Id");
        condition.Value.ShouldBe(42);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BuildDeleteWithExplicitOperator()
    {
        var call = Delete.From("Orders")
            .DataStore("CRM")
            .Path("sales")
            .Where("Status", new NotEqualOperator(), "Active")
            .Build();

        var cmd = (DeleteCommand)call.Command;

        cmd.Filter.ShouldNotBeNull();
        var condition = cmd.Filter!.Root.ShouldBeOfType<FilterCondition>();
        condition.Operator.ShouldBeOfType<NotEqualOperator>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BuildDeleteWithMultipleConditionsCreatesAndGroup()
    {
        var call = Delete.From("Customers")
            .DataStore("CRM")
            .Path("sales")
            .Where("Status", "Inactive")
            .Where("IsDeleted", true)
            .Build();

        var cmd = (DeleteCommand)call.Command;

        cmd.Filter.ShouldNotBeNull();
        cmd.Filter!.Root.ShouldBeOfType<FilterGroup>();
        var group = (FilterGroup)cmd.Filter.Root;
        group.Operator.ShouldBe(LogicalOperator.And);
        group.Nodes.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BuildDeleteWithOrGroup()
    {
        var call = Delete.From("Customers")
            .DataStore("CRM")
            .Path("sales")
            .BeginOrGroup()
                .Where("Status", "Inactive")
                .Where("Status", "Pending")
            .EndGroup()
            .Build();

        var cmd = (DeleteCommand)call.Command;

        cmd.Filter.ShouldNotBeNull();
        cmd.Filter!.Root.ShouldBeOfType<FilterGroup>();
        var group = (FilterGroup)cmd.Filter.Root;
        group.Operator.ShouldBe(LogicalOperator.Or);
        group.Nodes.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BuildDeleteWithNestedGroups()
    {
        var call = Delete.From("Customers")
            .DataStore("CRM")
            .Path("sales")
            .BeginAndGroup()
                .Where("IsDeleted", true)
                .BeginOrGroup()
                    .Where("Status", "Inactive")
                    .Where("Status", "Expired")
                .EndGroup()
            .EndGroup()
            .Build();

        var cmd = (DeleteCommand)call.Command;

        cmd.Filter.ShouldNotBeNull();
        var rootGroup = cmd.Filter!.Root.ShouldBeOfType<FilterGroup>();
        rootGroup.Operator.ShouldBe(LogicalOperator.And);
        rootGroup.Nodes.Count.ShouldBe(2);
        rootGroup.Nodes[0].ShouldBeOfType<FilterCondition>();
        rootGroup.Nodes[1].ShouldBeOfType<FilterGroup>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BuildDeleteThrowsWhenDataStoreNotSpecified()
    {
        var builder = Delete.From("Customers")
            .Path("sales")
            .Where("Id", 1);

        Should.Throw<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BuildDeleteThrowsWhenPathNotSpecified()
    {
        var builder = Delete.From("Customers")
            .DataStore("CRM")
            .Where("Id", 1);

        Should.Throw<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void EndGroupThrowsWhenNoGroupStarted()
    {
        var builder = Delete.From("Customers")
            .DataStore("CRM")
            .Path("sales");

        Should.Throw<InvalidOperationException>(() => builder.EndGroup());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DataStoreThrowsForNull()
    {
        Should.Throw<ArgumentNullException>(() =>
            Delete.From("Customers").DataStore(null!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void PathThrowsForNull()
    {
        Should.Throw<ArgumentNullException>(() =>
            Delete.From("Customers").Path(null!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BuildDeleteWithNoFilterHasNullFilter()
    {
        var call = Delete.From("Customers")
            .DataStore("CRM")
            .Path("sales")
            .Build();

        var cmd = (DeleteCommand)call.Command;

        cmd.Filter.ShouldBeNull();
    }
}
