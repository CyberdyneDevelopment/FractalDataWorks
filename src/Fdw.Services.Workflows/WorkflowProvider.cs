using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.MessageLogging;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Workflows.Abstractions;
using Fdw.Services.Workflows.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Fdw.Services.Workflows;

/// <summary>
/// Provides centralized registry and resolution for Workflow configurations.
/// Uses the Dual-Source Provider Pattern with IOptionsMonitor and runtime registration.
/// </summary>
public sealed class WorkflowProvider : IWorkflowProvider, IDisposable
{
    private bool _disposed;
    private readonly ILogger<WorkflowProvider> _logger;
    private readonly IOptionsMonitor<List<WorkflowConfiguration>>? _workflowOptions;
    private readonly ConcurrentDictionary<string, WorkflowConfiguration> _workflows;

    // Configured index for fast lookups
    private readonly ReaderWriterLockSlim _indexLock = new();
    private WorkflowIndex _configuredIndex;

    /// <summary>
    /// Immutable index record for pre-built configuration lookups.
    /// </summary>
    private sealed record WorkflowIndex(
        IReadOnlyDictionary<Guid, WorkflowConfiguration> WorkflowsById,
        IReadOnlyDictionary<string, WorkflowConfiguration> WorkflowsByName);

    /// <summary>
    /// Initializes a new instance without IOptionsMonitor (backward compat).
    /// </summary>
    public WorkflowProvider(ILogger<WorkflowProvider>? logger)
    {
        _logger = logger ?? NullLogger<WorkflowProvider>.Instance;
        _workflows = new ConcurrentDictionary<string, WorkflowConfiguration>(StringComparer.OrdinalIgnoreCase);
        _configuredIndex = new WorkflowIndex(
            new Dictionary<Guid, WorkflowConfiguration>(),
            new Dictionary<string, WorkflowConfiguration>(StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Initializes a new instance with IOptionsMonitor for configuration-backed workflows.
    /// </summary>
    public WorkflowProvider(
        ILogger<WorkflowProvider>? logger,
        IOptionsMonitor<List<WorkflowConfiguration>> workflowOptions)
        : this(logger)
    {
        _workflowOptions = workflowOptions ?? throw new ArgumentNullException(nameof(workflowOptions));
        _workflowOptions.OnChange(_ => RebuildConfiguredIndex());
        _configuredIndex = BuildConfiguredIndex();
    }

    /// <inheritdoc/>
    public int Count
    {
        get
        {
            _indexLock.EnterReadLock();
            try
            {
                return _workflows.Count + _configuredIndex.WorkflowsById.Count;
            }
            finally
            {
                _indexLock.ExitReadLock();
            }
        }
    }

    /// <inheritdoc/>
    public Task<IGenericResult<IGenericConfiguration>> Get(Guid id, CancellationToken cancellationToken = default)
    {
        // Check materialized first
        var materialized = _workflows.Values.FirstOrDefault(w => w.Id == id);
        if (materialized != null)
        {
            WorkflowProviderLog.WorkflowRetrievedById(_logger, id, "materialized");
            return Task.FromResult(GenericResult<IGenericConfiguration>.Success(materialized));
        }

        // Check configured index
        _indexLock.EnterReadLock();
        try
        {
            if (_configuredIndex.WorkflowsById.TryGetValue(id, out var config))
            {
                WorkflowProviderLog.WorkflowRetrievedById(_logger, id, "configured");
                return Task.FromResult(GenericResult<IGenericConfiguration>.Success(config));
            }
        }
        finally
        {
            _indexLock.ExitReadLock();
        }

        WorkflowProviderLog.WorkflowByIdNotFound(_logger, id);
        return Task.FromResult(GenericResult<IGenericConfiguration>.Failure(
            WorkflowResultCodes.ByName("WorkflowNotFound"),
            ResultDetails.Create().With("WorkflowId", id)));
    }

    /// <inheritdoc/>
    public Task<IGenericResult<IGenericConfiguration>> Get(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Task.FromResult(GenericResult<IGenericConfiguration>.Failure(
                WorkflowResultCodes.ByName("WorkflowNameRequired"), _logger));
        }

        // Check materialized first
        if (_workflows.TryGetValue(name, out var materialized))
        {
            WorkflowProviderLog.WorkflowRetrievedByName(_logger, name, "materialized");
            return Task.FromResult(GenericResult<IGenericConfiguration>.Success(materialized));
        }

        // Check configured index
        _indexLock.EnterReadLock();
        try
        {
            if (_configuredIndex.WorkflowsByName.TryGetValue(name, out var config))
            {
                WorkflowProviderLog.WorkflowRetrievedByName(_logger, name, "configured");
                return Task.FromResult(GenericResult<IGenericConfiguration>.Success(config));
            }
        }
        finally
        {
            _indexLock.ExitReadLock();
        }

        WorkflowProviderLog.WorkflowByNameNotFound(_logger, name);
        return Task.FromResult(GenericResult<IGenericConfiguration>.Failure(
            WorkflowResultCodes.ByName("WorkflowNotFound"),
            ResultDetails.Create().With("WorkflowName", name)));
    }

    /// <inheritdoc/>
    public Task<IGenericResult<IReadOnlyList<IGenericConfiguration>>> Get(CancellationToken cancellationToken = default)
    {
        var results = new List<IGenericConfiguration>();

        results.AddRange(_workflows.Values);

        _indexLock.EnterReadLock();
        try
        {
            var materializedNames = new HashSet<string>(_workflows.Keys, StringComparer.OrdinalIgnoreCase);
            results.AddRange(_configuredIndex.WorkflowsByName.Values
                .Where(c => !materializedNames.Contains(c.Name)));
        }
        finally
        {
            _indexLock.ExitReadLock();
        }

        WorkflowProviderLog.AllWorkflowsRetrieved(_logger, results.Count);
        return Task.FromResult(GenericResult<IReadOnlyList<IGenericConfiguration>>.Success(results));
    }

    /// <summary>Registers a workflow configuration at runtime.</summary>
    public void Register(WorkflowConfiguration configuration)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        if (string.IsNullOrWhiteSpace(configuration.Name))
            throw new ArgumentException("Workflow name is required", nameof(configuration));

        _workflows.AddOrUpdate(configuration.Name, configuration, (_, _) => configuration);
        WorkflowProviderLog.WorkflowRegistered(_logger, configuration.Name);
    }

    /// <summary>Unregisters a workflow configuration by name.</summary>
    public void Unregister(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return;

        if (_workflows.TryRemove(name, out _))
        {
            WorkflowProviderLog.WorkflowUnregistered(_logger, name);
        }
    }

    /// <summary>Returns true if a workflow is registered by name.</summary>
    public bool IsRegistered(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        if (_workflows.ContainsKey(name))
            return true;

        _indexLock.EnterReadLock();
        try
        {
            return _configuredIndex.WorkflowsByName.ContainsKey(name);
        }
        finally
        {
            _indexLock.ExitReadLock();
        }
    }

    /// <summary>
    /// Builds the configured index from IOptionsMonitor current values.
    /// </summary>
    private WorkflowIndex BuildConfiguredIndex()
    {
        if (_workflowOptions == null)
        {
            return new WorkflowIndex(
                new Dictionary<Guid, WorkflowConfiguration>(),
                new Dictionary<string, WorkflowConfiguration>(StringComparer.OrdinalIgnoreCase));
        }

        var workflows = _workflowOptions.CurrentValue ?? new List<WorkflowConfiguration>();

        var workflowsById = workflows
            .Where(w => w.Id != Guid.Empty)
            .ToDictionary(w => w.Id, w => w);

        var workflowsByName = workflows
            .Where(w => !string.IsNullOrWhiteSpace(w.Name))
            .ToDictionary(w => w.Name, w => w, StringComparer.OrdinalIgnoreCase);

        WorkflowProviderLog.WorkflowIndexRebuilt(_logger, workflows.Count);
        return new WorkflowIndex(workflowsById, workflowsByName);
    }

    /// <summary>
    /// Rebuilds the configured index in response to configuration changes.
    /// </summary>
    private void RebuildConfiguredIndex()
    {
        WorkflowProviderLog.ConfigurationChangeDetected(_logger);
        var newIndex = BuildConfiguredIndex();

        _indexLock.EnterWriteLock();
        try
        {
            _configuredIndex = newIndex;
        }
        finally
        {
            _indexLock.ExitWriteLock();
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
            return;

        _indexLock.Dispose();
        _disposed = true;
    }
}
