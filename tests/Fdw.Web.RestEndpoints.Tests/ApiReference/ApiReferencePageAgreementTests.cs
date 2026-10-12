using System.Threading.Tasks;
using Fdw.Web.RestEndpoints.ApiReference;
using Microsoft.Extensions.Configuration;

namespace Fdw.Web.RestEndpoints.Tests.ApiReference;

/// <summary>The page list and the page's document endpoint decide from the same policies.</summary>
[Collection(nameof(RestEndpointsTestCollection))]
public sealed class ApiReferencePageAgreementTests
{
    public static TheoryData<string?, string[]> Cases => new()
    {
        { null, [] },
        { null, [PolicyTestPages.DeclaredPolicy] },
        { null, [PolicyTestPages.ConfiguredPolicy] },
        { PolicyTestPages.ConfiguredPolicy, [] },
        { PolicyTestPages.ConfiguredPolicy, [PolicyTestPages.DeclaredPolicy] },
        { PolicyTestPages.ConfiguredPolicy, [PolicyTestPages.ConfiguredPolicy] },
        { string.Empty, [] },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task TheListAndTheDocumentAgree(string? configured, string[] held)
    {
        var configuration = configured is null
            ? PolicyTestPages.Configuration()
            : PolicyTestPages.Configuration((PolicyTestPages.Key, configured));
        var caller = PolicyTestPages.SignedIn(held);

        var links = await PolicyTestPages.Page.Links(caller, PolicyTestPages.Request(configuration), TestContext.Current.CancellationToken);
        var document = new PolicyTestDocumentEndpoint("PolicyTestPage").Check(caller, configuration);

        (links.Value!.Count > 0).ShouldBe(document.IsSuccess);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void ADocumentRefusesACallerTheConfiguredPageRefuses()
    {
        var configuration = PolicyTestPages.Configuration((PolicyTestPages.Key + ":0", PolicyTestPages.ConfiguredPolicy));

        new PolicyTestDocumentEndpoint("PolicyTestPage").Check(PolicyTestPages.SignedIn(PolicyTestPages.DeclaredPolicy), configuration)
            .IsFailure.ShouldBeTrue();
        new PolicyTestDocumentEndpoint("PolicyTestPage").Check(PolicyTestPages.SignedIn(PolicyTestPages.ConfiguredPolicy), configuration)
            .IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void AnAnonymousCallerIsToldToSignInAndASignedInOneIsForbidden()
    {
        var configuration = PolicyTestPages.Configuration();
        var endpoint = new PolicyTestDocumentEndpoint("PolicyTestPage");

        endpoint.Check(PolicyTestPages.Anonymous(), configuration).Code.ShouldNotBeNull().Name.ShouldBe("ReferencePageSignInRequired");
        endpoint.Check(PolicyTestPages.SignedIn(), configuration).Code.ShouldNotBeNull().Name.ShouldBe("ReferencePageNotPermitted");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void ADocumentThatNamesNoPageIsServedAsBefore()
    {
        new PolicyTestDocumentEndpoint(null).Check(PolicyTestPages.Anonymous(), PolicyTestPages.Configuration()).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void ADocumentThatNamesAPageNoOneRegisteredFailsLoud()
    {
        new PolicyTestDocumentEndpoint("NoSuchPage").Check(PolicyTestPages.SignedIn(PolicyTestPages.DeclaredPolicy), PolicyTestPages.Configuration())
            .IsFailure.ShouldBeTrue();
    }
}
