using System;
using System.Security.Claims;
using Fdw.Services.Settings;
using Fdw.Web.RestEndpoints.Pagination;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Web.RestEndpoints.Tests.Pagination;

/// <summary>
/// Every list endpoint reads its page through <see cref="PaginationQuery"/>, so what it parses, how it
/// sizes the page from settings and what it refuses is what every list endpoint does.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "Api")]
public sealed class PaginationQueryTests
{
    private static Mock<IEffectiveSettingsProvider> Settings(int defaultSize, int maxSize)
    {
        var settings = new Mock<IEffectiveSettingsProvider>();
        settings
            .Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.DefaultPaginationSize, It.IsAny<Guid?>(), It.IsAny<string?>()))
            .Returns(defaultSize);
        settings
            .Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.MaxPaginationSize, It.IsAny<Guid?>(), It.IsAny<string?>()))
            .Returns(maxSize);
        return settings;
    }

    private static DefaultHttpContext Context(string query, IEffectiveSettingsProvider? settings)
    {
        var services = new Mock<IServiceProvider>();
        services.Setup(p => p.GetService(typeof(IEffectiveSettingsProvider))).Returns(settings!);
        var context = new DefaultHttpContext { RequestServices = services.Object };
        context.Request.QueryString = new QueryString(query);
        return context;
    }

    private static DefaultHttpContext Context(string query, int defaultSize = 25, int maxSize = 1000)
        => Context(query, Settings(defaultSize, maxSize).Object);

    [Fact]
    public void NoQueryMeansTheFirstPageOfTheDefaultPaginationSize()
    {
        var paging = PaginationQuery.Parse(Context("", defaultSize: 25), NullLogger.Instance);

        paging.IsSuccess.ShouldBeTrue();
        paging.Value!.Skip.ShouldBe(0);
        paging.Value.Take.ShouldBe(25);
    }

    [Fact]
    public void SkipAndTakeAreRead()
    {
        var paging = PaginationQuery.Parse(Context("?skip=20&take=10"), NullLogger.Instance);

        paging.Value!.Skip.ShouldBe(20);
        paging.Value.Take.ShouldBe(10);
    }

    // Why these are refused rather than replaced: they used to be swapped for the default page, so a
    // caller's mistake came back as a page it never asked for.
    [Theory]
    [InlineData("?skip=-5")]
    [InlineData("?skip=abc")]
    [InlineData("?take=0")]
    [InlineData("?take=xyz")]
    public void InvalidValuesAreRefused(string query)
    {
        var paging = PaginationQuery.Parse(Context(query), NullLogger.Instance);

        paging.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void TheMaxPaginationSizeSettingCapsTheTake()
    {
        var paging = PaginationQuery.Parse(Context("?take=500", maxSize: 50), NullLogger.Instance);

        paging.Value!.Take.ShouldBe(50);
    }

    [Fact]
    public void TheMaxPaginationSizeSettingCapsTheDefaultToo()
    {
        var paging = PaginationQuery.Parse(Context("", defaultSize: 100, maxSize: 20), NullLogger.Instance);

        paging.Value!.Take.ShouldBe(20);
    }

    [Fact]
    public void AMaxAboveTheOldBuiltInCeilingIsHonoured()
    {
        var paging = PaginationQuery.Parse(Context("?take=5000", maxSize: 5000), NullLogger.Instance);

        paging.Value!.Take.ShouldBe(5000);
    }

    // Why a missing setting fails instead of falling back: the fallbacks were constants nothing could
    // override, and they hid that the settings were never read at all.
    [Theory]
    [InlineData(0, 1000)]
    [InlineData(25, 0)]
    public void AMissingPaginationSettingFailsTheRequest(int defaultSize, int maxSize)
    {
        var paging = PaginationQuery.Parse(Context("", defaultSize, maxSize), NullLogger.Instance);

        paging.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void NoSettingsProviderFailsTheRequest()
    {
        var paging = PaginationQuery.Parse(Context("", settings: null), NullLogger.Instance);

        paging.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void TheSettingsAreResolvedForTheCallersTenantAndRole()
    {
        var tenantId = Guid.NewGuid();
        var settings = Settings(25, 1000);
        var context = Context("?take=500", settings.Object);
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("tenantId", tenantId.ToString()),
            new Claim(ClaimTypes.Role, "Admin"),
        ], "test"));

        PaginationQuery.Parse(context, NullLogger.Instance);

        settings.Verify(s => s.GetEffectiveValue<int>(SettingDefinitions.MaxPaginationSize, tenantId, "Admin"), Times.Once);
        settings.Verify(s => s.GetEffectiveValue<int>(SettingDefinitions.DefaultPaginationSize, tenantId, "Admin"), Times.Once);
    }

    [Fact]
    public void APageNumberBecomesASkipOfWholePages()
    {
        var paging = PaginationQuery.ResolvePage(Context(""), NullLogger.Instance, page: 3, requestedPageSize: 10);

        paging.Value!.Skip.ShouldBe(20);
        paging.Value.Take.ShouldBe(10);
    }

    [Fact]
    public void NoPageSizeMeansTheDefaultPaginationSize()
    {
        var paging = PaginationQuery.ResolvePage(Context("", defaultSize: 40), NullLogger.Instance, page: 2, requestedPageSize: null);

        paging.Value!.Skip.ShouldBe(40);
        paging.Value.Take.ShouldBe(40);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    public void APageOrPageSizeBelowOneIsRefused(int page, int pageSize)
    {
        var paging = PaginationQuery.ResolvePage(Context(""), NullLogger.Instance, page, pageSize);

        paging.IsFailure.ShouldBeTrue();
    }

    // --- The most rows a data query may read to filter or sort on a calculated field (FDW-1045) ---

    private static Mock<IEffectiveSettingsProvider> RowCapSettings(int cap)
    {
        var settings = new Mock<IEffectiveSettingsProvider>();
        settings
            .Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.MaxCalculatedFilterRows, It.IsAny<Guid?>(), It.IsAny<string?>()))
            .Returns(cap);
        return settings;
    }

    [Fact]
    public void TheCalculatedRowCapIsReadFromTheSettings()
    {
        var cap = PaginationQuery.ResolveCalculatedRowCap(Context("", RowCapSettings(250).Object), NullLogger.Instance);

        cap.IsSuccess.ShouldBeTrue();
        cap.Value.ShouldBe(250);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void ACalculatedRowCapThatResolvesToNothingPositiveIsRefused(int resolved)
        => PaginationQuery.ResolveCalculatedRowCap(Context("", RowCapSettings(resolved).Object), NullLogger.Instance).IsFailure.ShouldBeTrue();

    [Fact]
    public void WithNoSettingsProviderTheCalculatedRowCapIsRefused()
        => PaginationQuery.ResolveCalculatedRowCap(Context("", (IEffectiveSettingsProvider?)null), NullLogger.Instance).IsFailure.ShouldBeTrue();
}
