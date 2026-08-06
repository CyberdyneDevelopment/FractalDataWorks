using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;

namespace Fdw.Services.Workflows.Abstractions;

/// <summary>
/// Provides centralized registry and resolution for Workflow configurations.
/// Supports both IOptionsMonitor-backed configuration and runtime registration.
/// </summary>
public interface IWorkflowProvider
{
    /// <summary>Gets a workflow configuration by its unique identifier.</summary>
    Task<IGenericResult<IGenericConfiguration>> Get(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Gets a workflow configuration by its name.</summary>
    Task<IGenericResult<IGenericConfiguration>> Get(string name, CancellationToken cancellationToken = default);

    /// <summary>Gets all registered workflow configurations.</summary>
    Task<IGenericResult<IReadOnlyList<IGenericConfiguration>>> Get(CancellationToken cancellationToken = default);
}
