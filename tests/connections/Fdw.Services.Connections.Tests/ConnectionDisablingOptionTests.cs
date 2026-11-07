using Fdw.ServiceTypes;
using Fdw.Services.Connections;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Connections.Tests;

/// <summary>
/// FDW-4192: whether a global-tenant rule may disable another tenant's connection is declared by an option, strict by default, and a host changes it at registration with the same
/// mechanism as FDW-3823's tuning defaults.
/// </summary>
public sealed class ConnectionDisablingOptionTests
{
    // A subtype of its own: an override is keyed by the option's type, and the production option must stay untouched by this test.
    private sealed class OverriddenOption : ConnectionDisablingOption
    {
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void TheDeclaredDefaultIsStrictAndOnlyTheConnectionsOwnTenantMayDisableIt()
    {
        var answer = new ConnectionDisablingOption().GlobalRulesMayDisableOthers(NullLogger.Instance);

        answer.IsSuccess.ShouldBeTrue();
        answer.Value.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void AHostsOverrideAtRegistrationTakesEffectAndTheDeclaredDefaultIsNotTheOnlyAnswer()
    {
        var option = new OverriddenOption();

        ServiceTypeDefaults.Override(option, ConnectionDisablingOption.GlobalRulesMayDisableOtherTenantsConnections, true);
        var answer = option.GlobalRulesMayDisableOthers(NullLogger.Instance);

        answer.IsSuccess.ShouldBeTrue();
        answer.Value.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void TheDefaultIsDeclaredUnderItsName()
    {
        ConnectionDisablingOption.Instance.DeclaredDefaults.ShouldContainKey(ConnectionDisablingOption.GlobalRulesMayDisableOtherTenantsConnections);
        ConnectionDisablingOption.Instance.Category.ShouldBe("Connection");
    }
}
