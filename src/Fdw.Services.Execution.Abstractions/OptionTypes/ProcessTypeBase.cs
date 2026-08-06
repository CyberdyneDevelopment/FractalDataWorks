using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Fdw.Collections;

namespace Fdw.Services.Execution.Abstractions.OptionTypes;

/// <summary>
/// Base class for all process types in the Fdw system.
/// Process types define what kinds of work can be executed.
/// Examples: ETL, HealthCheck, DataSync, etc.
/// </summary>
public abstract class ProcessTypeBase : TypeOptionBase<int, IProcessType>, ITypeOption<int, ProcessTypeBase>, IProcessType
{
    /// <summary>
    /// Initializes a new instance of the ProcessTypeBase class.
    /// </summary>
    /// <param name="id">Unique identifier for this process type.</param>
    /// <param name="name">Name of the process type.</param>
    protected ProcessTypeBase(int id, string name) : base(id, name)
    {
    }

    /// <summary>
    /// Factory method to create a process instance of this type.
    /// </summary>
    /// <param name="processId">Unique identifier for the process instance.</param>
    /// <param name="configuration">Configuration for the process.</param>
    /// <param name="serviceProvider">Service provider for dependency resolution.</param>
    /// <returns>A process instance ready for execution.</returns>
    public abstract IProcess CreateProcess(string processId, object configuration, IServiceProvider serviceProvider);

    /// <summary>
    /// Execute a specific operation on a process of this type.
    /// </summary>
    /// <param name="operationName">Name of the operation to execute.</param>
    /// <param name="processId">Unique identifier for the process instance.</param>
    /// <param name="serviceProvider">Service provider for dependency resolution.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of the operation execution.</returns>
    public abstract Task<IProcessResult> Execute(
        string operationName,
        string processId,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the operations supported by this process type.
    /// </summary>
    /// <returns>Array of supported operation names.</returns>
    public abstract string[] GetSupportedOperations();

    /// <summary>
    /// Get the configuration type required by this process type.
    /// </summary>
    /// <returns>Type of the configuration class.</returns>
    public abstract Type GetConfigurationType();

    /// <summary>
    /// Validate that a configuration object is valid for this process type.
    /// </summary>
    /// <param name="configuration">Configuration to validate.</param>
    /// <returns>True if valid, false otherwise.</returns>
    public abstract bool IsValidConfiguration(object configuration);
}