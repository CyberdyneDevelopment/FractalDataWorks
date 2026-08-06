using System;
using Fdw.Services.Resiliency.Abstractions;

namespace Fdw.Services.Resiliency.Polly.Tests.TestDoubles;

/// <summary>
/// Minimal <see cref="IResiliencyExecutionContext"/> test double.
/// </summary>
internal sealed class FakeResiliencyExecutionContext : IResiliencyExecutionContext
{
    public Guid ExecutionId { get; init; } = Guid.NewGuid();

    public Guid StageId { get; init; } = Guid.NewGuid();

    public Guid? SourceDataSetId { get; init; }

    public int AttemptNumber { get; init; }
}
