using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Aui;

/// <summary>
/// Defines an executable action within the Agent User Interface (AUI).
/// </summary>
public interface IAuiAction
{
    /// <summary>
    /// Gets the unique name of the action.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Executes the action with the provided parameters.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="parameters">The parameters provided by the agent.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating success or failure of the execution.</returns>
    Task<IGenericResult> Execute(Guid userId, IDictionary<string, object> parameters, CancellationToken ct = default);
}
