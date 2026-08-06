using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Hosting.Logging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Hosting.Startup;

/// <summary>
/// Mutable accumulator for bootstrap step results.
/// Records success/failure of each startup step and provides a final summary.
/// </summary>
public sealed class StartupResult
{
    private readonly List<StartupStepResult> _steps = [];
    private readonly ILogger _logger;

    public StartupResult(ILogger<StartupResult>? logger = null)
    {
        _logger = logger ?? NullLogger<StartupResult>.Instance;
    }

    /// <summary>
    /// Gets whether all steps completed successfully.
    /// </summary>
    public bool IsSuccess => !_steps.Any(s => !s.IsSuccess);

    /// <summary>
    /// Gets whether any fatal step has failed.
    /// </summary>
    public bool HasFatalFailure => _steps.Any(s => s.IsFatal && !s.IsSuccess);

    /// <summary>
    /// Gets all recorded steps.
    /// </summary>
    public IReadOnlyList<StartupStepResult> Steps => _steps;

    /// <summary>
    /// Gets only the failed steps.
    /// </summary>
    public IReadOnlyList<StartupStepResult> Failures =>
        _steps.Where(s => !s.IsSuccess).ToList();

    /// <summary>
    /// Records a successful step.
    /// </summary>
    public StartupResult AddSuccess(string phase, string stepName, IGenericMessage? message = null)
    {
        _steps.Add(new StartupStepResult
        {
            Phase = phase,
            StepName = stepName,
            IsSuccess = true,
            IsFatal = false,
            Message = message,
            Timestamp = DateTimeOffset.UtcNow
        });
        return this;
    }

    /// <summary>
    /// Records a failed step.
    /// </summary>
    public StartupResult AddFailure(string phase, string stepName, IGenericMessage message, bool fatal = false)
    {
        _steps.Add(new StartupStepResult
        {
            Phase = phase,
            StepName = stepName,
            IsSuccess = false,
            IsFatal = fatal,
            Message = message,
            Timestamp = DateTimeOffset.UtcNow
        });
        return this;
    }

    /// <summary>
    /// Records a failed step with an exception.
    /// </summary>
    public StartupResult AddFailure(string phase, string stepName, Exception ex, IGenericMessage message, bool fatal = true)
    {
        _steps.Add(new StartupStepResult
        {
            Phase = phase,
            StepName = stepName,
            IsSuccess = false,
            IsFatal = fatal,
            Message = message,
            Exception = ex,
            Timestamp = DateTimeOffset.UtcNow
        });
        return this;
    }

    /// <summary>
    /// Wraps a synchronous action in try/catch and records the result.
    /// Does not log immediately — <see cref="Complete"/> handles all failure logging.
    /// </summary>
    public StartupResult TryStep(string phase, string stepName, Action action, bool fatal = true)
    {
        try
        {
            action();
            AddSuccess(phase, stepName);
        }
        catch (Exception ex)
        {
            _steps.Add(new StartupStepResult
            {
                Phase = phase,
                StepName = stepName,
                IsSuccess = false,
                IsFatal = fatal,
                Exception = ex,
                Timestamp = DateTimeOffset.UtcNow
            });
        }
        return this;
    }

    /// <summary>
    /// Wraps an async action in try/catch and records the result.
    /// Does not log immediately — <see cref="Complete"/> handles all failure logging.
    /// </summary>
    public async Task<StartupResult> TryStep(string phase, string stepName, Func<Task> action, bool fatal = true)
    {
        try
        {
            await action().ConfigureAwait(false);
            AddSuccess(phase, stepName);
        }
        catch (Exception ex)
        {
            _steps.Add(new StartupStepResult
            {
                Phase = phase,
                StepName = stepName,
                IsSuccess = false,
                IsFatal = fatal,
                Exception = ex,
                Timestamp = DateTimeOffset.UtcNow
            });
        }
        return this;
    }

    /// <summary>
    /// Wraps an async action that returns a value in try/catch and records the result.
    /// Returns default(T) on failure. Does not log immediately — <see cref="Complete"/> handles all failure logging.
    /// </summary>
    public async Task<(StartupResult Result, T? Value)> TryStep<T>(
        string phase, string stepName, Func<Task<T>> action, bool fatal = true)
    {
        try
        {
            var value = await action().ConfigureAwait(false);
            AddSuccess(phase, stepName);
            return (this, value);
        }
        catch (Exception ex)
        {
            _steps.Add(new StartupStepResult
            {
                Phase = phase,
                StepName = stepName,
                IsSuccess = false,
                IsFatal = fatal,
                Exception = ex,
                Timestamp = DateTimeOffset.UtcNow
            });
            return (this, default);
        }
    }

    /// <summary>
    /// Logs all failures and returns the process exit code.
    /// Returns 0 on success, 1 on failure.
    /// </summary>
    public int Complete(string applicationName)
    {
        var failures = Failures;
        if (failures.Count > 0)
        {
            HostingLog.StartupFailed(_logger, failures.Count);
            foreach (var failure in failures)
            {
                var error = failure.Message?.ToString() ?? failure.Exception?.Message ?? "Unknown error";
                HostingLog.StartupStepFailed(_logger, failure.Phase, failure.StepName, error);
            }
            return 1;
        }

        HostingLog.StartupCompleted(_logger, _steps.Count);
        return 0;
    }
}
