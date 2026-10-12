using Fdw.Web.RestEndpoints.ApiReference;

namespace Fdw.Web.RestEndpoints.Tests.ApiReference;

/// <summary>The policies a host configures for an API reference page.</summary>
[Collection(nameof(RestEndpointsTestCollection))]
public sealed class ApiReferencePagePoliciesTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void NothingConfiguredKeepsThePagesOwnPolicies()
    {
        ApiReferencePagePolicies.For(PolicyTestPages.Page, PolicyTestPages.Configuration())
            .ShouldBe([PolicyTestPages.DeclaredPolicy]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void AnIndexedListReplacesThePagesPolicies()
    {
        ApiReferencePagePolicies.For(
                PolicyTestPages.Page,
                PolicyTestPages.Configuration((PolicyTestPages.Key + ":0", "a:read"), (PolicyTestPages.Key + ":1", "b:read")))
            .ShouldBe(["a:read", "b:read"]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ASingleCommaSeparatedValueIsAListToo()
    {
        ApiReferencePagePolicies.For(PolicyTestPages.Page, PolicyTestPages.Configuration((PolicyTestPages.Key, " a:read , b:read,a:READ ")))
            .ShouldBe(["a:read", "b:read"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void APresentButEmptySettingAdmitsAnySignedInCaller()
    {
        var configuration = PolicyTestPages.Configuration((PolicyTestPages.Key, string.Empty));

        ApiReferencePagePolicies.For(PolicyTestPages.Page, configuration).ShouldBeEmpty();
        ApiReferencePagePolicies.Refusal(PolicyTestPages.Page, PolicyTestPages.SignedIn(), configuration).ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void ConfiguredPoliciesAreWhatACallerMustHold()
    {
        var configuration = PolicyTestPages.Configuration((PolicyTestPages.Key + ":0", PolicyTestPages.ConfiguredPolicy));

        ApiReferencePagePolicies.Refusal(PolicyTestPages.Page, PolicyTestPages.SignedIn(PolicyTestPages.DeclaredPolicy), configuration)
            .ShouldNotBeNull();
        ApiReferencePagePolicies.Refusal(PolicyTestPages.Page, PolicyTestPages.SignedIn(PolicyTestPages.ConfiguredPolicy), configuration)
            .ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void ASettingForAnotherPageChangesNothing()
    {
        var configuration = PolicyTestPages.Configuration(("ApiReference:Pages:SomeOtherPage:RequiredPolicies:0", "x:read"));

        ApiReferencePagePolicies.For(PolicyTestPages.Page, configuration).ShouldBe([PolicyTestPages.DeclaredPolicy]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void ConfigurationNeverLetsAnAnonymousCallerIntoASignInPage()
    {
        var configuration = PolicyTestPages.Configuration((PolicyTestPages.Key, string.Empty));

        ApiReferencePagePolicies.Refusal(PolicyTestPages.Page, PolicyTestPages.Anonymous(), configuration).ShouldNotBeNull();
    }
}
