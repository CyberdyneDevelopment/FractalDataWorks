using System.Collections.Generic;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.Tests.Expressions;

public sealed class FilterGroupTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AndGroupContainsAllNodes()
    {
        var sut = new FilterGroup
        {
            Operator = LogicalOperator.And,
            Nodes =
            [
                new FilterCondition { PropertyName = "A", Operator = new EqualOperator(), Value = 1 },
                new FilterCondition { PropertyName = "B", Operator = new EqualOperator(), Value = 2 }
            ]
        };

        sut.Operator.ShouldBe(LogicalOperator.And);
        sut.Nodes.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void OrGroupContainsAllNodes()
    {
        var sut = new FilterGroup
        {
            Operator = LogicalOperator.Or,
            Nodes =
            [
                new FilterCondition { PropertyName = "Status", Operator = new EqualOperator(), Value = "Active" },
                new FilterCondition { PropertyName = "Status", Operator = new EqualOperator(), Value = "Pending" }
            ]
        };

        sut.Operator.ShouldBe(LogicalOperator.Or);
        sut.Nodes.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void NestedGroupsAreSupported()
    {
        var innerGroup = new FilterGroup
        {
            Operator = LogicalOperator.Or,
            Nodes =
            [
                new FilterCondition { PropertyName = "Name", Operator = new EqualOperator(), Value = "A" },
                new FilterCondition { PropertyName = "Name", Operator = new EqualOperator(), Value = "B" }
            ]
        };

        var sut = new FilterGroup
        {
            Operator = LogicalOperator.And,
            Nodes =
            [
                innerGroup,
                new FilterCondition { PropertyName = "Active", Operator = new EqualOperator(), Value = true }
            ]
        };

        sut.Nodes.Count.ShouldBe(2);
        sut.Nodes[0].ShouldBeOfType<FilterGroup>();
        sut.Nodes[1].ShouldBeOfType<FilterCondition>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ImplementsIFilterNode()
    {
        var sut = new FilterGroup
        {
            Operator = LogicalOperator.And,
            Nodes = []
        };
        sut.ShouldBeAssignableTo<IFilterNode>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void RecordEqualityWorksForSameContent()
    {
        var nodes = new IFilterNode[]
        {
            new FilterCondition { PropertyName = "A", Operator = new EqualOperator(), Value = 1 }
        };

        var group1 = new FilterGroup { Operator = LogicalOperator.And, Nodes = nodes };
        var group2 = new FilterGroup { Operator = LogicalOperator.And, Nodes = nodes };

        group1.ShouldBe(group2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Regression")]
    public void RecordEqualityAndHashCodeAreValueBasedAcrossDistinctNodeCollectionInstances()
    {
        var group1 = new FilterGroup
        {
            Operator = LogicalOperator.And,
            Nodes = new List<IFilterNode>
            {
                new FilterCondition { PropertyName = "IsCurrent", Operator = new EqualOperator(), Value = true },
                new FilterCondition { PropertyName = "IsDeleted", Operator = new EqualOperator(), Value = false }
            }
        };
        var group2 = new FilterGroup
        {
            Operator = LogicalOperator.And,
            Nodes = new List<IFilterNode>
            {
                new FilterCondition { PropertyName = "IsCurrent", Operator = new EqualOperator(), Value = true },
                new FilterCondition { PropertyName = "IsDeleted", Operator = new EqualOperator(), Value = false }
            }
        };

        group1.ShouldBe(group2);
        group1.GetHashCode().ShouldBe(group2.GetHashCode());
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Regression")]
    public void RecordEqualityDistinguishesDifferentNodeContent()
    {
        var group1 = new FilterGroup
        {
            Operator = LogicalOperator.And,
            Nodes = new List<IFilterNode>
            {
                new FilterCondition { PropertyName = "IsCurrent", Operator = new EqualOperator(), Value = true }
            }
        };
        var group2 = new FilterGroup
        {
            Operator = LogicalOperator.And,
            Nodes = new List<IFilterNode>
            {
                new FilterCondition { PropertyName = "IsCurrent", Operator = new EqualOperator(), Value = false }
            }
        };

        group1.ShouldNotBe(group2);
    }
}
