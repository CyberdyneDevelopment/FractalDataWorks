using Fdw.Operations.Abstractions.Thresholds;
using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Fdw.Services.Connections.Tests;

/// <summary>
/// FDW-3951: the DisableConnection consequence disables the connection the threshold fired on, with the firing's sentence as the
/// reason and the threshold and rule as the author, and fails when the state cannot be recorded.
/// </summary>
public sealed class ConnectionDisablerTests
{
    private static readonly Guid ConnectionId = Guid.Parse("66666666-6666-6666-6666-666666666666");
    private static readonly Guid TenantId = Guid.Parse("77777777-7777-7777-7777-777777777777");

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task AFiringOnAConnectionDisablesItWithTheFiringsReasonAndTheThresholdAsAuthor()
    {
        var states = new Mock<IConnectionDisabledStates>();
        states
            .Setup(s => s.Disable(ConnectionId, TenantId, false, "3 failures in 10 minutes", "threshold 'SalesDbFailures' rule 2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ConnectionDisabledRecord>.Success(new ConnectionDisabledRecord(ConnectionId, DateTimeOffset.UnixEpoch, "x", "y")));
        var disabler = new ConnectionDisabler(states.Object, Tenants(false).Object, new ConnectionDisablingOption(), NullLogger<ConnectionDisabler>.Instance);

        var result = await disabler.Disable(Firing("Connection"), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        states.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task AFailureToRecordTheStateFailsTheConsequence()
    {
        var states = new Mock<IConnectionDisabledStates>();
        states
            .Setup(s => s.Disable(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ConnectionDisabledRecord>.Failure(new Fdw.Messages.GenericMessage("write refused")));
        var disabler = new ConnectionDisabler(states.Object, Tenants(false).Object, new ConnectionDisablingOption(), NullLogger<ConnectionDisabler>.Instance);

        var result = await disabler.Disable(Firing("Connection"), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task AFiringOnSomethingThatIsNotAConnectionOrAPipelineIsRefusedAndNothingIsDisabled()
    {
        var states = new Mock<IConnectionDisabledStates>(MockBehavior.Strict);
        var disabler = new ConnectionDisabler(states.Object, Tenants(false).Object, new ConnectionDisablingOption(), NullLogger<ConnectionDisabler>.Instance);

        var result = await disabler.Disable(Firing("Schedule"), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task AFiringOnAPipelineDisablesTheConnectionItsValuesNameAndNotThePipelinesId()
    {
        var failedOn = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var states = new Mock<IConnectionDisabledStates>();
        states
            .Setup(s => s.Disable(failedOn, TenantId, false, "3 failures in 10 minutes", "threshold 'SalesDbFailures' rule 2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ConnectionDisabledRecord>.Success(new ConnectionDisabledRecord(failedOn, DateTimeOffset.UnixEpoch, "x", "y")));
        var disabler = new ConnectionDisabler(states.Object, Tenants(false).Object, new ConnectionDisablingOption(), NullLogger<ConnectionDisabler>.Instance);
        var values = new Dictionary<string, object?> { [FailedConnectionValue.Name] = failedOn.ToString() };

        var result = await disabler.Disable(Firing("Pipeline", values), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        states.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task AFiringOnAPipelineWhoseValuesNameNoConnectionFailsLoudlyAndDisablesNothing()
    {
        var states = new Mock<IConnectionDisabledStates>(MockBehavior.Strict);
        var disabler = new ConnectionDisabler(states.Object, Tenants(false).Object, new ConnectionDisablingOption(), NullLogger<ConnectionDisabler>.Instance);

        var result = await disabler.Disable(Firing("Pipeline"), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Theory]
    [InlineData("not a guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task AFiringOnAPipelineWhoseConnectionValueIsNotAnIdFailsLoudlyAndDisablesNothing(string value)
    {
        var states = new Mock<IConnectionDisabledStates>(MockBehavior.Strict);
        var disabler = new ConnectionDisabler(states.Object, Tenants(false).Object, new ConnectionDisablingOption(), NullLogger<ConnectionDisabler>.Instance);

        var result = await disabler.Disable(
            Firing("Pipeline", new Dictionary<string, object?> { [FailedConnectionValue.Name] = value }), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    // An option whose declared default allows a global tenant's rule to disable another tenant's connection, as a platform that allows it declares.
    private sealed class AllowingOption : ConnectionDisablingOption
    {
        public override IReadOnlyDictionary<string, object> DeclaredDefaults { get; } = new Dictionary<string, object>
        {
            [GlobalRulesMayDisableOtherTenantsConnections] = true,
        };
    }

    private static Mock<Fdw.Services.Multitenancy.Abstractions.ITenantProvider> Tenants(bool isGlobal, bool readable = true)
    {
        var tenant = new Mock<Fdw.Services.Multitenancy.Abstractions.ITenant>();
        tenant.SetupGet(t => t.IsGlobal).Returns(isGlobal);
        var provider = new Mock<Fdw.Services.Multitenancy.Abstractions.ITenantProvider>();
        provider
            .Setup(p => p.GetTenant(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(readable
                ? GenericResult<Fdw.Services.Multitenancy.Abstractions.ITenant>.Success(tenant.Object)
                : GenericResult<Fdw.Services.Multitenancy.Abstractions.ITenant>.Failure(new Fdw.Messages.GenericMessage("tenant store down")));
        return provider;
    }

    private static Mock<IConnectionDisabledStates> StatesThatAnswer(bool expectedMayDisableOthers)
    {
        var states = new Mock<IConnectionDisabledStates>();
        states
            .Setup(s => s.Disable(ConnectionId, TenantId, expectedMayDisableOthers, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ConnectionDisabledRecord>.Success(new ConnectionDisabledRecord(ConnectionId, DateTimeOffset.UnixEpoch, "x", "y")));
        return states;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task WhenThePlatformAllowsItAGlobalTenantsRuleMayDisableOtherTenantsConnections()
    {
        var states = StatesThatAnswer(true);
        var disabler = new ConnectionDisabler(states.Object, Tenants(isGlobal: true).Object, new AllowingOption(), NullLogger<ConnectionDisabler>.Instance);

        var result = await disabler.Disable(Firing("Connection"), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        states.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task WhenThePlatformAllowsItATenantThatIsNotTheGlobalTenantStillMayNot()
    {
        var states = StatesThatAnswer(false);
        var disabler = new ConnectionDisabler(states.Object, Tenants(isGlobal: false).Object, new AllowingOption(), NullLogger<ConnectionDisabler>.Instance);

        var result = await disabler.Disable(Firing("Connection"), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        states.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ByDefaultTheTenantIsNotEvenLookedUpAndEvenAGlobalTenantsRuleMayNot()
    {
        var states = StatesThatAnswer(false);
        var tenants = Tenants(isGlobal: true);
        var disabler = new ConnectionDisabler(states.Object, tenants.Object, new ConnectionDisablingOption(), NullLogger<ConnectionDisabler>.Instance);

        var result = await disabler.Disable(Firing("Connection"), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        states.VerifyAll();
        tenants.Verify(p => p.GetTenant(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task WhenThePlatformAllowsItATenantThatCannotBeReadFailsTheConsequenceAndDisablesNothing()
    {
        var states = new Mock<IConnectionDisabledStates>(MockBehavior.Strict);
        var disabler = new ConnectionDisabler(states.Object, Tenants(isGlobal: true, readable: false).Object, new AllowingOption(), NullLogger<ConnectionDisabler>.Instance);

        var result = await disabler.Disable(Firing("Connection"), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    private static EscalationFiring Firing(string subjectKind, IReadOnlyDictionary<string, object?>? values = null)
        => new(
            new ThresholdSubject(subjectKind, "SalesDb", ConnectionId, TenantId, null),
            "SalesDbFailures",
            2,
            new ThresholdRule(new Mock<Fdw.Data.Abstractions.IFilterNode>().Object, "DisableConnection", null, []),
            values ?? new Dictionary<string, object?>(),
            "3 failures in 10 minutes");
}
