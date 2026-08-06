using Fdw.Data;
using Fdw.Data.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.Tests.Expressions;

public sealed class AggregationExpressionTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GroupByFieldsArePreserved()
    {
        var sut = new AggregationExpression
        {
            GroupByFields = ["Category", "Region"],
            Aggregations = new Dictionary<string, string>
            {
                ["TotalAmount"] = "SUM(Amount)",
                ["OrderCount"] = "COUNT(*)"
            }
        };

        sut.GroupByFields.Count.ShouldBe(2);
        sut.GroupByFields[0].ShouldBe("Category");
        sut.GroupByFields[1].ShouldBe("Region");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AggregationsArePreserved()
    {
        var sut = new AggregationExpression
        {
            GroupByFields = ["Status"],
            Aggregations = new Dictionary<string, string>
            {
                ["Avg"] = "AVG(Score)"
            }
        };

        sut.Aggregations.Count.ShouldBe(1);
        sut.Aggregations["Avg"].ShouldBe("AVG(Score)");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ImplementsIAggregationExpression()
    {
        var sut = new AggregationExpression
        {
            GroupByFields = [],
            Aggregations = new Dictionary<string, string>()
        };
        sut.ShouldBeAssignableTo<IAggregationExpression>();
    }
}
