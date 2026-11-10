using Fdw.Operations.Abstractions.Escalation;
using Fdw.Operations.Configuration;
using Fdw.Operations.Escalation;
using Fdw.Results;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Operations.Tests.Escalation;

/// <summary>
/// Tests for the two policy reads of <see cref="EscalationService"/> (FDW-4874): the list read returns every policy with its state, so a policy created not in force can be
/// found and edited, and the alert engine's read returns only the enabled ones and does not read a disabled policy in full.
/// </summary>
[Trait("Priority", "P0")]
public sealed class EscalationServicePolicyReadTests
{
    private static readonly EscalationPolicyImplementationConfiguration InForce = Policy("InForce", isEnabled: true);
    private static readonly EscalationPolicyImplementationConfiguration NotInForce = Policy("NotInForce", isEnabled: false);

    private readonly Mock<IEscalationConfigurationProvider> _provider = new();

    private static EscalationPolicyImplementationConfiguration Policy(string name, bool isEnabled) => new()
    {
        Id = Guid.CreateVersion7(),
        Name = name,
        IsEnabled = isEnabled,
        ScheduleId = Guid.CreateVersion7(),
        MaxEscalationLevel = 1,
        CooldownMinutes = 15,
        MaxRetries = 2,
        RetryDelaySeconds = 60
    };

    private EscalationService Sut()
    {
        IReadOnlyList<IEscalationPolicyImplementationConfiguration> headers = [InForce, NotInForce];
        _provider
            .Setup(p => p.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<IEscalationPolicyImplementationConfiguration>>.Success(headers));
        foreach (var policy in new[] { InForce, NotInForce })
        {
            IEscalationPolicyImplementationConfiguration full = policy;
            _provider
                .Setup(p => p.Get(policy.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<IEscalationPolicyImplementationConfiguration>.Success(full));
        }

        return new EscalationService(_provider.Object, NullLoggerFactory.Instance);
    }

    [Fact]
    public async Task TheListReadReturnsEveryPolicyWithItsState()
    {
        var result = await Sut().GetAllPolicies(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Select(p => (p.Name, p.IsEnabled)).ShouldBe(new[] { ("InForce", true), ("NotInForce", false) });
    }

    [Fact]
    public async Task TheEngineReadReturnsOnlyTheEnabledPolicies()
    {
        var result = await Sut().GetEnabledPolicies(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Select(p => p.Name).ShouldBe(new[] { "InForce" });
    }

    [Fact]
    public async Task TheEngineReadDoesNotReadADisabledPolicyInFull()
    {
        var sut = Sut();

        await sut.GetEnabledPolicies(TestContext.Current.CancellationToken);

        _provider.Verify(p => p.Get(NotInForce.Id, It.IsAny<CancellationToken>()), Times.Never);
        _provider.Verify(p => p.Get(InForce.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TheListReadReadsEveryPolicyInFull()
    {
        var sut = Sut();

        await sut.GetAllPolicies(TestContext.Current.CancellationToken);

        _provider.Verify(p => p.Get(NotInForce.Id, It.IsAny<CancellationToken>()), Times.Once);
        _provider.Verify(p => p.Get(InForce.Id, It.IsAny<CancellationToken>()), Times.Once);
    }
}
