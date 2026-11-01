using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Web.RestEndpoints.Query;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests.DataQuery;

/// <summary>
/// FDW-1075: every route that returns a data set's rows reads it through <see cref="DataSetQuery"/>: the read gate, the data
/// set path of the gateway, the filterable check and only the declared fields.
/// </summary>
public sealed class DataSetQueryTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task RowsCarryOnlyTheDataSetsDeclaredFields()
    {
        var gateway = Gateway([Row(("Name", "Ada"), ("Salary", 100m), ("SSN", "123-45-6789"))]);

        var page = await new DataSetQuery(Provider(gateway), NullLogger.Instance)
            .Run(People(), new DataQueryRequest { DataSetName = "People", Take = 10 }, 10, 100, DataSetQuery.Unscoped, TestContext.Current.CancellationToken);

        page.IsSuccess.ShouldBeTrue();
        var row = page.Value.ShouldNotBeNull().Rows.ShouldHaveSingleItem();
        row.Keys.ShouldBe(new[] { "Name", "Salary" }, ignoreOrder: true);
        page.Value.Columns.Select(c => c.Name).ShouldNotContain("SSN");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ReadsThroughTheDataSetNotItsContainer()
    {
        var gateway = Gateway([Row(("Name", "Ada"))]);

        await new DataSetQuery(Provider(gateway), NullLogger.Instance)
            .Run(People(), new DataQueryRequest { DataSetName = "People", Take = 10 }, 10, 100, DataSetQuery.Unscoped, TestContext.Current.CancellationToken);

        gateway.Verify(g => g.Execute<IEnumerable<Dictionary<string, object?>>>(
            It.IsAny<IDataCommand>(), It.Is<DataSetTarget>(t => t.DataSet == "People"), It.IsAny<CancellationToken>()), Times.Once);
        gateway.Verify(g => g.Execute<IEnumerable<Dictionary<string, object?>>>(
            It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AFilterOnAFieldThatIsNotFilterableIsRefusedBeforeAnyRead()
    {
        var gateway = Gateway([]);
        var request = new DataQueryRequest
        {
            DataSetName = "People",
            Take = 10,
            Filter = new QueryFilterNode { Field = "Salary", Operator = "GreaterThan", Value = Json("100000") },
        };

        var page = await new DataSetQuery(Provider(gateway), NullLogger.Instance)
            .Run(People(), request, 10, 100, DataSetQuery.Unscoped, TestContext.Current.CancellationToken);

        page.IsFailure.ShouldBeTrue();
        page.CurrentMessage.ShouldNotBeNull().ShouldContain("'Salary' is not a field of this data set that can be filtered on");
        gateway.Verify(g => g.Execute<IEnumerable<Dictionary<string, object?>>>(
            It.IsAny<IDataCommand>(), It.IsAny<DataSetTarget>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task AFederatedSetIsRefusedByTheReadGate()
    {
        var gateway = Gateway([]);
        var federated = new FederatedDataSetConfiguration { Name = "BreweriesInNflCities", Implementation = "Federated" };

        var page = await new DataSetQuery(Provider(gateway), NullLogger.Instance)
            .Run(federated, new DataQueryRequest { DataSetName = federated.Name, Take = 10 }, 10, 100, DataSetQuery.Unscoped, TestContext.Current.CancellationToken);

        page.IsFailure.ShouldBeTrue();
        page.CurrentMessage.ShouldNotBeNull().ShouldContain("kind 'Federated' does not page in the store");
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OneRowPastThePageSaysMoreRowsExist()
    {
        var gateway = Gateway([Row(("Name", "Ada")), Row(("Name", "Grace")), Row(("Name", "Edsger"))]);

        var page = await new DataSetQuery(Provider(gateway), NullLogger.Instance)
            .Run(People(), new DataQueryRequest { DataSetName = "People", Take = 2 }, 2, 100, DataSetQuery.Unscoped, TestContext.Current.CancellationToken);

        page.Value.ShouldNotBeNull().Rows.Count.ShouldBe(2);
        page.Value.HasMoreRows.ShouldBeTrue();
    }

    internal static SimpleDataSetConfiguration People() => new()
    {
        Name = "People",
        Implementation = "Simple",
        Fields =
        [
            new DataSetFieldConfiguration { Name = "Name", Ordinal = 0, TypeName = "String", IsKey = true },
            new DataSetFieldConfiguration { Name = "Salary", Ordinal = 1, TypeName = "Decimal", IsFilterable = false },
        ],
    };

    internal static Dictionary<string, object?> Row(params (string Name, object? Value)[] cells)
        => cells.ToDictionary(c => c.Name, c => c.Value);

    internal static Mock<IDataGateway> Gateway(List<Dictionary<string, object?>> rows)
    {
        var gateway = new Mock<IDataGateway>();
        gateway
            .Setup(g => g.Execute<IEnumerable<Dictionary<string, object?>>>(It.IsAny<IDataCommand>(), It.IsAny<DataSetTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<Dictionary<string, object?>>>.Success(rows));
        return gateway;
    }

    internal static IDataGatewayProvider Provider(Mock<IDataGateway> gateway)
    {
        var provider = new Mock<IDataGatewayProvider>();
        provider.Setup(p => p.ByName("Main")).Returns(gateway.Object);
        return provider.Object;
    }

    private static JsonElement Json(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
