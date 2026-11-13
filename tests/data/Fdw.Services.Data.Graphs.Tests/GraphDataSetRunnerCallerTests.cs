using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Graphs.Tests;

/// <summary>
/// FDW-5298: a graph reads every source as the caller whose read it is. The caller lives in the ambient authentication context, the runner reads
/// through the gateway it is handed, and the engine runs its work on tasks that carry the ambient context, so a source is opened for the caller and
/// never for a service identity, and two callers reading at once never see each other's rows.
/// </summary>
public sealed class GraphDataSetRunnerCallerTests
{
    private static GraphDataSetRunner Runner() =>
        new(new GraphDataSetReadOptions(1000, 30, 2), NullLogger<GraphDataSetRunner>.Instance);

    private static GraphDataSetConfiguration OneSource()
    {
        var graph = new GraphDataSetConfiguration { Name = "Weather", Implementation = "Graph", FieldsOutComponent = "src", FieldsOutOutput = "output" };
        graph.Components.Add(new GraphDataSetComponentConfiguration { Name = "src", ComponentType = "DataSetSource", DataSetName = "Readings", BatchSize = 2 });
        return graph;
    }

    private static IAuthenticationContext Caller(string userId)
    {
        var caller = new Mock<IAuthenticationContext>();
        caller.Setup(c => c.UserId).Returns(userId);
        return caller.Object;
    }

    // Why the rows carry the caller: the source stands in for a store whose row rules narrow what it answers to the session it is opened under.
    private static IDataGateway GatewayAnsweringTheCaller(AuthenticationContextAccessor accessor, ConcurrentBag<string> opened, Dictionary<string, int> rowsPerCaller)
    {
        var gateway = new Mock<IDataGateway>();
        gateway
            .Setup(g => g.OpenRecordSource(It.IsAny<IDataCommand>(), It.IsAny<DataSetTarget>(), It.IsAny<CancellationToken>()))
            .Returns(async (IDataCommand _, DataSetTarget _, CancellationToken token) =>
            {
                // Why a pause: it makes the two reads overlap, so a source opened under the wrong caller shows.
                await Task.Delay(50, token).ConfigureAwait(false);
                var user = accessor.Current?.UserId ?? "nobody";
                opened.Add(user);
                IReadOnlyList<IReadOnlyDictionary<string, object?>> rows =
                    [.. Enumerable.Range(0, rowsPerCaller[user]).Select(i => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>(StringComparer.Ordinal) { ["Id"] = i, ["Owner"] = user })];
                return GenericResult<IRecordSource<DataRecord>>.Success(new RowListRecordSource(RowListRecordSource.SchemaOfRows(rows), rows));
            });
        return gateway.Object;
    }

    private static async Task<IGenericResult<IReadOnlyList<IDictionary<string, object?>>>> ReadAs(
        AuthenticationContextAccessor accessor, string userId, IDataGateway gateway, CancellationToken cancellationToken)
    {
        // Why Task.Run: each caller's read starts in a flow of its own, as two requests do, and sets its own ambient context there.
        return await Task.Run(
            async () =>
            {
                accessor.Current = Caller(userId);
                return await Runner().Read(gateway, OneSource(), cancellationToken).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5298")]
    public async Task AGraphReadAsOneCallerSeesThatCallersRowsAndTheSourceIsOpenedUnderThatCaller()
    {
        var accessor = new AuthenticationContextAccessor();
        var opened = new ConcurrentBag<string>();
        var gateway = GatewayAnsweringTheCaller(accessor, opened, new Dictionary<string, int>(StringComparer.Ordinal) { ["alice"] = 3 });

        var result = await ReadAs(accessor, "alice", gateway, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var rows = result.Value.ShouldNotBeNull();
        rows.Count.ShouldBe(3);
        rows.Select(row => row["Owner"]).Distinct().ShouldBe(["alice"]);
        opened.ShouldBe(["alice"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5298")]
    public async Task TwoCallersReadingTheSameGraphAtOnceEachSeeTheirOwnRowsAndNeverTheOthers()
    {
        var accessor = new AuthenticationContextAccessor();
        var opened = new ConcurrentBag<string>();
        var gateway = GatewayAnsweringTheCaller(accessor, opened, new Dictionary<string, int>(StringComparer.Ordinal) { ["alice"] = 3, ["bob"] = 2 });

        var both = await Task.WhenAll(
            ReadAs(accessor, "alice", gateway, TestContext.Current.CancellationToken),
            ReadAs(accessor, "bob", gateway, TestContext.Current.CancellationToken));

        both[0].IsSuccess.ShouldBeTrue(both[0].CurrentMessage);
        both[1].IsSuccess.ShouldBeTrue(both[1].CurrentMessage);
        var alice = both[0].Value.ShouldNotBeNull();
        var bob = both[1].Value.ShouldNotBeNull();
        alice.Count.ShouldBe(3);
        alice.Select(row => row["Owner"]).Distinct().ShouldBe(["alice"]);
        bob.Count.ShouldBe(2);
        bob.Select(row => row["Owner"]).Distinct().ShouldBe(["bob"]);
        opened.OrderBy(user => user, StringComparer.Ordinal).ShouldBe(["alice", "bob"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5298")]
    public async Task ARunnerHoldsNoCallerBetweenReadsSoALaterCallerIsNeverReadAsAnEarlierOne()
    {
        var accessor = new AuthenticationContextAccessor();
        var opened = new ConcurrentBag<string>();
        var gateway = GatewayAnsweringTheCaller(accessor, opened, new Dictionary<string, int>(StringComparer.Ordinal) { ["alice"] = 3, ["bob"] = 2 });
        var runner = Runner();

        var first = await Task.Run(async () => { accessor.Current = Caller("alice"); return await runner.Read(gateway, OneSource(), TestContext.Current.CancellationToken); }, TestContext.Current.CancellationToken);
        var second = await Task.Run(async () => { accessor.Current = Caller("bob"); return await runner.Read(gateway, OneSource(), TestContext.Current.CancellationToken); }, TestContext.Current.CancellationToken);

        first.IsSuccess.ShouldBeTrue(first.CurrentMessage);
        second.IsSuccess.ShouldBeTrue(second.CurrentMessage);
        first.Value.ShouldNotBeNull().Select(row => row["Owner"]).Distinct().ShouldBe(["alice"]);
        second.Value.ShouldNotBeNull().Select(row => row["Owner"]).Distinct().ShouldBe(["bob"]);
    }
}
