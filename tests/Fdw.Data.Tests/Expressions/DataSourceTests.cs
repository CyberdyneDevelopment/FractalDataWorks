using Fdw.Data;
using Fdw.Data.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.Tests.Expressions;

public sealed class DataSourceTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void PropertiesArePreserved()
    {
        var sut = new DataSource
        {
            Name = "Orders",
            ContainerName = "OrdersTable",
            ConnectionName = "DefaultSql",
            Alias = "o"
        };

        sut.Name.ShouldBe("Orders");
        sut.ContainerName.ShouldBe("OrdersTable");
        sut.ConnectionName.ShouldBe("DefaultSql");
        sut.Alias.ShouldBe("o");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AliasCanBeNull()
    {
        var sut = new DataSource
        {
            Name = "Orders",
            ContainerName = "OrdersTable",
            ConnectionName = "DefaultSql"
        };

        sut.Alias.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FilterCanBeNull()
    {
        var sut = new DataSource
        {
            Name = "Orders",
            ContainerName = "OrdersTable",
            ConnectionName = "DefaultSql"
        };

        sut.Filter.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FilterCanBeSet()
    {
        var filter = new FilterExpression
        {
            Root = new FilterCondition
            {
                PropertyName = "Active",
                Operator = new EqualOperator(),
                Value = true
            }
        };

        var sut = new DataSource
        {
            Name = "Orders",
            ContainerName = "OrdersTable",
            ConnectionName = "DefaultSql",
            Filter = filter
        };

        sut.Filter.ShouldNotBeNull();
        sut.Filter.Root.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ImplementsIDataSource()
    {
        var sut = new DataSource
        {
            Name = "X",
            ContainerName = "X",
            ConnectionName = "X"
        };
        sut.ShouldBeAssignableTo<IDataSource>();
    }
}
