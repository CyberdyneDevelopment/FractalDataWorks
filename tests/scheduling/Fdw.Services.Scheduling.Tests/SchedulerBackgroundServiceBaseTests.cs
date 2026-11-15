using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Scheduling.Tests;

/// <summary>
/// The scheduler loop runs every evaluation as the system: a scheduler reads every tenant's
/// schedules, and a pass that ran with no context would open its connections as the deny principal
/// and see none of them.
/// </summary>
public sealed class SchedulerBackgroundServiceBaseTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task EachEvaluationRunsInTheSystemContextAndRestoresThePreviousOne()
    {
        var accessor = new AuthenticationContextAccessor();
        var previous = accessor.Current;
        var loop = new RecordingLoop(accessor, new ScriptedRole(GenericResult<bool>.Success(true)), passes: 2);

        await loop.StartAsync(TestContext.Current.CancellationToken);
        await loop.Done.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        await loop.StopAsync(TestContext.Current.CancellationToken);

        loop.SystemDuringEvaluation.ShouldBe([true, true]);
        accessor.Current.ShouldBeSameAs(previous);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Slots")]
    public async Task AStandbyCopyEvaluatesNothingButKeepsAskingTheRole()
    {
        var role = new ScriptedRole(GenericResult<bool>.Success(false));
        var loop = new RecordingLoop(new AuthenticationContextAccessor(), role, passes: 1);

        await loop.StartAsync(TestContext.Current.CancellationToken);
        await WaitUntil(() => role.Asked >= 4);
        await loop.StopAsync(TestContext.Current.CancellationToken);

        loop.SystemDuringEvaluation.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Slots")]
    public async Task ASwapMakesTheNextPassEvaluate()
    {
        var role = new ScriptedRole(GenericResult<bool>.Success(false), GenericResult<bool>.Success(false), GenericResult<bool>.Success(true));
        var loop = new RecordingLoop(new AuthenticationContextAccessor(), role, passes: 1);

        await loop.StartAsync(TestContext.Current.CancellationToken);
        await loop.Done.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        await loop.StopAsync(TestContext.Current.CancellationToken);

        role.Asked.ShouldBeGreaterThanOrEqualTo(3);
        loop.SystemDuringEvaluation.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Slots")]
    public async Task ACopyThatCannotReadItsRoleEvaluatesNothing()
    {
        var unknown = GenericResult<bool>.Failure(SchedulerLoopLog.PassSkippedRoleUnknown(NullLogger.Instance));
        var role = new ScriptedRole(unknown);
        var loop = new RecordingLoop(new AuthenticationContextAccessor(), role, passes: 1);

        await loop.StartAsync(TestContext.Current.CancellationToken);
        await WaitUntil(() => role.Asked >= 3);
        await loop.StopAsync(TestContext.Current.CancellationToken);

        loop.SystemDuringEvaluation.ShouldBeEmpty();
    }

    private static async Task WaitUntil(Func<bool> done)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (!done() && DateTime.UtcNow < deadline)
            await Task.Delay(5, TestContext.Current.CancellationToken);
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

    private sealed class RecordingLoop : SchedulerBackgroundServiceBase
    {
        private readonly IAuthenticationContextAccessor _accessor;
        private readonly int _passes;

        public RecordingLoop(IAuthenticationContextAccessor accessor, ISlotRole role, int passes)
            : base(accessor, role, logger: null)
        {
            _accessor = accessor;
            _passes = passes;
        }

        public List<bool> SystemDuringEvaluation { get; } = [];

        public TaskCompletionSource Done { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        protected override TimeSpan EvaluationInterval => TimeSpan.FromMilliseconds(1);

        protected override Task Evaluate(CancellationToken cancellationToken)
        {
            if (SystemDuringEvaluation.Count < _passes)
            {
                SystemDuringEvaluation.Add(_accessor.Current?.IsSystemContext == true);
                if (SystemDuringEvaluation.Count == _passes)
                    Done.TrySetResult();
            }

            return Task.CompletedTask;
        }
    }
}
