using System;
using Fdw.Services.Resiliency.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Resiliency.Polly.Tests.TestDoubles;

/// <summary>
/// <see cref="IResiliencyExecutionContext"/> test double that also implements
/// <see cref="ILoggerProvider"/> so the "ctx is ILoggerProvider" branch in
/// <see cref="PollyRetryResiliencyType.Execute"/> can be exercised.
/// </summary>
internal sealed class FakeLoggerProviderExecutionContext : IResiliencyExecutionContext, ILoggerProvider
{
    public Guid ExecutionId { get; init; } = Guid.NewGuid();

    public Guid StageId { get; init; } = Guid.NewGuid();

    public Guid? SourceDataSetId { get; init; }

    public int AttemptNumber { get; init; }

    /// <summary>Gets a value indicating whether <see cref="CreateLogger"/> was invoked.</summary>
    public bool CreateLoggerCalled { get; private set; }

    /// <summary>Gets the category name passed to the last <see cref="CreateLogger"/> call.</summary>
    public string? LastCategoryName { get; private set; }

    public ILogger CreateLogger(string categoryName)
    {
        CreateLoggerCalled = true;
        LastCategoryName = categoryName;
        return NullLogger.Instance;
    }

    public void Dispose()
    {
    }
}
