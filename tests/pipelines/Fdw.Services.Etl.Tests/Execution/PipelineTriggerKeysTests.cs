using Fdw.Abstractions;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace Fdw.Services.Etl.Tests.Execution;

/// <summary>
/// Tests for the key of a scheduled fire (FDW-4317): a new key is claimed, a repeat answers the run the first call started, a fire older than the retention is refused as expired, a key
/// whose run is not made yet is in progress, and nothing is assumed when the setting or the store is missing.
/// </summary>
[Collection(EtlOperationalConnectionCollection.Name)]
public sealed class PipelineTriggerKeysTests : IDisposable
{
    private static readonly Guid Tenant = Guid.Parse("cccccccc-0000-0000-0000-000000000003");
    private static readonly DateTimeOffset Now = new(2026, 11, 6, 12, 0, 0, TimeSpan.Zero);
    private const string Key = "7a1f0a52-0000-0000-0000-000000000001:2026-11-06T11:59:00.0000000+00:00";

    private readonly string? _before = EtlPipelineTypes.OperationalConnection;
    private readonly Mock<IDataGateway> _gateway = new();
    private readonly Mock<IEffectiveSettingsProvider> _settings = new();
    private readonly List<IDataCommand> _calls = [];
    private readonly PipelineTriggerKeys _keys;

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    public PipelineTriggerKeysTests()
    {
        EtlPipelineTypes.OperationalConnection = "Ops";
        _settings.Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.PipelineTriggerKeyRetentionSeconds, null, null)).Returns(3600);
        var gateways = new Mock<IDataGatewayProvider>();
        gateways.Setup(provider => provider.ByName("Main")).Returns(_gateway.Object);
        var accessor = new Mock<IAuthenticationContextAccessor>();
        accessor.SetupProperty(a => a.Current);
        var services = new ServiceCollection().AddSingleton(gateways.Object).AddSingleton(accessor.Object).AddSingleton(_settings.Object).BuildServiceProvider();
        _keys = new PipelineTriggerKeys(services.GetRequiredService<IServiceScopeFactory>(), new FixedTime(Now));
        _gateway.Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .Callback((IDataCommand command, DataStoreTarget _, CancellationToken _) => _calls.Add(command))
            .ReturnsAsync(GenericResult<int>.Success(1));
    }

    public void Dispose() => EtlPipelineTypes.OperationalConnection = _before;

    private void InsertRefusedAndRowIs(params EtlPipelineTriggerKeyRecord[] rows)
    {
        _gateway.Setup(g => g.Execute<int>(It.Is<IDataCommand>(c => c.GetType().Name.Contains("Insert")), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<int>.Failure(new GenericMessage("the key is taken")));
        _gateway.Setup(g => g.Execute<IEnumerable<EtlPipelineTriggerKeyRecord>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<EtlPipelineTriggerKeyRecord>>.Success(rows));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AKeyNobodyHasIsClaimedNew()
    {
        var claim = await _keys.Claim(Tenant, Key, Now.AddMinutes(-1), TestContext.Current.CancellationToken);

        claim.IsSuccess.ShouldBeTrue();
        claim.Value.ShouldNotBeNull().IsNew.ShouldBeTrue();
        _calls.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARepeatOfAKeyThatStartedARunAnswersThatRun()
    {
        var run = Guid.NewGuid();
        InsertRefusedAndRowIs(new EtlPipelineTriggerKeyRecord { TenantId = Tenant, KeyText = Key, FireTime = Now.AddMinutes(-1), ExecutionId = run });

        var claim = await _keys.Claim(Tenant, Key, Now.AddMinutes(-1), TestContext.Current.CancellationToken);

        claim.IsSuccess.ShouldBeTrue();
        claim.Value.ShouldNotBeNull().IsNew.ShouldBeFalse();
        claim.Value.ExecutionId.ShouldBe(run);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AKeyWhoseRunIsNotMadeYetIsInProgressAndNotNew()
    {
        InsertRefusedAndRowIs(new EtlPipelineTriggerKeyRecord { TenantId = Tenant, KeyText = Key, FireTime = Now.AddMinutes(-1), ExecutionId = null });

        var claim = await _keys.Claim(Tenant, Key, Now.AddMinutes(-1), TestContext.Current.CancellationToken);

        claim.IsFailure.ShouldBeTrue();
        claim.CurrentMessage.ShouldNotBeNull().ShouldContain("has not finished");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AFireOlderThanTheRetentionIsRefusedAsExpiredAndTouchesNothing()
    {
        var claim = await _keys.Claim(Tenant, Key, Now.AddSeconds(-3601), TestContext.Current.CancellationToken);

        claim.IsFailure.ShouldBeTrue();
        claim.CurrentMessage.ShouldNotBeNull().ShouldContain("expired");
        _calls.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AnInsertRefusedForAnotherReasonIsAFailureAndNeverNew()
    {
        InsertRefusedAndRowIs();

        var claim = await _keys.Claim(Tenant, Key, Now.AddMinutes(-1), TestContext.Current.CancellationToken);

        claim.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task WithNoRetentionSettingNoKeyIsClaimedAndNoNumberIsAssumed()
    {
        _settings.Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.PipelineTriggerKeyRetentionSeconds, null, null)).Returns(0);

        var claim = await _keys.Claim(Tenant, Key, Now.AddMinutes(-1), TestContext.Current.CancellationToken);

        claim.IsFailure.ShouldBeTrue();
        _calls.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task WithNoOperationalConnectionNoKeyIsClaimed()
    {
        EtlPipelineTypes.OperationalConnection = null;

        var claim = await _keys.Claim(Tenant, Key, Now.AddMinutes(-1), TestContext.Current.CancellationToken);

        claim.IsFailure.ShouldBeTrue();
        _calls.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task BindingARunToAKeyWithNoRowIsAFailure()
    {
        _gateway.Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<int>.Success(0));

        var bound = await _keys.Bind(Tenant, Key, Guid.NewGuid(), TestContext.Current.CancellationToken);

        bound.IsFailure.ShouldBeTrue();
    }
}
