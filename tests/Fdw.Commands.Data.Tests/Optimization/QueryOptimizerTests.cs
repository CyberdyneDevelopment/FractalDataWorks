using System.Collections.Generic;
using Fdw.Commands.Data;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Commands.Data.Tests.Optimization;

public sealed class QueryOptimizerTests
{
    private readonly QueryOptimizer _sut = new();

    private static Mock<IDataSource> CreateSource(string name, bool hasFilter = false)
    {
        var source = new Mock<IDataSource>();
        source.Setup(s => s.ContainerName).Returns(name);
        source.Setup(s => s.Filter).Returns(
            hasFilter ? new FilterExpression { Root = new FilterCondition { PropertyName = "Id", Operator = new EqualOperator(), Value = 1 } } : null);
        return source;
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void OptimizeSourceOrderReturnsEmptyForNull()
    {
        _sut.OptimizeSourceOrder(null!).ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void OptimizeSourceOrderReturnsSingleSource()
    {
        var source = CreateSource("A");
        var result = _sut.OptimizeSourceOrder([source.Object]);
        result.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void OptimizeSourceOrderPutsFilteredSourcesFirst()
    {
        var unfiltered = CreateSource("Unfiltered", hasFilter: false);
        var filtered = CreateSource("Filtered", hasFilter: true);

        var result = _sut.OptimizeSourceOrder([unfiltered.Object, filtered.Object]);

        result.Count.ShouldBe(2);
        result[0].ContainerName.ShouldBe("Filtered");
        result[1].ContainerName.ShouldBe("Unfiltered");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void OptimizeSourceOrderPreservesOrderWhenAllFiltered()
    {
        var a = CreateSource("A", hasFilter: true);
        var b = CreateSource("B", hasFilter: true);

        var result = _sut.OptimizeSourceOrder([a.Object, b.Object]);

        result.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void PushDownPredicatesReturnsSourcesUnchanged()
    {
        var source = CreateSource("A");
        var filter = new FilterExpression
        {
            Root = new FilterCondition { PropertyName = "Id", Operator = new EqualOperator(), Value = 1 }
        };

        var result = _sut.PushDownPredicates([source.Object], filter);

        result.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void EstimateCardinalityReturnsNull()
    {
        var source = CreateSource("A");
        _sut.EstimateCardinality(source.Object).ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SelectJoinAlgorithmReturnsHashWhenCardinalitiesUnknown()
    {
        _sut.SelectJoinAlgorithm(null, null).ShouldBe("Hash");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SelectJoinAlgorithmReturnsHashWhenLeftUnknown()
    {
        _sut.SelectJoinAlgorithm(null, 50).ShouldBe("Hash");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SelectJoinAlgorithmReturnsHashWhenRightUnknown()
    {
        _sut.SelectJoinAlgorithm(50, null).ShouldBe("Hash");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SelectJoinAlgorithmReturnsNestedLoopForSmallSources()
    {
        _sut.SelectJoinAlgorithm(50, 50).ShouldBe("NestedLoop");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SelectJoinAlgorithmReturnsHashForLargeSources()
    {
        _sut.SelectJoinAlgorithm(1000, 500).ShouldBe("Hash");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SelectJoinAlgorithmReturnsHashWhenOneSourceIsLarge()
    {
        _sut.SelectJoinAlgorithm(50, 200).ShouldBe("Hash");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SelectJoinAlgorithmBoundaryBothAt99ReturnsNestedLoop()
    {
        _sut.SelectJoinAlgorithm(99, 99).ShouldBe("NestedLoop");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SelectJoinAlgorithmBoundaryOneAt100ReturnsHash()
    {
        _sut.SelectJoinAlgorithm(100, 50).ShouldBe("Hash");
    }
}
