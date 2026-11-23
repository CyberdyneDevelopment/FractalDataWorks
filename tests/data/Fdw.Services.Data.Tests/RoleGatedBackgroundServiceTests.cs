using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Logging;
using Fdw.Services.Data.Slots;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// A periodic worker on the role-gated base asks the role before every pass (FDW-5846): it runs while live, does nothing while standby,
/// follows a swap on its next pass, does nothing when the role cannot be read, and survives a pass that throws.
/// </summary>
public sealed class RoleGatedBackgroundServiceTests
{
    private sealed class ScriptedRole(params IGenericResult<bool>[] answers) : ISlotRole
    {
        public string? SlotName => null;

        private int _next;

        public int Asked => _next;

        public IGenericResult<bool> BackgroundWorkMayRun()
        {
            var answer = answers[Math.Min(_next, answers.Length - 1)];
            _next++;
            return answer;
        }
    }

    private sealed class Worker(ISlotRole role, Func<int, Task>? onPass = null)
        : RoleGatedBackgroundService(role, NullLogger.Instance)
    {
        private readonly List<int> _passes = [];

        public List<int> Passes => _passes;

        public TimeSpan IntervalToUse { get; set; } = TimeSpan.FromMilliseconds(5);

        protected override TimeSpan Interval => IntervalToUse;

        protected override async Task Pass(CancellationToken cancellationToken)
        {
            _passes.Add(_passes.Count + 1);
            if (onPass is not null)
                await onPass(_passes.Count).ConfigureAwait(false);
        }
    }

    private static IGenericResult<bool> Yes() => GenericResult<bool>.Success(true);

    private static IGenericResult<bool> No() => GenericResult<bool>.Success(false);

    private static IGenericResult<bool> Unknown() => GenericResult<bool>.Failure(SlotRoleLog.RoleFileUnreadable(NullLogger.Instance, "role", "gone"));

    private static async Task RunUntil(Worker worker, Func<bool> done, CancellationToken cancellationToken)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        await worker.StartAsync(cancellation.Token).ConfigureAwait(false);
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (!done() && DateTime.UtcNow < deadline)
            await Task.Delay(5, cancellationToken).ConfigureAwait(false);

        await worker.StopAsync(CancellationToken.None).ConfigureAwait(false);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Slots")]
    public async Task ALiveCopyRunsItsPasses()
    {
        var role = new ScriptedRole(Yes());
        var worker = new Worker(role);

        await RunUntil(worker, () => worker.Passes.Count >= 3, TestContext.Current.CancellationToken);

        worker.Passes.Count.ShouldBeGreaterThanOrEqualTo(3);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Slots")]
    public async Task AStandbyCopyRunsNoPassButKeepsAskingTheRole()
    {
        var role = new ScriptedRole(No());
        var worker = new Worker(role);

        await RunUntil(worker, () => role.Asked >= 4, TestContext.Current.CancellationToken);

        worker.Passes.ShouldBeEmpty();
        role.Asked.ShouldBeGreaterThanOrEqualTo(4);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Slots")]
    public async Task ASwapMidRunStartsThePassesOnTheNextAsk()
    {
        var role = new ScriptedRole(No(), No(), Yes());
        var worker = new Worker(role);

        await RunUntil(worker, () => worker.Passes.Count >= 2, TestContext.Current.CancellationToken);

        role.Asked.ShouldBeGreaterThanOrEqualTo(4);
        worker.Passes.Count.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Slots")]
    public async Task ACopyThatCannotReadItsRoleRunsNoPass()
    {
        var role = new ScriptedRole(Unknown());
        var worker = new Worker(role);

        await RunUntil(worker, () => role.Asked >= 3, TestContext.Current.CancellationToken);

        worker.Passes.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Slots")]
    public async Task APassThatThrowsDoesNotEndTheWorker()
    {
        var role = new ScriptedRole(Yes());
        var worker = new Worker(role, pass => pass == 1 ? throw new InvalidOperationException("one bad pass") : Task.CompletedTask);

        await RunUntil(worker, () => worker.Passes.Count >= 3, TestContext.Current.CancellationToken);

        worker.Passes.Count.ShouldBeGreaterThanOrEqualTo(3);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Slots")]
    public async Task AnIntervalOfNoTimeRunsNothing()
    {
        var role = new ScriptedRole(Yes());
        var worker = new Worker(role) { IntervalToUse = TimeSpan.Zero };

        await worker.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(50, TestContext.Current.CancellationToken);
        await worker.StopAsync(CancellationToken.None);

        worker.Passes.ShouldBeEmpty();
        role.Asked.ShouldBe(0);
    }
}
