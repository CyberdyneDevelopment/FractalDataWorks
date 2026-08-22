using System.Collections.Generic;
using Fdw.Commands.Data;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Commands.Data.Extensions.Tests;

public sealed class FilterConditionBuilderTests
{
    private sealed class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int? Age { get; set; }
    }

    private const string Ds = "TestDb";
    private const string Path = "dbo";
    private const string Container = "Users";

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void EqualCreatesEqualOperatorCondition()
    {
        var call = new QueryCommandBuilder<TestEntity>(Ds, Path, Container)
            .Where(e => e.Name).Equal("John")
            .Build();

        var condition = ((QueryCommand<TestEntity>)call.Command).Filter!.Root.ShouldBeOfType<FilterCondition>();
        condition.PropertyName.ShouldBe("Name");
        condition.Operator.ShouldBeOfType<EqualOperator>();
        condition.Value.ShouldBe("John");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void NotEqualCreatesNotEqualOperatorCondition()
    {
        var call = new QueryCommandBuilder<TestEntity>(Ds, Path, Container)
            .Where(e => e.Status).NotEqual("Deleted")
            .Build();

        var condition = ((QueryCommand<TestEntity>)call.Command).Filter!.Root.ShouldBeOfType<FilterCondition>();
        condition.Operator.ShouldBeOfType<NotEqualOperator>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GreaterThanCreatesGreaterThanOperatorCondition()
    {
        var call = new QueryCommandBuilder<TestEntity>(Ds, Path, Container)
            .Where(e => e.Id).GreaterThan(10)
            .Build();

        var condition = ((QueryCommand<TestEntity>)call.Command).Filter!.Root.ShouldBeOfType<FilterCondition>();
        condition.Operator.ShouldBeOfType<GreaterThanOperator>();
        condition.Value.ShouldBe(10);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GreaterThanOrEqualCreatesCorrectOperator()
    {
        var call = new QueryCommandBuilder<TestEntity>(Ds, Path, Container)
            .Where(e => e.Id).GreaterThanOrEqual(5)
            .Build();

        var condition = ((QueryCommand<TestEntity>)call.Command).Filter!.Root.ShouldBeOfType<FilterCondition>();
        condition.Operator.ShouldBeOfType<GreaterThanOrEqualOperator>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void LessThanCreatesLessThanOperatorCondition()
    {
        var call = new QueryCommandBuilder<TestEntity>(Ds, Path, Container)
            .Where(e => e.Id).LessThan(100)
            .Build();

        var condition = ((QueryCommand<TestEntity>)call.Command).Filter!.Root.ShouldBeOfType<FilterCondition>();
        condition.Operator.ShouldBeOfType<LessThanOperator>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void LessThanOrEqualCreatesCorrectOperator()
    {
        var call = new QueryCommandBuilder<TestEntity>(Ds, Path, Container)
            .Where(e => e.Id).LessThanOrEqual(50)
            .Build();

        var condition = ((QueryCommand<TestEntity>)call.Command).Filter!.Root.ShouldBeOfType<FilterCondition>();
        condition.Operator.ShouldBeOfType<LessThanOrEqualOperator>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ContainsCreatesContainsOperatorCondition()
    {
        var call = new QueryCommandBuilder<TestEntity>(Ds, Path, Container)
            .Where(e => e.Name).Contains("Corp")
            .Build();

        var condition = ((QueryCommand<TestEntity>)call.Command).Filter!.Root.ShouldBeOfType<FilterCondition>();
        condition.Operator.ShouldBeOfType<ContainsOperator>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void StartsWithCreatesStartsWithOperatorCondition()
    {
        var call = new QueryCommandBuilder<TestEntity>(Ds, Path, Container)
            .Where(e => e.Name).StartsWith("Acm")
            .Build();

        var condition = ((QueryCommand<TestEntity>)call.Command).Filter!.Root.ShouldBeOfType<FilterCondition>();
        condition.Operator.ShouldBeOfType<StartsWithOperator>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void EndsWithCreatesEndsWithOperatorCondition()
    {
        var call = new QueryCommandBuilder<TestEntity>(Ds, Path, Container)
            .Where(e => e.Name).EndsWith("Inc")
            .Build();

        var condition = ((QueryCommand<TestEntity>)call.Command).Filter!.Root.ShouldBeOfType<FilterCondition>();
        condition.Operator.ShouldBeOfType<EndsWithOperator>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void InWithEnumerableCreatesInOperatorCondition()
    {
        var values = new List<string> { "Active", "Pending" };
        var call = new QueryCommandBuilder<TestEntity>(Ds, Path, Container)
            .Where(e => e.Status).In(values)
            .Build();

        var condition = ((QueryCommand<TestEntity>)call.Command).Filter!.Root.ShouldBeOfType<FilterCondition>();
        condition.Operator.ShouldBeOfType<InOperator>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void InWithParamsCreatesInOperatorCondition()
    {
        var call = new QueryCommandBuilder<TestEntity>(Ds, Path, Container)
            .Where(e => e.Status).In("Active", "Pending", "Review")
            .Build();

        var condition = ((QueryCommand<TestEntity>)call.Command).Filter!.Root.ShouldBeOfType<FilterCondition>();
        condition.Operator.ShouldBeOfType<InOperator>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IsNullCreatesIsNullOperatorCondition()
    {
        var call = new QueryCommandBuilder<TestEntity>(Ds, Path, Container)
            .Where(e => e.Age).IsNull()
            .Build();

        var condition = ((QueryCommand<TestEntity>)call.Command).Filter!.Root.ShouldBeOfType<FilterCondition>();
        condition.Operator.ShouldBeOfType<IsNullOperator>();
        condition.Value.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IsNotNullCreatesIsNotNullOperatorCondition()
    {
        var call = new QueryCommandBuilder<TestEntity>(Ds, Path, Container)
            .Where(e => e.Age).IsNotNull()
            .Build();

        var condition = ((QueryCommand<TestEntity>)call.Command).Filter!.Root.ShouldBeOfType<FilterCondition>();
        condition.Operator.ShouldBeOfType<IsNotNullOperator>();
        condition.Value.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TypeSafeWhereExtractsPropertyNameCorrectly()
    {
        var call = new QueryCommandBuilder<TestEntity>(Ds, Path, Container)
            .Where(e => e.Name).Equal("test")
            .Build();

        var condition = ((QueryCommand<TestEntity>)call.Command).Filter!.Root.ShouldBeOfType<FilterCondition>();
        condition.PropertyName.ShouldBe("Name");
    }
}
