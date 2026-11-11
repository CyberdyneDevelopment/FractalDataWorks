using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Connections.Http.Paging;
using Fdw.ServiceTypes;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Connections.Http.Tests.Paging;

/// <summary>
/// FDW-4938: a connection names its paging style and states what the style needs; no style is guessed, and a missing or wrong property is
/// refused naming it before any request is sent.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class HttpPagingStylesTests
{
    [Fact]
    public void EveryStyleIsInTheCollection()
        => HttpPagingStyles.All().Select(style => style.Name).OrderBy(name => name, StringComparer.Ordinal).ShouldBe(
            new[] { "BodyNextLink", "ContinuationToken", "LinkHeader", "OffsetLimit", "PageNumber", "SingleResponse" });

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void AConnectionThatStatesNoStyleIsRefused(string? style)
    {
        var plan = HttpPaging.PlanFor(style, Settings([]), HttpReadWindow.All);

        plan.IsFailure.ShouldBeTrue();
        plan.CurrentMessage!.ShouldContain("states no PagingStyle");
    }

    [Fact]
    public void AStyleNoOptionAnswersToIsRefused()
    {
        var plan = HttpPaging.PlanFor("Cursorish", Settings([]), HttpReadWindow.All);

        plan.IsFailure.ShouldBeTrue();
        plan.CurrentMessage!.ShouldContain("'Cursorish', which is not an HTTP paging style");
    }

    [Theory]
    [InlineData("OffsetLimit", HttpPagingProperties.OffsetParameter)]
    [InlineData("PageNumber", HttpPagingProperties.PageNumberParameter)]
    [InlineData("BodyNextLink", HttpPagingProperties.NextLinkPath)]
    [InlineData("ContinuationToken", HttpPagingProperties.TokenPath)]
    public void AStyleRefusesAConnectionThatLeavesOutAPropertyItNeeds(string style, string missing)
    {
        var plan = HttpPaging.PlanFor(style, Settings([]), HttpReadWindow.All);

        plan.IsFailure.ShouldBeTrue();
        plan.CurrentMessage!.ShouldContain($"needs the additional property '{missing}'");
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("ten")]
    public void APageSizeThatIsNotAPositiveWholeNumberIsRefusedNotReplaced(string size)
    {
        var plan = HttpPaging.PlanFor("OffsetLimit", Settings(
            [(HttpPagingProperties.OffsetParameter, "offset"), (HttpPagingProperties.LimitParameter, "limit"), (HttpPagingProperties.PageSize, size)]), HttpReadWindow.All);

        plan.IsFailure.ShouldBeTrue();
        plan.CurrentMessage!.ShouldContain($"'{size}' for '{HttpPagingProperties.PageSize}'");
    }

    [Fact]
    public void APageSizeTheConnectionLeavesOutIsTheOneTheStyleDeclares()
    {
        var plan = HttpPaging.PlanFor("OffsetLimit", Settings(
            [(HttpPagingProperties.OffsetParameter, "offset"), (HttpPagingProperties.LimitParameter, "limit")]), HttpReadWindow.All);

        plan.IsSuccess.ShouldBeTrue(plan.CurrentMessage);
        ServiceTypeDefaults.TryResolve(HttpPagingStyles.OffsetLimit, HttpPagingProperties.PageSize, out var declared).ShouldBeTrue();
        plan.Value!.PageSize.ShouldBe((int)declared!);
        plan.Value.FirstPage(new Uri("items?filter=open", UriKind.Relative)).OriginalString
            .ShouldBe($"items?filter=open&offset=0&limit={declared}");
    }

    [Fact]
    public void ThePagingKeysAreTheOnesEachStyleExpectsAndNoneIsAnAuthenticationKey()
    {
        foreach (var style in HttpPagingStyles.All())
            style.ExpectedProperties.ShouldAllBe(property => property.StartsWith("Paging", StringComparison.Ordinal));
    }

    [Fact]
    public void AQuerysWindowIsWrittenWithTheNamesTheConnectionStates()
    {
        var window = HttpPaging.WindowFor("OffsetLimit", Settings(
            [(HttpPagingProperties.OffsetParameter, "$skip"), (HttpPagingProperties.LimitParameter, "$top")]), 40, 20);

        window.IsSuccess.ShouldBeTrue(window.CurrentMessage);
        window.Value!.Parameters.ShouldBe(new[] { ("$skip", "40"), ("$top", "20") });
    }

    [Fact]
    public void APageNumberWindowStartsOnAPageBoundary()
    {
        var window = HttpPaging.WindowFor("PageNumber", PageNumberSettings(), 50, 25);

        window.IsSuccess.ShouldBeTrue(window.CurrentMessage);
        window.Value!.Parameters.ShouldBe(new[] { ("page[number]", "3"), ("page[size]", "25") });
    }

    [Fact]
    public void APageNumberWindowThatStartsInsideAPageIsRefusedNotRounded()
    {
        var window = HttpPaging.WindowFor("PageNumber", PageNumberSettings(), 30, 25);

        window.IsFailure.ShouldBeTrue();
        window.CurrentMessage!.ShouldContain("not a whole number of pages of 25");
    }

    [Fact]
    public void APageNumberWindowThatTakesNoRowsIsRefused()
    {
        var window = HttpPaging.WindowFor("PageNumber", PageNumberSettings(), 0, null);

        window.IsFailure.ShouldBeTrue();
        window.CurrentMessage!.ShouldContain("takes no number of rows");
    }

    [Theory]
    [InlineData("LinkHeader")]
    [InlineData("BodyNextLink")]
    [InlineData("ContinuationToken")]
    public void APointerFollowingStyleRefusesAWindowThatSkips(string style)
    {
        var settings = Settings(
            [(HttpPagingProperties.NextLinkPath, "next"), (HttpPagingProperties.TokenPath, "token"), (HttpPagingProperties.TokenParameter, "cursor")]);

        HttpPaging.WindowFor(style, settings, 10, 5).IsFailure.ShouldBeTrue();
        HttpPaging.PlanFor(style, settings, new HttpReadWindow(10, 5)).IsFailure.ShouldBeTrue();
        HttpPaging.WindowFor(style, settings, 0, 5).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void ASingleResponseWindowIsCutByTheReader()
    {
        var window = HttpPaging.WindowFor("SingleResponse", Settings([]), 10, 5);

        window.IsSuccess.ShouldBeTrue(window.CurrentMessage);
        window.Value!.Parameters.ShouldBeEmpty();
        window.Value.SkipHere.ShouldBe(10);
        window.Value.Take.ShouldBe(5);
    }

    [Theory]
    [InlineData("OffsetLimit", -5, 10)]
    [InlineData("PageNumber", -25, 25)]
    [InlineData("SingleResponse", -1, 5)]
    public void AWindowThatSkipsANegativeNumberOfRowsIsRefusedWithItsOwnMessage(string style, int skip, int take)
    {
        var window = HttpPaging.WindowFor(style, SettingsFor(style), skip, take);

        window.IsFailure.ShouldBeTrue();
        window.CurrentMessage!.ShouldContain("cannot skip a negative number of rows");
    }

    [Theory]
    [InlineData("OffsetLimit", 0)]
    [InlineData("OffsetLimit", -1)]
    [InlineData("PageNumber", 0)]
    [InlineData("SingleResponse", 0)]
    public void AWindowThatTakesNoRowsIsRefusedWithItsOwnMessageNotSentAsLimitZero(string style, int take)
    {
        var window = HttpPaging.WindowFor(style, SettingsFor(style), 0, take);

        window.IsFailure.ShouldBeTrue();
        window.CurrentMessage!.ShouldContain($"takes {take} rows, and this read takes at least 1");
    }

    [Fact]
    public void AReadThatSkipsOrTakesANegativeNumberIsRefusedAndOneThatTakesNoneIsNot()
    {
        var settings = SettingsFor("OffsetLimit");

        HttpPaging.PlanFor("OffsetLimit", settings, new HttpReadWindow(-1, 5)).CurrentMessage!.ShouldContain("cannot skip a negative number of rows");
        HttpPaging.PlanFor("OffsetLimit", settings, new HttpReadWindow(0, -1)).CurrentMessage!.ShouldContain("takes -1 rows, and this read takes at least 0");
        HttpPaging.PlanFor("OffsetLimit", settings, new HttpReadWindow(0, 0)).IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData(HttpPagingProperties.FirstPageNumber, "-1")]
    [InlineData(HttpPagingProperties.PageSize, "0")]
    public void APageNumberConnectionThatStatesAPageBeforeZeroOrASizeOfZeroIsRefused(string property, string value)
    {
        var settings = new Dictionary<string, string?>(PageNumberSettings().Values, StringComparer.OrdinalIgnoreCase) { [property] = value };

        var plan = HttpPaging.PlanFor("PageNumber", new HttpPagingSettings("Remote", settings, NullLogger.Instance), HttpReadWindow.All);

        plan.IsFailure.ShouldBeTrue();
        plan.CurrentMessage!.ShouldContain($"'{value}' for '{property}'");
    }

    [Theory]
    [InlineData("BodyNextLink", """{"next": ""}""")]
    [InlineData("BodyNextLink", """{"next": null}""")]
    [InlineData("BodyNextLink", """{}""")]
    [InlineData("ContinuationToken", """{"token": ""}""")]
    [InlineData("ContinuationToken", """{"token": null}""")]
    public void AnEmptyNextLinkOrTokenInTheBodyEndsTheRead(string style, string body)
    {
        var next = PlanOf(style).Next(Outcome(body));

        next.IsSuccess.ShouldBeTrue(next.CurrentMessage);
        next.Value.ShouldBeNull();
    }

    [Theory]
    [InlineData("<>; rel=\"next\"")]
    [InlineData("<https://remote.test/api/items?page=1>; rel=\"prev\"")]
    public void ALinkHeaderWithAnEmptyNextLinkOrNoneEndsTheRead(string header)
    {
        using var response = new HttpResponseMessage();
        response.Headers.TryAddWithoutValidation("Link", header);

        var next = PlanOf("LinkHeader").Next(new HttpPageOutcome(1, new Uri("https://remote.test/api/items"), 7, response.Headers, ReadOnlyMemory<byte>.Empty));

        next.IsSuccess.ShouldBeTrue(next.CurrentMessage);
        next.Value.ShouldBeNull();
    }

    private static HttpPagingSettings SettingsFor(string style) => style switch
    {
        "PageNumber" => PageNumberSettings(),
        "SingleResponse" => Settings([]),
        _ => Settings([(HttpPagingProperties.OffsetParameter, "offset"), (HttpPagingProperties.LimitParameter, "limit")]),
    };

    private static HttpPagingPlan PlanOf(string style)
    {
        var plan = HttpPaging.PlanFor(style, Settings(
            [(HttpPagingProperties.NextLinkPath, "next"), (HttpPagingProperties.TokenPath, "token"), (HttpPagingProperties.TokenParameter, "cursor")]), HttpReadWindow.All);
        plan.IsSuccess.ShouldBeTrue(plan.CurrentMessage);
        return plan.Value!;
    }

    private static HttpPageOutcome Outcome(string body)
    {
        using var response = new HttpResponseMessage();
        return new HttpPageOutcome(1, new Uri("https://remote.test/api/items"), 7, response.Headers, System.Text.Encoding.UTF8.GetBytes(body));
    }

    private static HttpPagingSettings PageNumberSettings() => Settings(
        [(HttpPagingProperties.PageNumberParameter, "page[number]"), (HttpPagingProperties.PageSizeParameter, "page[size]"), (HttpPagingProperties.FirstPageNumber, "1")]);

    private static HttpPagingSettings Settings((string Name, string Value)[] values)
        => new("Remote", values.ToDictionary(v => v.Name, v => (string?)v.Value, StringComparer.OrdinalIgnoreCase), NullLogger.Instance);
}
