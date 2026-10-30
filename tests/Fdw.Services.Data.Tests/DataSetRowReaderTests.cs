using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Endpoints;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// FDW-3165, FDW-3130: a data set's rows are read through the gateway the caller holds, up to a cap, by the path <c>/data</c> reads by
/// (<see cref="DataSetQuery"/>); a view's stored filter is applied, a data set over the cap and an unknown name are refused, and a
/// stored filter that cannot be read refuses, never reading short or unfiltered.
/// </summary>
public sealed class DataSetRowReaderTests
{
    private const string QuarterbacksFilter = "{\"field\":\"Position\",\"operator\":\"Equal\",\"value\":\"QB\"}";

    private static readonly SimpleDataSetConfiguration Players = new()
    {
        Id = Guid.NewGuid(),
        Name = "AfcPlayers",
        Implementation = "Simple",
        Fields =
        [
            new DataSetFieldConfiguration { Name = "Name", Ordinal = 0, TypeName = "String", IsKey = true },
            new DataSetFieldConfiguration { Name = "Position", Ordinal = 1, TypeName = "String" },
        ],
    };

    private static DataSetRowReader ReaderResolving(ResolvedDataSet? resolved)
    {
        var resolver = new Mock<IDataSetResolver>();
        resolver
            .Setup(r => r.Resolve("AfcPlayers", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ResolvedDataSet?>.Success(resolved));
        return new DataSetRowReader(resolver.Object, NullLogger<DataSetRowReader>.Instance);
    }

    private static Dictionary<string, object?> Row(string name, string position, string? undeclared = null)
    {
        var row = new Dictionary<string, object?> { ["Name"] = name, ["Position"] = position };
        if (undeclared is not null)
            row["Ssn"] = undeclared;

        return row;
    }

    private static Mock<IDataGateway> GatewayReturning(List<IDataCommand> received, params Dictionary<string, object?>[] rows)
    {
        var gateway = new Mock<IDataGateway>();
        gateway
            .Setup(g => g.Execute<IEnumerable<Dictionary<string, object?>>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataSetTarget>(), It.IsAny<CancellationToken>()))
            .Callback<IDataCommand, DataSetTarget, CancellationToken>((command, _, _) => received.Add(command))
            .ReturnsAsync(GenericResult<IEnumerable<Dictionary<string, object?>>>.Success(rows));
        return gateway;
    }

    private static ResolvedDataSet DataSet() => new(Players, null, null, []);

    private static ResolvedDataSet Quarterbacks() => new(Players, null, "AfcPlayers", [QuarterbacksFilter]);

    [Fact]
    public async Task A_data_set_under_the_cap_is_read_whole_through_the_callers_gateway()
    {
        var received = new List<IDataCommand>();
        var gateway = GatewayReturning(received, Row("Ada", "QB", "123-45"), Row("Grace", "WR"));

        var result = await ReaderResolving(DataSet()).Read(gateway.Object, "AfcPlayers", 5, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.Count.ShouldBe(2);
        gateway.Verify(
            g => g.Execute<IEnumerable<Dictionary<string, object?>>>(
                It.IsAny<IDataCommand>(), It.Is<DataSetTarget>(t => t.DataSet == "AfcPlayers"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Rows_carry_only_the_data_sets_declared_fields_as_they_do_on_the_data_routes()
    {
        var gateway = GatewayReturning([], Row("Ada", "QB", "123-45"));

        var result = await ReaderResolving(DataSet()).Read(gateway.Object, "AfcPlayers", 5, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.ShouldHaveSingleItem().Keys.ShouldBe(["Name", "Position"], ignoreOrder: true);
    }

    [Fact]
    public async Task A_data_set_that_holds_more_rows_than_the_cap_is_refused_and_not_cut_short()
    {
        var gateway = GatewayReturning([], Row("Ada", "QB"), Row("Grace", "WR"));

        var result = await ReaderResolving(DataSet()).Read(gateway.Object, "AfcPlayers", 1, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages[^1].Code.ShouldBe("DATA-21101");
    }

    [Fact]
    public async Task A_view_is_read_with_its_stored_filter_sent_to_the_store()
    {
        var received = new List<IDataCommand>();
        var gateway = GatewayReturning(received, Row("Ada", "QB"));

        var result = await ReaderResolving(Quarterbacks()).Read(gateway.Object, "AfcPlayers", 5, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var query = received.ShouldHaveSingleItem().ShouldBeOfType<QueryCommand<Dictionary<string, object?>>>();
        var condition = query.Filter.ShouldNotBeNull().Root.ShouldBeOfType<FilterCondition>();
        condition.PropertyName.ShouldBe("Position");
        condition.Value.ShouldBe("QB");
    }

    [Fact]
    public async Task A_view_read_by_the_reader_and_by_the_data_query_sends_the_same_command_and_returns_the_same_rows()
    {
        var viaReader = new List<IDataCommand>();
        var viaQuery = new List<IDataCommand>();
        Dictionary<string, object?>[] stored = [Row("Ada", "QB"), Row("Grace", "QB")];
        var view = Quarterbacks();

        var read = await ReaderResolving(view).Read(GatewayReturning(viaReader, stored).Object, "AfcPlayers", 5, TestContext.Current.CancellationToken);
        var page = await new DataSetQuery(NullLogger.Instance).Run(
            GatewayReturning(viaQuery, stored).Object,
            view.DataSet,
            new DataQueryRequest { DataSetName = "AfcPlayers", Skip = 0, Take = 5 },
            5,
            5,
            _ => Task.FromResult(StoredViewFilters.Of(view.ViewFilters, "AfcPlayers", NullLogger.Instance)),
            TestContext.Current.CancellationToken);

        read.IsSuccess.ShouldBeTrue(read.CurrentMessage);
        page.IsSuccess.ShouldBeTrue(page.CurrentMessage);
        var readerFilter = viaReader.ShouldHaveSingleItem().ShouldBeOfType<QueryCommand<Dictionary<string, object?>>>().Filter.ShouldNotBeNull();
        var queryFilter = viaQuery.ShouldHaveSingleItem().ShouldBeOfType<QueryCommand<Dictionary<string, object?>>>().Filter.ShouldNotBeNull();
        readerFilter.Root.ShouldBe(queryFilter.Root);
        read.Value!.Select(row => row["Name"]).ShouldBe(page.Value!.Rows.Select(row => row["Name"]));
    }

    [Fact]
    public async Task A_view_whose_stored_filter_cannot_be_read_is_refused_and_nothing_is_read()
    {
        var received = new List<IDataCommand>();
        var gateway = GatewayReturning(received, Row("Ada", "QB"));
        var view = new ResolvedDataSet(Players, null, "AfcPlayers", ["not a filter tree"]);

        var result = await ReaderResolving(view).Read(gateway.Object, "AfcPlayers", 5, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages[^1].Code.ShouldBe("DATAENDPOINTS-21001");
        received.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_name_no_data_set_or_view_carries_is_refused()
    {
        var result = await ReaderResolving(null).Read(GatewayReturning([]).Object, "AfcPlayers", 5, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task A_cap_that_is_not_positive_is_a_programming_error()
    {
        await Should.ThrowAsync<ArgumentOutOfRangeException>(
            () => ReaderResolving(DataSet()).Read(GatewayReturning([]).Object, "AfcPlayers", 0, TestContext.Current.CancellationToken));
    }
}
