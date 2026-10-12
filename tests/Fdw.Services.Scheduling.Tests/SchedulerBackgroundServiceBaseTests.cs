using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Authentication.Abstractions.Security;
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
        var loop = new RecordingLoop(accessor, passes: 2);

        await loop.StartAsync(TestContext.Current.CancellationToken);
        await loop.Done.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        await loop.StopAsync(TestContext.Current.CancellationToken);

        loop.SystemDuringEvaluation.ShouldBe([true, true]);
        accessor.Current.ShouldBeSameAs(previous);
    }

    private sealed class RecordingLoop : SchedulerBackgroundServiceBase
    {
        private readonly IAuthenticationContextAccessor _accessor;
        private readonly int _passes;

        public RecordingLoop(IAuthenticationContextAccessor accessor, int passes)
            : base(accessor, logger: null)
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
