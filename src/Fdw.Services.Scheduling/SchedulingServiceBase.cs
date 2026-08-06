using Fdw.Configuration;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Fdw.Services;
using Fdw.Services.Scheduling.Abstractions;

namespace Fdw.Services.Scheduling;

/// <summary>
/// Base class for scheduling service implementations.
/// Provides common scheduling service functionality following the standard service pattern.
/// </summary>
/// <typeparam name="TCommand">The command type this scheduling service executes.</typeparam>
/// <typeparam name="TConfiguration">The configuration type for the scheduling service.</typeparam>
/// <typeparam name="TService">The concrete service type for logging and identification purposes.</typeparam>
/// <remarks>
/// This class provides a foundation for building scheduling services that integrate
/// with the Fdw framework's service management and scheduling abstractions.
/// All scheduling services should inherit from this class to ensure consistent
/// behavior across different scheduling providers.
/// </remarks>
[ExcludeFromCodeCoverage(Justification = "Abstract base class with constructor-only logic")]
public abstract class SchedulingServiceBase<TCommand, TConfiguration, TService>
    : ServiceBase<TCommand, TConfiguration, TService>
    where TCommand : ISchedulingCommand
    where TConfiguration : class, IGenericConfiguration
    where TService : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SchedulingServiceBase{TCommand, TConfiguration, TService}"/> class.
    /// </summary>
    /// <param name="logger">The logger for this scheduling service.</param>
    /// <param name="configuration">The configuration for this scheduling service.</param>
    protected SchedulingServiceBase(ILogger<TService> logger, TConfiguration configuration)
        : base(logger, configuration)
    {
    }
}