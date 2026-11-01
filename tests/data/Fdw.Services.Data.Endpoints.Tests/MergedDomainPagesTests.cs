using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Endpoints;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// FDW-2392: a page of several name-ordered sources is read by asking each for the first skip + take + 1 rows, merging by name and
/// cutting the window, and says more follows when a row is left past it.
/// </summary>
public sealed class MergedDomainPagesTests
{
    private static readonly DomainQuery Asked = new(null, null, 0, 3, false);

    [Fact]
    public async Task TwoSourcesAreMergedInNameOrderIgnoringCase()
    {
        var result = await MergedDomainPages.Read([Source("alpha", "Delta"), Source("beta", "Echo")], Asked, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        (await Names(result.Value!)).ShouldBe(new[] { "alpha", "beta", "Delta" });
        result.Value!.HasMore.ShouldBeTrue();
    }

    [Fact]
    public async Task TheWindowSkipsRowsOfBothSources()
    {
        var result = await MergedDomainPages.Read(
            [Source("a", "d"), Source("b", "e"), Source("c")], new DomainQuery(null, null, 2, 2, false), TestContext.Current.CancellationToken);

        (await Names(result.Value!)).ShouldBe(new[] { "c", "d" });
        result.Value!.HasMore.ShouldBeTrue();
    }

    [Fact]
    public async Task WhenNothingIsLeftPastTheWindowNoMoreFollows()
    {
        var result = await MergedDomainPages.Read([Source("a"), Source("b")], Asked, TestContext.Current.CancellationToken);

        (await Names(result.Value!)).ShouldBe(new[] { "a", "b" });
        result.Value!.HasMore.ShouldBeFalse();
    }

    [Fact]
    public async Task EachSourceIsAskedForTheRowsTheMergeCanNeedAndNoMore()
    {
        DomainQuery? seen = null;
        MergedDomainPages.Source source = (query, _) =>
        {
            seen = query;
            return Task.FromResult<IGenericResult<MergedDomainPages.Rows>>(GenericResult<MergedDomainPages.Rows>.Success(new MergedDomainPages.Rows([], null)));
        };

        await MergedDomainPages.Read([source], new DomainQuery(null, null, 20, 10, true), TestContext.Current.CancellationToken);

        seen!.Skip.ShouldBe(0);
        seen.Take.ShouldBe(31);
        seen.IncludeTotal.ShouldBeTrue();
    }

    [Fact]
    public async Task TheTotalIsTheSumOfTheSourcesAndAbsentWhenOneCannotSayAndWhenNotAsked()
    {
        var asked = new DomainQuery(null, null, 0, 3, true);
        var summed = await MergedDomainPages.Read([SourceWithTotal(4, "a"), SourceWithTotal(6, "b")], asked, TestContext.Current.CancellationToken);
        var partial = await MergedDomainPages.Read([SourceWithTotal(4, "a"), SourceWithTotal(null, "b")], asked, TestContext.Current.CancellationToken);
        var notAsked = await MergedDomainPages.Read([SourceWithTotal(4, "a")], Asked, TestContext.Current.CancellationToken);

        summed.Value!.Total.ShouldBe(10);
        partial.Value!.Total.ShouldBeNull();
        notAsked.Value!.Total.ShouldBeNull();
    }

    [Fact]
    public async Task AFailingSourceFailsTheMerge()
    {
        MergedDomainPages.Source failing = (_, _) => Task.FromResult<IGenericResult<MergedDomainPages.Rows>>(
            GenericResult<MergedDomainPages.Rows>.Failure(new Fdw.Messages.GenericMessage("source unavailable")));

        var result = await MergedDomainPages.Read([Source("a"), failing], Asked, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldBe("source unavailable");
    }

    private static MergedDomainPages.Source Source(params string[] names) => SourceWithTotal(null, names);

    private static MergedDomainPages.Source SourceWithTotal(int? total, params string[] names)
    {
        IReadOnlyList<DomainConfiguration> rows = [.. names.Select(name => new DomainConfiguration { Name = name })];
        return (_, _) => Task.FromResult<IGenericResult<MergedDomainPages.Rows>>(GenericResult<MergedDomainPages.Rows>.Success(new MergedDomainPages.Rows(rows, total)));
    }

    private static async Task<string[]> Names(ConfigurationPage<DomainConfiguration> page)
    {
        var names = new List<string>();
        await foreach (var row in page.Items)
            names.Add(row.Name);
        return [.. names];
    }
}
