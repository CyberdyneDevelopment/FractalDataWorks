using System;
using Fdw.Services.Authentication.Abstractions.Steps;
using Shouldly;
using Xunit;

namespace Fdw.Services.Authentication.Tests.Flow;

/// <summary>
/// A flow's return URLs match the page a caller names whether or not either side ends in a slash:
/// the Scalar sign-in panel sends /api/v1/scalar/ while a flow may list /api/v1/scalar.
/// </summary>
public sealed class AuthenticationFlowReturnUrlTests
{
    private static AuthenticationFlow Flow(params string[] returnUrls) => new()
    {
        Name = "EntraReference",
        Steps = FlowSteps.Named("OidcRedirect"),
        RequiredPermissions = [],
        Audience = "api",
        ExecutionLifetime = TimeSpan.FromMinutes(5),
        ReturnUrls = returnUrls,
    };

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    [InlineData("https://api.test/api/v1/scalar", "https://api.test/api/v1/scalar")]
    [InlineData("https://api.test/api/v1/scalar", "https://api.test/api/v1/scalar/")]
    [InlineData("https://api.test/api/v1/scalar/", "https://api.test/api/v1/scalar")]
    [InlineData("https://api.test/api/v1/scalar/", "https://api.test/api/v1/scalar/")]
    public void ATrailingSlashIsNotSignificantOnEitherSide(string listed, string asked)
        => Flow(listed).AllowsReturnUrl(asked).ShouldBeTrue();

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void AnotherPageIsNotAllowed()
        => Flow("https://api.test/api/v1/scalar").AllowsReturnUrl("https://api.test/api/v1/scalar/other").ShouldBeFalse();

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    [InlineData("https://api.test/api/v1/scalar//")]
    [InlineData("https://api.test/api/v1/scalar?next=/other")]
    [InlineData("https://api.test/api/v1/scalar#top")]
    [InlineData("https://other.test/api/v1/scalar")]
    public void OnlyASingleTrailingSlashIsLoose(string asked)
        => Flow("https://api.test/api/v1/scalar").AllowsReturnUrl(asked).ShouldBeFalse();

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void AFlowKeepsTheReturnUrlsExactlyAsListed()
        => Flow("https://api.test/api/v1/scalar/").ReturnUrls.ShouldBe(["https://api.test/api/v1/scalar/"]);

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void AFlowThatReturnsNoBrowserAllowsNothing()
        => new AuthenticationFlow
        {
            Name = "PasswordLogin",
            Steps = FlowSteps.Named("PasswordCredential"),
            RequiredPermissions = [],
            Audience = "api",
            ExecutionLifetime = TimeSpan.FromMinutes(5),
        }.AllowsReturnUrl("https://api.test/api/v1/scalar").ShouldBeFalse();
}
