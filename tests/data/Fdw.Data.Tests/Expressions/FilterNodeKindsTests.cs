using System;
using System.Collections.Generic;
using Fdw.Data.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.Tests.Expressions;

/// <summary>The kind a filter node is, found for the translators that dispatch on it.</summary>
public sealed class FilterNodeKindsTests
{
    private sealed class OtherNode : IFilterNode
    {
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AConditionIsTheConditionKind()
    {
        var condition = new FilterCondition { PropertyName = "Id", Operator = new EqualOperator(), Value = 1 };

        FilterNodeKinds.Of(condition).Name.ShouldBe("Condition");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AGroupIsTheGroupKind()
    {
        var group = new FilterGroup { Operator = LogicalOperator.And, Nodes = new List<IFilterNode>() };

        FilterNodeKinds.Of(group).Name.ShouldBe("Group");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ANodeOfAnyOtherTypeHasNoKind() =>
        ReferenceEquals(FilterNodeKinds.Of(new OtherNode()), FilterNodeKinds.NotFound).ShouldBeTrue();

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AWalkOverANodeOfAnyOtherTypeIsRefused() =>
        Should.Throw<InvalidOperationException>(() => FilterNodeKinds.Conditions(new OtherNode()));
}
