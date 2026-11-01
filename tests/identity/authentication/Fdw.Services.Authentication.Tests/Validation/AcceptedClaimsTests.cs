using System.Linq;
using Fdw.Claims.Abstractions;
using Fdw.Services.Authentication.Validation;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Authentication.Tests.Validation;

/// <summary>
/// FDW-2933: a JwtBearer entry states which platform claims it accepts from its issuer. An unstated column is refused, a claim the platform
/// does not define is refused, and a session-bound claim can never be accepted from another issuer.
/// </summary>
public sealed class AcceptedClaimsTests
{
    private const string Entry = "scheduler";

    [Fact]
    [Trait("Priority", "P0")]
    public void AnUnstatedColumnIsRefusedNamingTheEntry()
    {
        var read = AcceptedClaims.Read(string.Empty, Entry, NullLogger.Instance);

        read.IsFailure.ShouldBeTrue();
        read.Messages.Single().Message.ShouldContain(Entry);
        read.Messages.Single().Message.ShouldContain("AcceptedClaims");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void TheWordNoneStatesAnIssuerTrustedForNoPlatformClaim()
    {
        var read = AcceptedClaims.Read("none", Entry, NullLogger.Instance);

        read.IsSuccess.ShouldBeTrue();
        read.Value.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void NamedClaimsAreAcceptedAsStated()
    {
        var read = AcceptedClaims.Read(" roles , perm ", Entry, NullLogger.Instance);

        read.IsSuccess.ShouldBeTrue();
        read.Value.ShouldNotBeNull().ShouldBe(new[] { ClaimDefinitions.roles.Name, ClaimDefinitions.perm.Name });
    }

    [Theory]
    [InlineData("actingRole")]
    [InlineData("actor")]
    [InlineData("sid")]
    [InlineData("agent")]
    [InlineData("agentKeyId")]
    [InlineData("agentLabel")]
    [InlineData("globalTenant")]
    [Trait("Priority", "P0")]
    public void AClaimOnlyThePlatformMayStateAboutASessionIsRefusedNamingIt(string claim)
    {
        var read = AcceptedClaims.Read($"roles,{claim}", Entry, NullLogger.Instance);

        read.IsFailure.ShouldBeTrue();
        read.Messages.Single().Message.ShouldContain(claim);
        read.Messages.Single().Message.ShouldContain(Entry);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void ANameThatIsNotAPlatformClaimIsRefusedAndTheKnownOnesAreListed()
    {
        var read = AcceptedClaims.Read("role", Entry, NullLogger.Instance);

        read.IsFailure.ShouldBeTrue();
        read.Messages.Single().Message.ShouldContain("'role'");
        read.Messages.Single().Message.ShouldContain(ClaimDefinitions.roles.Name);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void EverySessionBoundClaimIsOneTheDefinitionsMarkAndNoneIsMissed()
    {
        ClaimDefinitions.All().Where(definition => definition.IsSessionBound).Select(definition => definition.Name).OrderBy(name => name).ToArray()
            .ShouldBe(new[] { "actingRole", "actor", "agent", "agentKeyId", "agentLabel", "globalTenant", "sid" });
    }
}
