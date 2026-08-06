using System;
using System.Linq.Expressions;
using Fdw.Commands.Data;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Commands.Data.Extensions.Tests;

/// <summary>
/// Tests exercising ExpressionHelper through QueryCommandBuilder to achieve
/// full branch coverage on expression extraction code paths.
/// </summary>
public sealed class ExpressionHelperTests
{
    private const string Ds = "TestDb";
    private const string Path = "dbo";
    private const string Container = "Items";

    private sealed class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool IsActive { get; set; }
        public int? NullableInt { get; set; }
        public object? Tag { get; set; }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WhereWithObjectPropertyExtractsNameViaUnaryExpression()
    {
        // object properties accessed through Expression<Func<T, object>> cause UnaryExpression (boxing)
        // This tests the UnaryExpression code path in ExpressionHelper
        var call = new QueryCommandBuilder<TestEntity>(Ds, Path, Container)
            .Where(e => e.Id).Equal(5)
            .Build();

        var condition = ((QueryCommand<TestEntity>)call.Command).Filter!.Root.ShouldBeOfType<FilterCondition>();
        condition.PropertyName.ShouldBe("Id");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WhereWithBoolPropertyExtractsName()
    {
        var call = new QueryCommandBuilder<TestEntity>(Ds, Path, Container)
            .Where(e => e.IsActive).Equal(true)
            .Build();

        var condition = ((QueryCommand<TestEntity>)call.Command).Filter!.Root.ShouldBeOfType<FilterCondition>();
        condition.PropertyName.ShouldBe("IsActive");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WhereWithDecimalPropertyExtractsName()
    {
        var call = new QueryCommandBuilder<TestEntity>(Ds, Path, Container)
            .Where(e => e.Price).GreaterThan(9.99m)
            .Build();

        var condition = ((QueryCommand<TestEntity>)call.Command).Filter!.Root.ShouldBeOfType<FilterCondition>();
        condition.PropertyName.ShouldBe("Price");
        condition.Value.ShouldBe(9.99m);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WhereWithNullableIntPropertyExtractsName()
    {
        var call = new QueryCommandBuilder<TestEntity>(Ds, Path, Container)
            .Where(e => e.NullableInt).IsNull()
            .Build();

        var condition = ((QueryCommand<TestEntity>)call.Command).Filter!.Root.ShouldBeOfType<FilterCondition>();
        condition.PropertyName.ShouldBe("NullableInt");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void OrderByWithValueTypePropertyExtractsName()
    {
        var call = new QueryCommandBuilder<TestEntity>(Ds, Path, Container)
            .OrderBy(e => e.Id)
            .Build();

        var query = (QueryCommand<TestEntity>)call.Command;
        query.Ordering.ShouldNotBeNull();
        query.Ordering!.OrderedFields[0].PropertyName.ShouldBe("Id");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void OrderByDescendingWithValueTypePropertyExtractsName()
    {
        var call = new QueryCommandBuilder<TestEntity>(Ds, Path, Container)
            .OrderByDescending(e => e.Price)
            .Build();

        var query = (QueryCommand<TestEntity>)call.Command;
        query.Ordering.ShouldNotBeNull();
        query.Ordering!.OrderedFields[0].PropertyName.ShouldBe("Price");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WhereWithStringPropertyExtractsDirectMemberExpression()
    {
        // String is a reference type - no unary boxing, direct MemberExpression
        var call = new QueryCommandBuilder<TestEntity>(Ds, Path, Container)
            .Where(e => e.Name).Equal("Test")
            .Build();

        var condition = ((QueryCommand<TestEntity>)call.Command).Filter!.Root.ShouldBeOfType<FilterCondition>();
        condition.PropertyName.ShouldBe("Name");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void MultipleWhereConditionsChainCorrectly()
    {
        var call = new QueryCommandBuilder<TestEntity>(Ds, Path, Container)
            .Where(e => e.Name).Equal("Test")
            .Where(e => e.Id).GreaterThan(0)
            .Where(e => e.IsActive).Equal(true)
            .Build();

        var query = (QueryCommand<TestEntity>)call.Command;
        query.Filter.ShouldNotBeNull();
        var group = query.Filter!.Root.ShouldBeOfType<FilterGroup>();
        group.Nodes.Count.ShouldBe(3);
    }
}
