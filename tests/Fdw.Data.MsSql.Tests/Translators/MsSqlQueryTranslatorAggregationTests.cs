using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.MsSql;
using Fdw.Results;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Data.MsSql.Tests.Translators;

/// <summary>
/// The grouped read: group-by columns and aggregates, the filter before the grouping, an order by a group
/// column or an aggregate's output column, a page, and the count of groups (FDW-1023).
/// </summary>
[Collection(nameof(DataMsSqlTestCollection))]
public sealed class MsSqlQueryTranslatorAggregationTests
{
    private readonly MsSqlQueryTranslator _sut = new();

    private static Mock<IDataContainer> CreateContainer()
    {
        var fields = new[] { CreateField("grade"), CreateField("absences") };
        var containerSchema = new Mock<IContainerSchema>();
        containerSchema.Setup(s => s.Fields).Returns(fields);
        containerSchema.Setup(s => s.GetReturnedFields(It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>())).Returns(fields);

        var container = new Mock<IDataContainer>();
        container.Setup(c => c.Name).Returns("Attendance");
        container.As<IStorageContainer>().Setup(c => c.Path).Returns(new DatabasePath(string.Empty, "dbo", "Attendance"));
        container.Setup(c => c.Schema).Returns(containerSchema.Object);
        container.Setup(c => c.ReferencingKeys)
            .Returns(GenericResult<IReadOnlyList<ReferencingKeyBinding>>.Success([]));
        container.Setup(c => c.Keys).Returns(new List<IContainerKey>());
        return container;
    }

    private static IField CreateField(string name)
    {
        var field = new Mock<IField>();
        field.Setup(f => f.Name).Returns(name);
        field.Setup(f => f.IsIdentity).Returns(false);
        field.Setup(f => f.IsComputed).Returns(false);
        return field.Object;
    }

    private static AggregationExpression SumByGrade(bool includeGroupCount = false) => new()
    {
        GroupByFields = ["grade"],
        Aggregations = new Dictionary<string, string> { ["absences"] = "SUM(absences)" },
        IncludeGroupCount = includeGroupCount,
    };

    private static OrderingExpression OrderedBy(string name, bool descending)
    {
        var ordered = new Mock<IOrderedField>();
        ordered.Setup(f => f.PropertyName).Returns(name);
        ordered.Setup(f => f.Direction).Returns(descending ? new DescendingDirection() : new AscendingDirection());
        return new OrderingExpression { OrderedFields = [ordered.Object] };
    }

    private async Task<string> Translate(
        IAggregationExpression aggregation,
        IFilterExpression? filter = null,
        IOrderingExpression? ordering = null,
        IPagingExpression? paging = null)
    {
        var query = new Mock<IQueryCommand>();
        query.Setup(q => q.Joins).Returns([]);
        query.Setup(q => q.Filter).Returns(filter);
        query.Setup(q => q.Projection).Returns((IProjectionExpression?)null);
        query.Setup(q => q.Ordering).Returns(ordering);
        query.Setup(q => q.Paging).Returns(paging);
        query.Setup(q => q.Aggregation).Returns(aggregation);

        var result = await _sut.Translate(query.Object, CreateContainer().Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        return result.Value!.CommandText;
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task GroupsByTheGroupColumnsAndSumsTheMeasure()
    {
        var sql = await Translate(SumByGrade());

        sql.ShouldBe("SELECT [grade], SUM([absences]) AS [absences] FROM [dbo].[Attendance] GROUP BY [grade]");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task TheFilterAppliesBeforeTheGrouping()
    {
        var filter = new FilterExpression
        {
            Root = new FilterCondition { PropertyName = "grade", Operator = new EqualOperator(), Value = "07" },
        };

        var sql = await Translate(SumByGrade(), filter);

        sql.IndexOf("WHERE", StringComparison.Ordinal).ShouldBeLessThan(sql.IndexOf("GROUP BY", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task OrdersByAnAggregatesOutputColumnWithTheGroupColumnBreakingTies()
    {
        var sql = await Translate(SumByGrade(), ordering: OrderedBy("absences", descending: true), paging: new PagingExpression { Skip = 0, Take = 13 });

        sql.ShouldEndWith("GROUP BY [grade] ORDER BY [absences] DESC, [grade] OFFSET 0 ROWS FETCH NEXT 13 ROWS ONLY");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task APageWithNoOrderIsOrderedByTheGroupColumns()
    {
        var sql = await Translate(SumByGrade(), paging: new PagingExpression { Skip = 12, Take = 12 });

        sql.ShouldEndWith("GROUP BY [grade] ORDER BY [grade] OFFSET 12 ROWS FETCH NEXT 12 ROWS ONLY");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task CarriesTheNumberOfGroupsBeforeThePage()
    {
        var sql = await Translate(SumByGrade(includeGroupCount: true), paging: new PagingExpression { Skip = 0, Take = 5 });

        sql.ShouldContain("COUNT(*) OVER () AS [FdwGroupCount]");
        sql.IndexOf("COUNT(*) OVER ()", StringComparison.Ordinal).ShouldBeLessThan(sql.IndexOf("FROM", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task AnOrderingOnAColumnThatIsNeitherGroupedNorAggregatedIsLeftOut()
    {
        var sql = await Translate(SumByGrade(), ordering: OrderedBy("studentName", descending: false));

        sql.ShouldNotContain("ORDER BY");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task AggregatesWithNoGroupColumnReadOneRowAndTakeNoPage()
    {
        var total = new AggregationExpression
        {
            GroupByFields = [],
            Aggregations = new Dictionary<string, string> { ["absences"] = "SUM(absences)" },
            IncludeGroupCount = true,
        };

        var sql = await Translate(total, paging: new PagingExpression { Skip = 0, Take = 5 });

        sql.ShouldBe("SELECT SUM([absences]) AS [absences] FROM [dbo].[Attendance]");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task AnAliasThatIsNotAPlainNameFailsLoud()
    {
        var aggregation = new AggregationExpression
        {
            GroupByFields = ["grade"],
            Aggregations = new Dictionary<string, string> { ["total absences"] = "SUM(absences)" },
        };
        var query = new Mock<IQueryCommand>();
        query.Setup(q => q.Joins).Returns([]);
        query.Setup(q => q.Aggregation).Returns(aggregation);

        var result = await _sut.Translate(query.Object, CreateContainer().Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }
}
