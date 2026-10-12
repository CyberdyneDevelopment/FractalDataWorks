using Fdw.Commands.Data;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Commands.Data.Extensions.Tests;

public sealed class FilterOperatorExtensionsTests
{
    private sealed class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private const string Ds = "TestDb";
    private const string Path = "dbo";
    private const string Container = "Users";

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WhereEqualAddsEqualCondition()
    {
        var call = new QueryCommandBuilder<TestEntity>(Ds, Path, Container)
            .WhereEqual("Name", "John")
            .Build();

        var condition = ((QueryCommand<TestEntity>)call.Command).Filter!.Root.ShouldBeOfType<FilterCondition>();
        condition.Operator.ShouldBeOfType<EqualOperator>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WhereNotEqualAddsNotEqualCondition()
    {
        var call = new QueryCommandBuilder<TestEntity>(Ds, Path, Container)
            .WhereNotEqual("Status", "Deleted")
            .Build();

        var condition = ((QueryCommand<TestEntity>)call.Command).Filter!.Root.ShouldBeOfType<FilterCondition>();
        condition.Operator.ShouldBeOfType<NotEqualOperator>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WhereGreaterThanAddsGreaterThanCondition()
    {
        var call = new QueryCommandBuilder<TestEntity>(Ds, Path, Container)
            .WhereGreaterThan("Id", 10)
            .Build();

        var condition = ((QueryCommand<TestEntity>)call.Command).Filter!.Root.ShouldBeOfType<FilterCondition>();
        condition.Operator.ShouldBeOfType<GreaterThanOperator>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WhereGreaterThanOrEqualAddsCorrectCondition()
    {
        var call = new QueryCommandBuilder<TestEntity>(Ds, Path, Container)
            .WhereGreaterThanOrEqual("Id", 5)
            .Build();

        var condition = ((QueryCommand<TestEntity>)call.Command).Filter!.Root.ShouldBeOfType<FilterCondition>();
        condition.Operator.ShouldBeOfType<GreaterThanOrEqualOperator>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WhereLessThanAddsLessThanCondition()
    {
        var call = new QueryCommandBuilder<TestEntity>(Ds, Path, Container)
            .WhereLessThan("Id", 100)
            .Build();

        var condition = ((QueryCommand<TestEntity>)call.Command).Filter!.Root.ShouldBeOfType<FilterCondition>();
        condition.Operator.ShouldBeOfType<LessThanOperator>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WhereLessThanOrEqualAddsCorrectCondition()
    {
        var call = new QueryCommandBuilder<TestEntity>(Ds, Path, Container)
            .WhereLessThanOrEqual("Id", 50)
            .Build();

        var condition = ((QueryCommand<TestEntity>)call.Command).Filter!.Root.ShouldBeOfType<FilterCondition>();
        condition.Operator.ShouldBeOfType<LessThanOrEqualOperator>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WhereContainsAddsContainsCondition()
    {
        var call = new QueryCommandBuilder<TestEntity>(Ds, Path, Container)
            .WhereContains("Name", "Corp")
            .Build();

        var condition = ((QueryCommand<TestEntity>)call.Command).Filter!.Root.ShouldBeOfType<FilterCondition>();
        condition.Operator.ShouldBeOfType<ContainsOperator>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WhereStartsWithAddsStartsWithCondition()
    {
        var call = new QueryCommandBuilder<TestEntity>(Ds, Path, Container)
            .WhereStartsWith("Name", "Acm")
            .Build();

        var condition = ((QueryCommand<TestEntity>)call.Command).Filter!.Root.ShouldBeOfType<FilterCondition>();
        condition.Operator.ShouldBeOfType<StartsWithOperator>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WhereEndsWithAddsEndsWithCondition()
    {
        var call = new QueryCommandBuilder<TestEntity>(Ds, Path, Container)
            .WhereEndsWith("Name", "Inc")
            .Build();

        var condition = ((QueryCommand<TestEntity>)call.Command).Filter!.Root.ShouldBeOfType<FilterCondition>();
        condition.Operator.ShouldBeOfType<EndsWithOperator>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WhereInAddsInCondition()
    {
        var call = new QueryCommandBuilder<TestEntity>(Ds, Path, Container)
            .WhereIn("Status", new[] { "Active", "Pending" })
            .Build();

        var condition = ((QueryCommand<TestEntity>)call.Command).Filter!.Root.ShouldBeOfType<FilterCondition>();
        condition.Operator.ShouldBeOfType<InOperator>();
    }
}
