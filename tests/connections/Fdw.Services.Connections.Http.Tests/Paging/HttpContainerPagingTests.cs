using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Connections.Http.Paging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Connections.Http.Tests.Paging;

/// <summary>
/// FDW-5562: a container of an HTTP connection can page differently from its connection. Its own style and settings are laid over the connection's in one
/// place, a style the container does not share with its connection is never given the connection's style-specific keys, and a setting its style does not read,
/// a missing or bad value, or settings with no style are refused naming the key.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class HttpContainerPagingTests
{
    private static Dictionary<string, string?> Values(params (string Name, string Value)[] entries)
    {
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, value) in entries)
            values[name] = value;

        return values;
    }

    private static HttpConnectionConfiguration Connection(string? style, params (string Name, string Value)[] entries)
    {
        var connection = new HttpConnectionConfiguration { Name = "remote", PagingStyle = style };
        foreach (var (name, value) in entries)
            connection.AdditionalProperties[name] = value;

        return connection;
    }

    [Fact]
    public void AContainerThatStatesNothingTakesItsConnectionsStyleAndValues()
    {
        var resolved = HttpPagingSettings.For(
            "remote", "OffsetLimit", Values((HttpPagingProperties.OffsetParameter, "skip"), (HttpPagingProperties.LimitParameter, "top"), (HttpPagingProperties.PageSize, "50")),
            "orders", null, Values(), NullLogger.Instance);

        resolved.IsSuccess.ShouldBeTrue(resolved.CurrentMessage);
        resolved.Value!.Style.Name.ShouldBe("OffsetLimit");
        resolved.Value.StyleSource.ShouldBe(HttpPagingSources.Connection);
        resolved.Value.Settings.Values[HttpPagingProperties.PageSize].ShouldBe("50");
        resolved.Value.Sources[HttpPagingProperties.PageSize].ShouldBe(HttpPagingSources.Connection);
        resolved.Value.Settings.ConnectionName.ShouldBe("remote/orders");
    }

    [Fact]
    public void AContainerThatPagesInTheConnectionsStyleOverridesOnlyWhatItStates()
    {
        var resolved = HttpPagingSettings.For(
            "remote", "OffsetLimit", Values((HttpPagingProperties.OffsetParameter, "skip"), (HttpPagingProperties.LimitParameter, "top"), (HttpPagingProperties.PageSize, "50")),
            "orders", "OffsetLimit", Values((HttpPagingProperties.PageSize, "10")), NullLogger.Instance);

        resolved.IsSuccess.ShouldBeTrue(resolved.CurrentMessage);
        resolved.Value!.StyleSource.ShouldBe(HttpPagingSources.Container);
        resolved.Value.Settings.Values[HttpPagingProperties.PageSize].ShouldBe("10");
        resolved.Value.Sources[HttpPagingProperties.PageSize].ShouldBe(HttpPagingSources.Container);
        resolved.Value.Settings.Values[HttpPagingProperties.OffsetParameter].ShouldBe("skip");
        resolved.Value.Sources[HttpPagingProperties.OffsetParameter].ShouldBe(HttpPagingSources.Connection);
    }

    [Fact]
    public void AContainerInAnotherStyleTakesOnlyTheTuningValuesFromItsConnection()
    {
        var resolved = HttpPagingSettings.For(
            "remote", "LinkHeader", Values((HttpPagingProperties.PageSizeParameter, "per_page"), (HttpPagingProperties.PageSize, "50"), (HttpPagingProperties.MaxPages, "7")),
            "orders", "PageNumber", Values((HttpPagingProperties.PageNumberParameter, "page"), (HttpPagingProperties.FirstPageNumber, "1")), NullLogger.Instance);

        resolved.IsSuccess.ShouldBeTrue(resolved.CurrentMessage);
        resolved.Value!.Settings.Values[HttpPagingProperties.PageSize].ShouldBe("50");
        resolved.Value.Settings.Values[HttpPagingProperties.MaxPages].ShouldBe("7");
        resolved.Value.Settings.Values.ContainsKey(HttpPagingProperties.PageSizeParameter).ShouldBeFalse();

        // PageNumber needs the parameter that carries the page size and the container did not state it: the connection's belongs to its own style.
        var checkedSettings = resolved.Value.Style.Check(resolved.Value.Settings);
        checkedSettings.IsFailure.ShouldBeTrue();
        checkedSettings.CurrentMessage!.ShouldContain(HttpPagingProperties.PageSizeParameter);
    }

    [Fact]
    public void ASettingItsStyleDoesNotReadIsRefusedNamingIt()
    {
        var resolved = HttpPagingSettings.For(
            "remote", "OffsetLimit", Values(), "orders", "SingleResponse", Values((HttpPagingProperties.PageSize, "5")), NullLogger.Instance);

        resolved.IsFailure.ShouldBeTrue();
        var message = resolved.CurrentMessage.ShouldNotBeNull();
        message.ShouldContain(HttpPagingProperties.PageSize);
        message.ShouldContain("SingleResponse");
    }

    [Fact]
    public void NoStyleAtEitherLevelIsRefused()
    {
        var resolved = HttpPagingSettings.For("remote", null, Values(), "orders", null, Values(), NullLogger.Instance);

        resolved.IsFailure.ShouldBeTrue();
        resolved.CurrentMessage!.ShouldContain("states no PagingStyle");
    }

    [Fact]
    public void AContainerStyleNoOptionAnswersToIsRefused()
    {
        var resolved = HttpPagingSettings.For("remote", "OffsetLimit", Values(), "orders", "Cursorish", Values(), NullLogger.Instance);

        resolved.IsFailure.ShouldBeTrue();
        resolved.CurrentMessage!.ShouldContain("'Cursorish', which is not an HTTP paging style");
    }

    [Theory]
    [InlineData("0")]
    [InlineData("ten")]
    public void AStatedNumberThatIsNotAWholeNumberInRangeIsRefusedWhenChecked(string size)
    {
        var resolved = HttpPagingSettings.For(
            "remote", "OffsetLimit", Values((HttpPagingProperties.OffsetParameter, "skip"), (HttpPagingProperties.LimitParameter, "top")),
            "orders", "OffsetLimit", Values((HttpPagingProperties.PageSize, size)), NullLogger.Instance);

        resolved.IsSuccess.ShouldBeTrue(resolved.CurrentMessage);
        var checkedSettings = resolved.Value!.Style.Check(resolved.Value.Settings);

        checkedSettings.IsFailure.ShouldBeTrue();
        checkedSettings.CurrentMessage!.ShouldContain("whole number");
    }

    [Fact]
    public void TheRulesRefuseSettingsThatNameNoStyle()
    {
        var checkedPaging = HttpContainerPagingRules.Instance.Check(
            Connection("OffsetLimit"), "orders", null, Values((HttpPagingProperties.PageSize, "5")), NullLogger.Instance);

        checkedPaging.IsFailure.ShouldBeTrue();
        checkedPaging.CurrentMessage!.ShouldContain("no paging style");
    }

    [Fact]
    public void TheRulesAskNothingOfAContainerThatStatesNoPaging()
    {
        HttpContainerPagingRules.Instance.Check(Connection(null), "orders", null, Values(), NullLogger.Instance).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void TheRulesReportEachValueWithWhereItCameFrom()
    {
        var connection = Connection("OffsetLimit", (HttpPagingProperties.OffsetParameter, "skip"), (HttpPagingProperties.LimitParameter, "top"), (HttpPagingProperties.PageSize, "50"));

        var view = HttpContainerPagingRules.Instance.Resolve(
            connection, "orders", "OffsetLimit", Values((HttpPagingProperties.LimitParameter, "count")), NullLogger.Instance);

        view.IsSuccess.ShouldBeTrue(view.CurrentMessage);
        view.Value!.Style.ShouldBe("OffsetLimit");
        view.Value.StyleSource.ShouldBe(HttpPagingSources.Container);
        view.Value.Settings.Single(value => value.Name == HttpPagingProperties.LimitParameter).Source.ShouldBe(HttpPagingSources.Container);
        view.Value.Settings.Single(value => value.Name == HttpPagingProperties.OffsetParameter).Source.ShouldBe(HttpPagingSources.Connection);
        view.Value.Settings.Single(value => value.Name == HttpPagingProperties.PageSize).Value.ShouldBe("50");

        // The page count is stated by neither, so it is the style's declared default.
        view.Value.Settings.Single(value => value.Name == HttpPagingProperties.MaxPages).Source.ShouldBe(HttpPagingSources.Style);
    }

    [Fact]
    public void TheRulesReportNothingForAConnectionThatStatesNoStyleAndAContainerThatStatesNone()
    {
        var view = HttpContainerPagingRules.Instance.Resolve(Connection(null), "orders", null, Values(), NullLogger.Instance);

        view.IsSuccess.ShouldBeTrue(view.CurrentMessage);
        view.Value.ShouldBeNull();
    }

    [Fact]
    public void EachStyleDeclaresWhichOfItsPropertiesAContainerTakesFromAConnectionInAnotherStyle()
    {
        HttpPagingStyles.ByName("PageNumber").InheritableProperties.OrderBy(name => name, StringComparer.Ordinal)
            .ShouldBe(new[] { HttpPagingProperties.MaxPages, HttpPagingProperties.PageSize });
        HttpPagingStyles.ByName("SingleResponse").InheritableProperties.ShouldBeEmpty();
    }
}
