using System.Linq;
using Fdw.Web.RestEndpoints.Models;

namespace Fdw.Web.RestEndpoints.Tests.Pagination;

/// <summary>
/// <see cref="PaginatedListRequest.Apply{T}"/> cuts the page every list endpoint sends.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "Api")]
public sealed class PaginatedListRequestTests
{
    private static readonly int[] Hundred50 = Enumerable.Range(1, 150).ToArray();

    [Fact]
    public void AFirstPageOfAHundredCarriesTheFullTotal()
    {
        var page = new PaginatedListRequest { Take = 100 }.Apply(Hundred50);

        page.Items.Count.ShouldBe(100);
        page.Items[0].ShouldBe(1);
        page.TotalCount.ShouldBe(150);
        page.HasMore.ShouldBeTrue();
    }

    [Fact]
    public void SkipAndTakeCutTheMiddle()
    {
        var page = new PaginatedListRequest { Skip = 10, Take = 5 }.Apply(Hundred50);

        page.Items.ShouldBe(new[] { 11, 12, 13, 14, 15 });
        page.Skip.ShouldBe(10);
        page.Take.ShouldBe(5);
    }

    [Fact]
    public void TheLastPageSaysThereIsNoMore()
    {
        var page = new PaginatedListRequest { Skip = 140, Take = 50 }.Apply(Hundred50);

        page.Items.Count.ShouldBe(10);
        page.HasMore.ShouldBeFalse();
    }

    [Fact]
    public void ASkipPastTheEndIsAnEmptyPageNotAnError()
    {
        var page = new PaginatedListRequest { Skip = 500, Take = 10 }.Apply(Hundred50);

        page.Items.ShouldBeEmpty();
        page.TotalCount.ShouldBe(150);
    }
}
