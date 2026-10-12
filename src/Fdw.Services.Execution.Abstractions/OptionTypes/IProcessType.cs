using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;

namespace Fdw.Services.Execution.Abstractions.OptionTypes;

/// <summary>
/// Interface defining the contract for process type enum options.
/// </summary>
public interface IProcessType : ITypeOption<int, IProcessType>
{
    /// <summary>
    /// Factory method to create a process instance of this type.
    /// </summary>
    /// <param name="processId">Unique identifier for the process instance.</param>
    /// <param name="configuration">Configuration for the process.</param>
    /// <param name="serviceProvider">Service provider for dependency resolution.</param>
    /// <returns>A process instance ready for execution.</returns>
    IProcess CreateProcess(string processId, object configuration, IServiceProvider serviceProvider);

    /// <summary>
    /// Execute a specific operation on a process of this type.
    /// </summary>
    /// <param name="operationName">Name of the operation to execute.</param>
    /// <param name="processId">Unique identifier for the process instance.</param>
    /// <param name="serviceProvider">Service provider for dependency resolution.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of the operation execution.</returns>
    Task<IProcessResult> Execute(
        string operationName,
        string processId,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the operations supported by this process type.
    /// </summary>
    /// <returns>Array of supported operation names.</returns>
    string[] GetSupportedOperations();

    /// <summary>
    /// Get the configuration type required by this process type.
    /// </summary>
    /// <returns>Type of the configuration class.</returns>
    Type GetConfigurationType();

    /// <summary>
    /// Validate that a configuration object is valid for this process type.
    /// </summary>
    /// <param name="configuration">Configuration to validate.</param>
    /// <returns>True if valid, false otherwise.</returns>
    bool IsValidConfiguration(object configuration);
}