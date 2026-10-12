using System;
using System.Security.Claims;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Settings;
using Fdw.Web.RestEndpoints.Models;
using Fdw.Web.RestEndpoints.Pagination;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Web.RestEndpoints.Tests.Pagination;

/// <summary>
/// A caller an API key authenticated is a service running unattended, so the limits that protect an interactive caller do
/// not apply to it unless the ApiKey paging settings set one (FDW-1209). 0 means no limit, and an absent setting is refused,
/// never read as 0.
/// </summary>
[Trait("Priority", "P0")]
[Trait("Category", "Api")]
public sealed class PaginationQueryApiKeyTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    // Declared for every caller, or for exactly one tenant and role: a later setup wins, so an override is declared after the server value.
    private static void Declare(Mock<IEffectiveSettingsProvider> settings, string name, int value, Guid? tenantId = null, string? roleName = null)
    {
        var declared = value;
        if (tenantId is null && roleName is null)
        {
            settings
                .Setup(s => s.TryGetEffectiveValue<int>(name, out declared, It.IsAny<Guid?>(), It.IsAny<string?>()))
                .Returns(true);
            return;
        }

        settings
            .Setup(s => s.TryGetEffectiveValue<int>(name, out declared, tenantId, roleName))
            .Returns(true);
    }

    // The interactive settings are declared for every test: the caller either never reads the ApiKey ones, or never reads these.
    private static Mock<IEffectiveSettingsProvider> Settings(int? apiKeyDefault, int? apiKeyMax)
    {
        var settings = new Mock<IEffectiveSettingsProvider>();
        settings
            .Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.DefaultPaginationSize, It.IsAny<Guid?>(), It.IsAny<string?>()))
            .Returns(100);
        settings
            .Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.MaxPaginationSize, It.IsAny<Guid?>(), It.IsAny<string?>()))
            .Returns(1000);
        if (apiKeyDefault is { } defaultSize)
            Declare(settings, SettingDefinitions.ApiKeyDefaultPaginationSize, defaultSize);
        if (apiKeyMax is { } maxSize)
            Declare(settings, SettingDefinitions.ApiKeyMaxPaginationSize, maxSize);
        return settings;
    }

    // The principal the ApiKey handler builds: identity named after the scheme, the tenant, and each role under both claim types.
    private static ClaimsPrincipal ApiKey(string? role = null)
    {
        var claims = new System.Collections.Generic.List<Claim> { new("tenantId", TenantId.ToString()) };
        if (role is not null)
        {
            claims.Add(new Claim("roles", role));
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, ApiKeyAuthenticationSchemes.Prefix + "Keys"));
    }

    private static ClaimsPrincipal SignedIn()
        => new(new ClaimsIdentity([new Claim("tenantId", TenantId.ToString()), new Claim("agent", "true")], "Bearer"));

    private static DefaultHttpContext Context(string query, IEffectiveSettingsProvider settings, ClaimsPrincipal user)
    {
        var services = new Mock<IServiceProvider>();
        services.Setup(p => p.GetService(typeof(IEffectiveSettingsProvider))).Returns(settings);
        var context = new DefaultHttpContext { RequestServices = services.Object, User = user };
        context.Request.QueryString = new QueryString(query);
        return context;
    }

    [Fact]
    public void AnApiKeyWithNoTakeAndNoLimitIsUnlimited()
    {
        var paging = PaginationQuery.Parse(Context("", Settings(0, 0).Object, ApiKey()), NullLogger.Instance);

        paging.IsSuccess.ShouldBeTrue();
        paging.Value!.IsUnlimited.ShouldBeTrue();
        paging.Value.Take.ShouldBe(PaginatedListRequest.NoLimitTake);
        paging.Value.Take.ShouldBeGreaterThan(100);
    }

    [Fact]
    public void AnApiKeyWithATakeOf5000AndNoLimitGetsExactlyThat()
    {
        var paging = PaginationQuery.Parse(Context("?take=5000", Settings(0, 0).Object, ApiKey()), NullLogger.Instance);

        paging.Value!.Take.ShouldBe(5000);
        paging.Value.IsUnlimited.ShouldBeFalse();
    }

    [Fact]
    public void ASignedInUserKeepsTheDefaultPageAndTheCap()
    {
        var settings = Settings(0, 0).Object;

        var byDefault = PaginationQuery.Parse(Context("", settings, SignedIn()), NullLogger.Instance);
        var capped = PaginationQuery.Parse(Context("?take=5000", settings, SignedIn()), NullLogger.Instance);

        byDefault.Value!.Take.ShouldBe(100);
        byDefault.Value.IsUnlimited.ShouldBeFalse();
        capped.Value!.Take.ShouldBe(1000);
    }

    // Why the agent claim is not the marker: it is on agent keys only, and a person signed in can carry any claim a token holds.
    [Fact]
    public void OnlyTheSchemeThatAuthenticatedTheCallerMakesItAnApiKey()
    {
        ApiKeyAuthenticationSchemes.Authenticated(SignedIn()).ShouldBeFalse();
        ApiKeyAuthenticationSchemes.Authenticated(ApiKey()).ShouldBeTrue();
        ApiKeyAuthenticationSchemes.Authenticated(new ClaimsPrincipal(new ClaimsIdentity())).ShouldBeFalse();
    }

    [Fact]
    public void ANonZeroApiKeyDefaultAndMaxStillBoundTheRequest()
    {
        var settings = Settings(50, 200).Object;

        var byDefault = PaginationQuery.Parse(Context("", settings, ApiKey()), NullLogger.Instance);
        var capped = PaginationQuery.Parse(Context("?take=900", settings, ApiKey()), NullLogger.Instance);

        byDefault.Value!.Take.ShouldBe(50);
        byDefault.Value.IsUnlimited.ShouldBeFalse();
        capped.Value!.Take.ShouldBe(200);
    }

    [Fact]
    public void ANoLimitDefaultUnderANonZeroMaxIsCutAtTheMax()
    {
        var paging = PaginationQuery.Parse(Context("", Settings(0, 200).Object, ApiKey()), NullLogger.Instance);

        paging.Value!.Take.ShouldBe(200);
        paging.Value.IsUnlimited.ShouldBeFalse();
    }

    [Fact]
    public void ATenantOverrideOfTheApiKeySettingsIsHonoured()
    {
        var settings = Settings(0, 0);
        Declare(settings, SettingDefinitions.ApiKeyMaxPaginationSize, 50, TenantId);

        var paging = PaginationQuery.Parse(Context("?take=500", settings.Object, ApiKey()), NullLogger.Instance);

        paging.Value!.Take.ShouldBe(50);
    }

    [Fact]
    public void ARoleOverrideOfTheApiKeySettingsIsHonoured()
    {
        var settings = Settings(0, 0);
        Declare(settings, SettingDefinitions.ApiKeyMaxPaginationSize, 20, TenantId, "Auditor");

        var paging = PaginationQuery.Parse(Context("?take=500", settings.Object, ApiKey("Auditor")), NullLogger.Instance);

        paging.Value!.Take.ShouldBe(20);
    }

    [Fact]
    public void TheApiKeySettingsAreResolvedForTheCallersTenantAndRole()
    {
        var settings = Settings(0, 0);
        int unused;

        PaginationQuery.Parse(Context("", settings.Object, ApiKey("Auditor")), NullLogger.Instance);

        settings.Verify(s => s.TryGetEffectiveValue<int>(SettingDefinitions.ApiKeyMaxPaginationSize, out unused, TenantId, "Auditor"), Times.Once);
        settings.Verify(s => s.TryGetEffectiveValue<int>(SettingDefinitions.ApiKeyDefaultPaginationSize, out unused, TenantId, "Auditor"), Times.Once);
    }

    // Why an absent setting fails instead of reading as 0: 0 is how an operator says no limit, so a setting nobody declared
    // must not be able to say it for them.
    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void AnAbsentApiKeySettingFailsTheRequestInsteadOfBeingUnlimited(bool maxDeclared, bool defaultDeclared)
    {
        var settings = Settings(defaultDeclared ? 0 : null, maxDeclared ? 0 : null);

        var paging = PaginationQuery.Parse(Context("", settings.Object, ApiKey()), NullLogger.Instance);

        paging.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void ANegativeApiKeySettingFailsTheRequest()
    {
        var paging = PaginationQuery.Parse(Context("", Settings(0, -1).Object, ApiKey()), NullLogger.Instance);

        paging.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void TheApiKeySettingsAreNotNeededByASignedInUser()
    {
        var paging = PaginationQuery.Parse(Context("", Settings(null, null).Object, SignedIn()), NullLogger.Instance);

        paging.IsSuccess.ShouldBeTrue();
        paging.Value!.Take.ShouldBe(100);
    }

    [Fact]
    public void AnUnlimitedApiKeyIsTheWholeListAsPageOne()
    {
        var context = Context("", Settings(0, 0).Object, ApiKey());

        var first = PaginationQuery.ResolvePage(context, NullLogger.Instance, page: 1, requestedPageSize: null);
        var second = PaginationQuery.ResolvePage(context, NullLogger.Instance, page: 2, requestedPageSize: null);

        first.Value!.IsUnlimited.ShouldBeTrue();
        first.Value.Skip.ShouldBe(0);
        second.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void TheBatchSizeOfAnUnlimitedReadIsTheDefaultPaginationSize()
    {
        var batch = PaginationQuery.ResolveBatchSize(Context("", Settings(0, 0).Object, ApiKey()), NullLogger.Instance);

        batch.Value.ShouldBe(100);
    }
}
