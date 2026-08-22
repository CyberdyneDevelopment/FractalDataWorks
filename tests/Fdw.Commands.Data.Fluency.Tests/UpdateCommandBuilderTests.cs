using System;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Extensions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Commands.Data.Extensions.Tests;

public sealed class UpdateCommandBuilderTests
{
    private sealed class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BuildUpdateWithEqualityFilter()
    {
        var entity = new TestEntity { Id = 1, Name = "Updated" };

        var call = Update.In<TestEntity>("Customers")
            .DataStore("CRM")
            .Path("sales")
            .Where("Id", 1)
            .Value(entity);

        var cmd = (UpdateCommand<TestEntity>)call.Command;

        call.Target.Container.ShouldBe("Customers");
        call.Target.DataStore.ShouldBe("CRM");
        call.Target.Path.ShouldBe("sales");
        cmd.Data.ShouldBe(entity);
        cmd.Filter.ShouldNotBeNull();
        cmd.Filter!.Root.ShouldBeOfType<FilterCondition>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BuildUpdateWithExplicitOperator()
    {
        var entity = new TestEntity { Id = 1, Name = "Updated" };

        var call = Update.In<TestEntity>("Customers")
            .DataStore("CRM")
            .Path("sales")
            .Where("Status", new NotEqualOperator(), "Active")
            .Value(entity);

        var cmd = (UpdateCommand<TestEntity>)call.Command;

        cmd.Filter.ShouldNotBeNull();
        var condition = cmd.Filter!.Root.ShouldBeOfType<FilterCondition>();
        condition.Operator.ShouldBeOfType<NotEqualOperator>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BuildUpdateWithOrGroup()
    {
        var entity = new TestEntity { Id = 1, Name = "Updated" };

        var call = Update.In<TestEntity>("Customers")
            .DataStore("CRM")
            .Path("sales")
            .BeginOrGroup()
                .Where("Status", "Inactive")
                .Where("Status", "Pending")
            .EndGroup()
            .Value(entity);

        var cmd = (UpdateCommand<TestEntity>)call.Command;

        cmd.Filter.ShouldNotBeNull();
        var group = cmd.Filter!.Root.ShouldBeOfType<FilterGroup>();
        group.Operator.ShouldBe(LogicalOperator.Or);
        group.Nodes.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BuildUpdateWithAndGroup()
    {
        var entity = new TestEntity { Id = 1, Name = "Updated" };

        var call = Update.In<TestEntity>("Customers")
            .DataStore("CRM")
            .Path("sales")
            .BeginAndGroup()
                .Where("Name", "Acme")
                .Where("Status", "Active")
            .EndGroup()
            .Value(entity);

        var cmd = (UpdateCommand<TestEntity>)call.Command;

        cmd.Filter.ShouldNotBeNull();
        var group = cmd.Filter!.Root.ShouldBeOfType<FilterGroup>();
        group.Operator.ShouldBe(LogicalOperator.And);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BuildUpdateWithNestedGroups()
    {
        var entity = new TestEntity { Id = 1, Name = "Updated" };

        var call = Update.In<TestEntity>("Customers")
            .DataStore("CRM")
            .Path("sales")
            .BeginAndGroup()
                .Where("IsActive", true)
                .BeginOrGroup()
                    .Where("Status", "A")
                    .Where("Status", "B")
                .EndGroup()
            .EndGroup()
            .Value(entity);

        var cmd = (UpdateCommand<TestEntity>)call.Command;

        cmd.Filter.ShouldNotBeNull();
        var rootGroup = cmd.Filter!.Root.ShouldBeOfType<FilterGroup>();
        rootGroup.Operator.ShouldBe(LogicalOperator.And);
        rootGroup.Nodes.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BuildUpdateThrowsWhenDataStoreNotSpecified()
    {
        var entity = new TestEntity();
        var builder = Update.In<TestEntity>("Customers")
            .Path("sales")
            .Where("Id", 1);

        Should.Throw<InvalidOperationException>(() => builder.Value(entity));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BuildUpdateThrowsWhenPathNotSpecified()
    {
        var entity = new TestEntity();
        var builder = Update.In<TestEntity>("Customers")
            .DataStore("CRM")
            .Where("Id", 1);

        Should.Throw<InvalidOperationException>(() => builder.Value(entity));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void EndGroupThrowsWhenNoGroupStarted()
    {
        var builder = Update.In<TestEntity>("Customers")
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
            Update.In<TestEntity>("Customers").DataStore(null!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void PathThrowsForNull()
    {
        Should.Throw<ArgumentNullException>(() =>
            Update.In<TestEntity>("Customers").Path(null!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BuildUpdateWithNoFilterHasNullFilter()
    {
        var entity = new TestEntity();

        var call = Update.In<TestEntity>("Customers")
            .DataStore("CRM")
            .Path("sales")
            .Value(entity);

        var cmd = (UpdateCommand<TestEntity>)call.Command;

        cmd.Filter.ShouldBeNull();
    }
}
