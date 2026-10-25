using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Settings;
using Fdw.Services.Settings.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Settings.Tests;

/// <summary>
/// Tests for the home pages a scope holds (FDW-2325): a draft is kept apart from the published page, publishing needs a draft, a body that
/// is not a page is refused, and the page that applies to a person comes from their own scope, else their organization's, else their tenant's.
/// </summary>
[Trait("Category", "Configuration")]
public sealed class HomePagesTests
{
    private const string Page = """{"version":1,"cards":[]}""";
    private const string OtherPage = """{"version":1,"cards":[{"kind":"links"}]}""";

    private static readonly Guid Tenant = new("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid Finance = new("44444444-0000-0000-0000-000000000001");
    private static readonly Guid Legal = new("44444444-0000-0000-0000-000000000002");
    private static readonly Guid Person = new("55555555-0000-0000-0000-000000000001");

    private readonly Dictionary<string, HomePageImplementationConfiguration> _rows = new(StringComparer.Ordinal);
    private readonly Mock<IHomePageConfigurationProvider> _provider = new();
    private readonly HomePages _pages;

    public HomePagesTests()
    {
        _provider.Setup(p => p.Get(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((string name, CancellationToken _) =>
            _rows.TryGetValue(name, out var row)
                ? GenericResult<IHomePageImplementationConfiguration>.Success(row)
                : GenericResult<IHomePageImplementationConfiguration>.Success(null!));
        _provider.Setup(p => p.Save(It.IsAny<IHomePageImplementationConfiguration>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((IHomePageImplementationConfiguration row, string _, string _, string name, CancellationToken _) => _rows[name] = (HomePageImplementationConfiguration)row)
            .ReturnsAsync(GenericResult.Success());
        _pages = new HomePages(_provider.Object, NullLogger<HomePages>.Instance);
    }

    private static HomePageScope OrganizationScope(Guid organization) => new(HomePageKinds.Organization, organization, Tenant);

    private static HomePageScope PersonScope => new(HomePageKinds.Person, Person, Tenant);

    private static HomePageScope TenantScope => new(HomePageKinds.Tenant, Tenant, Tenant);

    private async Task Publish(HomePageScope scope, string body)
    {
        (await _pages.SaveDraft(scope, body, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
        (await _pages.Publish(scope, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task AScopeThatHoldsNothingAnswersBothPagesNull()
    {
        var set = await _pages.Get(OrganizationScope(Finance), TestContext.Current.CancellationToken);

        set.IsSuccess.ShouldBeTrue();
        var held = set.Value.ShouldNotBeNull();
        held.Published.ShouldBeNull();
        held.Draft.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task ASavedDraftIsKeptApartFromThePublishedPage()
    {
        await Publish(OrganizationScope(Finance), Page);

        (await _pages.SaveDraft(OrganizationScope(Finance), OtherPage, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();

        var set = await _pages.Get(OrganizationScope(Finance), TestContext.Current.CancellationToken);
        var held = set.Value.ShouldNotBeNull();
        held.Published.ShouldBe(Page);
        held.Draft.ShouldBe(OtherPage);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task PublishingMakesTheDraftThePublishedPage()
    {
        await Publish(OrganizationScope(Finance), OtherPage);

        var set = await _pages.Get(OrganizationScope(Finance), TestContext.Current.CancellationToken);
        var held = set.Value.ShouldNotBeNull();
        held.Published.ShouldBe(OtherPage);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task PublishingWithNoDraftIsRefusedAndPublishesNothing()
    {
        var published = await _pages.Publish(OrganizationScope(Finance), TestContext.Current.CancellationToken);

        published.IsFailure.ShouldBeTrue();
        _rows.ShouldBeEmpty();
    }

    [Theory]
    [Trait("Priority", "P1")]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("""{"cards":[]}""")]
    [InlineData("""{"version":2,"cards":[]}""")]
    [InlineData("""{"version":1}""")]
    [InlineData("""{"version":1,"cards":{}}""")]
    public async Task ABodyThatIsNotAPageIsRefusedAndNothingIsSaved(string body)
    {
        var saved = await _pages.SaveDraft(OrganizationScope(Finance), body, TestContext.Current.CancellationToken);

        saved.IsFailure.ShouldBeTrue();
        _rows.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task ACardTheServiceDoesNotKnowIsStoredAsWritten()
    {
        const string body = """{"version":1,"cards":[{"kind":"somethingNew","options":{"a":1}}]}""";

        await Publish(OrganizationScope(Finance), body);

        (await _pages.Get(OrganizationScope(Finance), TestContext.Current.CancellationToken)).Value.ShouldNotBeNull().Published.ShouldBe(body);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task APersonsOwnPageBeatsTheirOrganizationsAndTheirTenants()
    {
        await Publish(TenantScope, Page);
        await Publish(OrganizationScope(Finance), Page);
        await Publish(PersonScope, OtherPage);

        var applied = await _pages.Applying(Person, Tenant, Finance, TestContext.Current.CancellationToken);

        var page = applied.Value.ShouldNotBeNull();
        page.SourceKind.ShouldBe(HomePageKinds.Person);
        page.SourceId.ShouldBe(Person);
        page.Body.ShouldBe(OtherPage);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task WithNoPersonalPageTheOrganizationsPageBeatsTheTenants()
    {
        await Publish(TenantScope, Page);
        await Publish(OrganizationScope(Finance), OtherPage);

        var applied = await _pages.Applying(Person, Tenant, Finance, TestContext.Current.CancellationToken);

        var page = applied.Value.ShouldNotBeNull();
        page.SourceKind.ShouldBe(HomePageKinds.Organization);
        page.SourceId.ShouldBe(Finance);
        page.Body.ShouldBe(OtherPage);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task WithNeitherThePageOfTheTenantApplies()
    {
        await Publish(TenantScope, Page);

        var applied = await _pages.Applying(Person, Tenant, Finance, TestContext.Current.CancellationToken);

        var page = applied.Value.ShouldNotBeNull();
        page.SourceKind.ShouldBe(HomePageKinds.Tenant);
        page.SourceId.ShouldBe(Tenant);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task AnotherOrganizationsPageDoesNotApply()
    {
        await Publish(OrganizationScope(Legal), OtherPage);

        var applied = await _pages.Applying(Person, Tenant, Finance, TestContext.Current.CancellationToken);

        var page = applied.Value.ShouldNotBeNull();
        page.SourceKind.ShouldBe(HomePageKinds.None);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task APersonActingInNoOrganizationSkipsTheOrganizationsPage()
    {
        await Publish(OrganizationScope(Finance), OtherPage);
        await Publish(TenantScope, Page);

        var applied = await _pages.Applying(Person, Tenant, null, TestContext.Current.CancellationToken);

        var page = applied.Value.ShouldNotBeNull();
        page.SourceKind.ShouldBe(HomePageKinds.Tenant);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task ADraftThatIsNotPublishedDoesNotApply()
    {
        (await _pages.SaveDraft(TenantScope, Page, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();

        var applied = await _pages.Applying(Person, Tenant, Finance, TestContext.Current.CancellationToken);

        var page = applied.Value.ShouldNotBeNull();
        page.SourceKind.ShouldBe(HomePageKinds.None);
        page.SourceId.ShouldBeNull();
        page.Body.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task WhenNoScopeHoldsAPublishedPageNoneApplies()
    {
        var applied = await _pages.Applying(Person, Tenant, Finance, TestContext.Current.CancellationToken);

        applied.IsSuccess.ShouldBeTrue();
        var page = applied.Value.ShouldNotBeNull();
        page.SourceKind.ShouldBe(HomePageKinds.None);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task AFailedReadIsReturnedAndNoLaterScopeIsTried()
    {
        _provider.Setup(p => p.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IHomePageImplementationConfiguration>.Failure(new GenericMessage("the store is down")));

        var applied = await _pages.Applying(Person, Tenant, Finance, TestContext.Current.CancellationToken);

        applied.IsFailure.ShouldBeTrue();
        _provider.Verify(p => p.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
