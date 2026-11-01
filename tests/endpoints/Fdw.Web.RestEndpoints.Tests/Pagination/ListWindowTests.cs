using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Web.RestEndpoints.Models;
using Fdw.Web.RestEndpoints.Pagination;

namespace Fdw.Web.RestEndpoints.Tests.Pagination;

/// <summary>
/// FDW-1194: a list read whole and searched in memory is cut into pages in the envelope every list answers in.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "Api")]
public sealed class ListWindowTests
{
    private static readonly IReadOnlyList<string> Ten = Enumerable.Range(1, 10).Select(n => $"item-{n}").ToList();

    [Fact]
    public async Task TheFirstPageHoldsTheFirstItemsAndSaysAnotherFollows()
    {
        var page = ListWindow.Of(Ten, new PaginatedListRequest { Skip = 0, Take = 4 }, includeTotal: false);

        (await page.Items.ToListAsync(TestContext.Current.CancellationToken)).ShouldBe(["item-1", "item-2", "item-3", "item-4"]);
        page.Skip.ShouldBe(0);
        page.Take.ShouldBe(4);
        page.HasMore.ShouldBeTrue();
    }

    [Fact]
    public async Task TheLastPageSaysNothingFollows()
    {
        var page = ListWindow.Of(Ten, new PaginatedListRequest { Skip = 8, Take = 4 }, includeTotal: false);

        (await page.Items.ToListAsync(TestContext.Current.CancellationToken)).ShouldBe(["item-9", "item-10"]);
        page.HasMore.ShouldBeFalse();
    }

    [Fact]
    public async Task AFullLastPageDoesNotSayAnotherFollows()
    {
        var page = ListWindow.Of(Ten, new PaginatedListRequest { Skip = 6, Take = 4 }, includeTotal: false);

        (await page.Items.ToListAsync(TestContext.Current.CancellationToken)).Count.ShouldBe(4);
        page.HasMore.ShouldBeFalse();
    }

    [Fact]
    public async Task APageBeyondTheEndIsEmpty()
    {
        var page = ListWindow.Of(Ten, new PaginatedListRequest { Skip = 50, Take = 4 }, includeTotal: true);

        (await page.Items.ToListAsync(TestContext.Current.CancellationToken)).ShouldBeEmpty();
        page.HasMore.ShouldBeFalse();
        page.Total.ShouldBe(10);
    }

    // An absent total means not asked, where a zero would say the list is empty.
    [Fact]
    public void TheTotalIsCarriedOnlyWhenAsked()
    {
        ListWindow.Of(Ten, new PaginatedListRequest { Skip = 0, Take = 4 }, includeTotal: false).Total.ShouldBeNull();
        ListWindow.Of(Ten, new PaginatedListRequest { Skip = 0, Take = 4 }, includeTotal: true).Total.ShouldBe(10);
    }

    [Fact]
    public void ThePageEndsWithoutAnError()
    {
        ListWindow.Of(Ten, new PaginatedListRequest { Skip = 0, Take = 4 }, includeTotal: false).Error.ShouldBeNull();
    }
}
