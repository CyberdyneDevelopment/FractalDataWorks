using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Data.MsSql;
using Fdw.Data.MsSql.Tests.TestSupport;
using Fdw.Results;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Data.MsSql.Tests.Translators;

/// <summary>
/// A page of rows with no stated ordering follows the container's primary key, the WHOLE key (FDW-6309). Ordered by the first column of a
/// composite key alone, rows that share it are in no fixed order, and a row can repeat or be skipped between two pages.
/// </summary>
[Collection(nameof(DataMsSqlTestCollection))]
public sealed class MsSqlQueryTranslatorPagingKeyTests
{
    private readonly MsSqlQueryTranslator _sut = new();

    private static Mock<IDataContainer> Container(string[] columns, params string[] key)
    {
        var fields = columns.Select(column =>
        {
            var field = new Mock<IField>();
            field.Setup(f => f.FieldType).Returns(TestFieldTypes.Text);
            field.Setup(f => f.Name).Returns(column);
            return field.Object;
        }).ToArray();

        var schema = new Mock<IContainerSchema>();
        schema.Setup(s => s.Fields).Returns(fields);
        schema.Setup(s => s.GetReturnedFields(It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>())).Returns(fields);

        var container = new Mock<IDataContainer>();
        container.Setup(c => c.Name).Returns("OrderLines");
        container.As<IStorageContainer>().Setup(c => c.Name).Returns("OrderLines");
        container.As<IStorageContainer>().Setup(c => c.Path).Returns(new DatabasePath(string.Empty, "dbo", "OrderLines"));
        container.Setup(c => c.Schema).Returns(schema.Object);
        container.Setup(c => c.ReferencingKeys).Returns(GenericResult<IReadOnlyList<ReferencingKeyBinding>>.Success([]));
        container.Setup(c => c.Keys).Returns(key.Length == 0 ? new List<IContainerKey>() : TestPrimaryKeys.On(key));
        return container;
    }

    private static Mock<IQueryCommand> Page()
    {
        var query = new Mock<IQueryCommand>();
        query.Setup(q => q.Joins).Returns([]);
        query.Setup(q => q.Filter).Returns((IFilterExpression?)null);
        query.Setup(q => q.Projection).Returns((IProjectionExpression?)null);
        query.Setup(q => q.Ordering).Returns((IOrderingExpression?)null);
        query.Setup(q => q.Paging).Returns(new PagingExpression { Skip = 100, Take = 50 });
        return query;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task APageOfACompositeKeyTableIsOrderedByEveryColumnOfItsKeyInKeyOrder()
    {
        var container = Container(["OrderId", "LineNo", "Status"], "OrderId", "LineNo");

        var result = await _sut.Translate(Page().Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("ORDER BY [OrderId], [LineNo] OFFSET 100 ROWS FETCH NEXT 50 ROWS ONLY");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task APageOfASingleColumnKeyTableIsOrderedByThatColumnAsBefore()
    {
        var container = Container(["Id", "Status"], "Id");

        var result = await _sut.Translate(Page().Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("ORDER BY [Id] OFFSET 100 ROWS FETCH NEXT 50 ROWS ONLY");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task APageOfATableThatDeclaresNoKeyIsStillRefused()
    {
        var container = Container(["Id", "Status"]);

        var result = await _sut.Translate(Page().Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }
}
