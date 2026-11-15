using Fdw.Operations.Abstractions.Escalation;
using Fdw.Operations.Escalation;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Fdw.Operations.Tests.Escalation;

/// <summary>
/// The escalation timer asks the slot role before every pass (FDW-5870): the live copy advances the open alerts, a standby copy advances none,
/// a swap between two passes changes the next one, and a copy that cannot read its role advances none.
/// </summary>
public sealed class EscalationBackgroundServiceSlotRoleTests
{
    private sealed class OneSecondSettings : IEffectiveSettingsProvider
    {
        public T GetEffectiveValue<T>(string settingName, Guid? tenantId = null, string? roleName = null) => throw new NotSupportedException();

        public bool TryGetEffectiveValue<T>(string settingName, out T value, Guid? tenantId = null, string? roleName = null)
        {
            value = (T)(object)1;
            return true;
        }
    }

    private sealed class ScriptedRole(params IGenericResult<bool>[] answers) : ISlotRole
    {
        private int _next;

        public int Asked => _next;

        public IGenericResult<bool> BackgroundWorkMayRun()
        {
            var answer = answers[Math.Min(_next, answers.Length - 1)];
            _next++;
            return answer;
        }
    }

    private static (EscalationBackgroundService Service, Mock<IEscalationAlerts> Alerts) Build(ScriptedRole role)
    {
        var alerts = new Mock<IEscalationAlerts>();
        alerts.Setup(a => a.Advance(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<int>.Success(0));

        var provider = new ServiceCollection()
            .AddSingleton<IAuthenticationContextAccessor>(new AuthenticationContextAccessor())
            .AddSingleton<IEffectiveSettingsProvider>(new OneSecondSettings())
            .AddSingleton(alerts.Object)
            .BuildServiceProvider();

        return (new EscalationBackgroundService(provider.GetRequiredService<IServiceScopeFactory>(), TimeProvider.System, role, NullLogger<EscalationBackgroundService>.Instance), alerts);
    }

    private static async Task WaitUntil(Func<bool> done)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (!done() && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20, TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Slots")]
    public async Task ALiveCopyAdvancesTheOpenAlerts()
    {
        var role = new ScriptedRole(GenericResult<bool>.Success(true));
        var (service, alerts) = Build(role);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await WaitUntil(() => alerts.Invocations.Count >= 1);
        await service.StopAsync(CancellationToken.None);

        alerts.Verify(a => a.Advance(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce());
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Slots")]
    public async Task AStandbyCopyAdvancesNoAlertButKeepsAskingTheRole()
    {
        var role = new ScriptedRole(GenericResult<bool>.Success(false));
        var (service, alerts) = Build(role);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await WaitUntil(() => role.Asked >= 2);
        await service.StopAsync(CancellationToken.None);

        role.Asked.ShouldBeGreaterThanOrEqualTo(2);
        alerts.Verify(a => a.Advance(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Slots")]
    public async Task ASwapMakesTheNextPassAdvanceTheAlerts()
    {
        var role = new ScriptedRole(GenericResult<bool>.Success(false), GenericResult<bool>.Success(true));
        var (service, alerts) = Build(role);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await WaitUntil(() => alerts.Invocations.Count >= 1);
        await service.StopAsync(CancellationToken.None);

        alerts.Verify(a => a.Advance(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce());
        role.Asked.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Slots")]
    public async Task ACopyThatCannotReadItsRoleAdvancesNoAlert()
    {
        var unknown = GenericResult<bool>.Failure(Fdw.Services.Data.Logging.SlotRoleLog.RoleFileUnreadable(NullLogger.Instance, "role", "gone"));
        var role = new ScriptedRole(unknown);
        var (service, alerts) = Build(role);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await WaitUntil(() => role.Asked >= 2);
        await service.StopAsync(CancellationToken.None);

        alerts.Verify(a => a.Advance(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never());
    }
}
