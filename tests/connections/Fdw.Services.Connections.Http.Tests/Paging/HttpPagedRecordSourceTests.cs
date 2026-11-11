using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Fdw.Data.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Http.Abstractions.OptionTypes.HttpProtocolOptions;
using Fdw.Services.Connections.Http.Paging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Connections.Http.Tests.Paging;

/// <summary>
/// FDW-4938: a stream of an HTTP source reads the complete set, page by page in the style the connection states, until the remote says it is
/// done; every stop it makes is a failure, never a read returned as complete.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class HttpPagedRecordSourceTests
{
    private const int Cap = 7;

    public static TheoryData<string, int> EveryStyleAndRowCount()
    {
        var data = new TheoryData<string, int>();
        foreach (var style in new[] { "OffsetLimit", "PageNumber", "LinkHeader", "BodyNextLink", "ContinuationToken" })
        {
            data.Add(style, 23); // a short last page
            data.Add(style, 21); // a whole last page, then an empty one or no pointer
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(EveryStyleAndRowCount))]
    public async Task ARemoteThatCapsItsPagesReturnsEveryRowExactlyOnceInEveryStyle(string style, int rows)
    {
        var remote = new CappedRemote(rows, Cap, style);

        var (read, failure) = await ReadAll(remote, style, SettingsFor(style), HttpReadWindow.All);

        failure.ShouldBeNull();
        read.ShouldBe(Enumerable.Range(1, rows).ToList());
        remote.Requests.Count.ShouldBeGreaterThan(rows / Cap - 1);
        remote.Requests.ShouldAllBe(request => request.Query.Contains("filter=open", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ASingleResponseRemoteIsReadInOneRequest()
    {
        var remote = new CappedRemote(40, Cap, "SingleResponse");

        var (read, failure) = await ReadAll(remote, "SingleResponse", new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase), HttpReadWindow.All);

        failure.ShouldBeNull();
        read.Count.ShouldBe(40);
        remote.Requests.Count.ShouldBe(1);
    }

    [Theory]
    [InlineData("OffsetLimit")]
    [InlineData("PageNumber")]
    public async Task ARemoteThatCapsBelowTheStatedSizeStillReturnsEveryRow(string style)
    {
        // The connection asks for 10 a page and the remote returns at most 7: a short page is not the end, only an empty one is, so the
        // first page is never returned as the whole set.
        var remote = new CappedRemote(23, Cap, style);
        var settings = SettingsFor(style);
        settings[HttpPagingProperties.PageSize] = "10";

        var (read, failure) = await ReadAll(remote, style, settings, HttpReadWindow.All);

        failure.ShouldBeNull();
        read.ShouldBe(Enumerable.Range(1, 23).ToList());
    }

    [Fact]
    public async Task AQuerysWindowStartsAtItsSkipAndStopsAfterItsTake()
    {
        var remote = new CappedRemote(50, Cap, "OffsetLimit");

        var (read, failure) = await ReadAll(remote, "OffsetLimit", SettingsFor("OffsetLimit"), new HttpReadWindow(10, 12));

        failure.ShouldBeNull();
        read.ShouldBe(Enumerable.Range(11, 12).ToList());
        remote.Requests.Count.ShouldBe(2);
    }

    [Fact]
    public async Task ASingleResponseWindowIsCutFromTheOneResponse()
    {
        var remote = new CappedRemote(40, Cap, "SingleResponse");

        var (read, failure) = await ReadAll(remote, "SingleResponse", new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase), new HttpReadWindow(5, 3));

        failure.ShouldBeNull();
        read.ShouldBe(new[] { 6, 7, 8 });
        remote.Requests.Count.ShouldBe(1);
    }

    [Fact]
    public async Task ARemoteThatReturnsMoreRowsThanAskedStopsTheRead()
    {
        var remote = new CappedRemote(50, Cap, "OffsetLimit") { IgnoresLimit = true };

        var (_, failure) = await ReadAll(remote, "OffsetLimit", SettingsFor("OffsetLimit"), HttpReadWindow.All);

        failure.ShouldNotBeNull();
        failure.CurrentMessage!.ShouldContain("more than the 7 rows asked for");
    }

    [Fact]
    public async Task ANextLinkThatRepeatsAPageAlreadyReadStopsTheRead()
    {
        var remote = new CappedRemote(50, Cap, "BodyNextLink") { LinksToItself = true };

        var (read, failure) = await ReadAll(remote, "BodyNextLink", SettingsFor("BodyNextLink"), HttpReadWindow.All);

        failure.ShouldNotBeNull();
        failure.CurrentMessage!.ShouldContain("already followed");
        read.Count.ShouldBe(Cap);
    }

    [Fact]
    public async Task AContinuationTokenThatRepeatsStopsTheRead()
    {
        var remote = new CappedRemote(50, Cap, "ContinuationToken") { LinksToItself = true };

        var (_, failure) = await ReadAll(remote, "ContinuationToken", SettingsFor("ContinuationToken"), HttpReadWindow.All);

        failure.ShouldNotBeNull();
        failure.CurrentMessage!.ShouldContain("already followed");
    }

    [Fact]
    public async Task ANextLinkToAnotherOriginStopsTheRead()
    {
        var remote = new CappedRemote(50, Cap, "LinkHeader") { LinksToOrigin = "https://elsewhere.test" };

        var (_, failure) = await ReadAll(remote, "LinkHeader", SettingsFor("LinkHeader"), HttpReadWindow.All);

        failure.ShouldNotBeNull();
        failure.CurrentMessage!.ShouldContain("another origin");
        remote.Requests.Count.ShouldBe(1);
    }

    [Fact]
    public async Task ARemoteThatNeverEndsStopsAtTheMostPagesAReadMayTake()
    {
        var remote = new CappedRemote(50, Cap, "LinkHeader") { NeverEnds = true };
        var settings = SettingsFor("LinkHeader");
        settings[HttpPagingProperties.MaxPages] = "3";

        var (_, failure) = await ReadAll(remote, "LinkHeader", settings, HttpReadWindow.All);

        failure.ShouldNotBeNull();
        failure.CurrentMessage!.ShouldContain("after 3 pages");
        remote.Requests.Count.ShouldBe(3);
    }

    [Fact]
    public async Task ARefusedPageEndsTheStreamWithTheFailure()
    {
        var plan = Plan("OffsetLimit", SettingsFor("OffsetLimit"), HttpReadWindow.All);
        using var client = new HttpClient(new RefusingRemote()) { BaseAddress = new Uri("http://remote.test/api/") };
        await using var source = new HttpPagedRecordSource(
            client, new HttpRequestMessage(HttpMethod.Get, new Uri("items?filter=open", UriKind.Relative)), Protocol(), Container(), Fields, plan, NullLogger.Instance);

        var results = new List<IGenericResult<DataRecord>>();
        await foreach (var result in source.Read(TestContext.Current.CancellationToken))
            results.Add(result);

        results.Count.ShouldBe(1);
        results[0].IsFailure.ShouldBeTrue();
    }

    private static readonly IReadOnlyList<IDataField> Fields = [Field("id")];

    private static Dictionary<string, string?> SettingsFor(string style) => style switch
    {
        "OffsetLimit" => new(StringComparer.OrdinalIgnoreCase)
        {
            [HttpPagingProperties.OffsetParameter] = "skip",
            [HttpPagingProperties.LimitParameter] = "top",
            [HttpPagingProperties.PageSize] = "7",
        },
        "PageNumber" => new(StringComparer.OrdinalIgnoreCase)
        {
            [HttpPagingProperties.PageNumberParameter] = "p",
            [HttpPagingProperties.PageSizeParameter] = "size",
            [HttpPagingProperties.FirstPageNumber] = "1",
            [HttpPagingProperties.PageSize] = "7",
        },
        "LinkHeader" => new(StringComparer.OrdinalIgnoreCase)
        {
            [HttpPagingProperties.PageSizeParameter] = "per_page",
            [HttpPagingProperties.PageSize] = "7",
        },
        "BodyNextLink" => new(StringComparer.OrdinalIgnoreCase)
        {
            [HttpPagingProperties.NextLinkPath] = "@odata.nextLink",
        },
        "ContinuationToken" => new(StringComparer.OrdinalIgnoreCase)
        {
            [HttpPagingProperties.TokenPath] = "meta.next_cursor",
            [HttpPagingProperties.TokenParameter] = "at",
        },
        _ => new(StringComparer.OrdinalIgnoreCase),
    };

    private static HttpPagingPlan Plan(string style, Dictionary<string, string?> settings, HttpReadWindow window)
    {
        var plan = HttpPaging.PlanFor(style, new HttpPagingSettings("Remote", settings, NullLogger.Instance), window);
        plan.IsSuccess.ShouldBeTrue(plan.CurrentMessage);
        return plan.Value!;
    }

    private static async Task<(List<int> Read, IGenericResult<DataRecord>? Failure)> ReadAll(
        CappedRemote remote, string style, Dictionary<string, string?> settings, HttpReadWindow window)
    {
        using var client = new HttpClient(remote) { BaseAddress = new Uri("http://remote.test/api/") };
        await using var source = new HttpPagedRecordSource(
            client,
            new HttpRequestMessage(HttpMethod.Get, new Uri("items?filter=open", UriKind.Relative)),
            Protocol(),
            Container(),
            Fields,
            Plan(style, settings, window),
            NullLogger.Instance);

        var read = new List<int>();
        await foreach (var result in source.Read(TestContext.Current.CancellationToken))
        {
            if (result.IsFailure)
                return (read, result);
            read.Add(Convert.ToInt32(result.Value.Values[0], System.Globalization.CultureInfo.InvariantCulture));
        }

        return (read, null);
    }

    // The protocol's row reader for the tests: each page's "rows" array, one record per row.
    private static IHttpProtocol Protocol()
    {
        var protocol = new Mock<IHttpProtocol>();
        protocol.Setup(p => p.Name).Returns("Rest");
        protocol
            .Setup(p => p.TryOpenRecordSource(It.IsAny<IStorageContainer>(), It.IsAny<Stream>(), It.IsAny<IReadOnlyList<IDataField>>(), It.IsAny<ILogger?>()))
            .Returns<IStorageContainer, Stream, IReadOnlyList<IDataField>, ILogger?>((_, stream, fields, _) =>
            {
                using var document = JsonDocument.Parse(stream);
                var rows = document.RootElement.GetProperty("rows").EnumerateArray()
                    .Select(row => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?> { ["id"] = row.GetProperty("id").GetInt32() })
                    .ToList();
                return GenericResult<IRecordSource<DataRecord>?>.Success(new RowListRecordSource(new RecordSchema(fields), rows));
            });
        return protocol.Object;
    }

    private static IStorageContainer Container() => new Mock<IStorageContainer>().Object;

    private static IDataField Field(string name)
    {
        var field = new Mock<IDataField>();
        field.Setup(f => f.Name).Returns(name);
        return field.Object;
    }

    [ExcludeFromCodeCoverage]
    private sealed class RefusingRemote : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.Forbidden));
    }
}
