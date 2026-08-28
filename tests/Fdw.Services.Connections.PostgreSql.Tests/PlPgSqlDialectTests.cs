using System;
using Fdw.Data.Abstractions;
using Fdw.Data.PostgreSql;
using Fdw.Services.Connections.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.PostgreSql.Tests;

public sealed class PlPgSqlDialectTests
{
    private readonly ISqlDialect _sut = PlPgSqlDialect.Instance;

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void QuoteIdentifierWrapsInDoubleQuotes()
    {
        _sut.QuoteIdentifier("Col").ShouldBe("\"Col\"");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void QuoteIdentifierPreservesCase()
    {
        _sut.QuoteIdentifier("MyTable").ShouldBe("\"MyTable\"");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void ParameterPrefixIsAtSign()
    {
        _sut.ParameterPrefix.ShouldBe("@");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void AlwaysFalsePredicateIsFALSE()
    {
        _sut.AlwaysFalsePredicate.ShouldBe("FALSE");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void BuildPagingClauseEmitsLimitOffset()
    {
        var paging = new PagingExpression { Skip = 10, Take = 5 };
        _sut.BuildPagingClause(paging).ShouldBe("LIMIT 5 OFFSET 10");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public void BuildPagingClauseWithZeroSkipEmitsLimitZeroOffset()
    {
        var paging = new PagingExpression { Skip = 0, Take = 20 };
        _sut.BuildPagingClause(paging).ShouldBe("LIMIT 20 OFFSET 0");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void BuildPagingClauseThrowsWhenSkipIsNegative()
    {
        var paging = new PagingExpression { Skip = -1, Take = 10 };
        Should.Throw<ArgumentException>(() => _sut.BuildPagingClause(paging));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void BuildPagingClauseThrowsWhenTakeIsZero()
    {
        var paging = new PagingExpression { Skip = 0, Take = 0 };
        Should.Throw<ArgumentException>(() => _sut.BuildPagingClause(paging));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void BuildPagingClauseThrowsWhenTakeIsNegative()
    {
        var paging = new PagingExpression { Skip = 0, Take = -5 };
        Should.Throw<ArgumentException>(() => _sut.BuildPagingClause(paging));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public void InstanceIsSingleton()
    {
        PlPgSqlDialect.Instance.ShouldBeSameAs(PlPgSqlDialect.Instance);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public void NameIsPlPgSql()
    {
        _sut.Name.ShouldBe("PlPgSql");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public void SupportsSchemaNamespaceIsTrue()
    {
        _sut.SupportsSchemaNamespace.ShouldBeTrue();
    }
}
