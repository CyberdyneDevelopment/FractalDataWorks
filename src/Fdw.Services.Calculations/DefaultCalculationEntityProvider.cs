using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions;
using Fdw.Services.Calculations.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Calculations;

/// <summary>
/// Default in-memory registry of <see cref="ICalculationEntity"/> instances.
/// Populated at startup or on-demand via <see cref="Register"/>.
/// </summary>
/// <remarks>
/// This provider performs no I/O. Retrieval of calculation entities from the database
/// is the responsibility of <see cref="ICalculationEntityService"/>.
/// The registry uses case-insensitive name comparison consistent with FDW conventions.
/// </remarks>
public sealed class DefaultCalculationEntityProvider : ICalculationEntityProvider
{
    private readonly ICalculationEntityService _service;
    private readonly ILogger<DefaultCalculationEntityProvider> _logger;
    private readonly Dictionary<string, ICalculationEntity> _byName;
    private readonly Dictionary<Guid, ICalculationEntity> _byId;

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultCalculationEntityProvider"/>.
    /// </summary>
    /// <param name="service">The service used to load entities from the database when needed.</param>
    /// <param name="logger">The logger instance. Falls back to NullLogger if null.</param>
    public DefaultCalculationEntityProvider(
        ICalculationEntityService service,
        ILogger<DefaultCalculationEntityProvider>? logger)
    {
        _service = service;
        _logger = logger ?? NullLogger<DefaultCalculationEntityProvider>.Instance;
        _byName = new Dictionary<string, ICalculationEntity>(StringComparer.OrdinalIgnoreCase);
        _byId = new Dictionary<Guid, ICalculationEntity>();
    }

    /// <inheritdoc/>
    public Task<IGenericResult<ICalculationEntity>> Get(string name, CancellationToken cancellationToken = default)
    {
        if (_byName.TryGetValue(name, out var entity))
        {
            return Task.FromResult(GenericResult<ICalculationEntity>.Success(entity));
        }

        return Task.FromResult(GenericResult<ICalculationEntity>.Failure(
            CalculationEntityLog.CalculationNotFound(_logger, name)));
    }

    /// <inheritdoc/>
    public Task<IGenericResult<ICalculationEntity>> Get(Guid id, CancellationToken cancellationToken = default)
    {
        if (_byId.TryGetValue(id, out var entity))
        {
            return Task.FromResult(GenericResult<ICalculationEntity>.Success(entity));
        }

        return Task.FromResult(GenericResult<ICalculationEntity>.Failure(
            CalculationEntityLog.CalculationNotFound(_logger, id.ToString())));
    }

    /// <inheritdoc/>
    public Task<IGenericResult<IReadOnlyList<ICalculationEntity>>> Get(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ICalculationEntity> result = new List<ICalculationEntity>(_byName.Values);
        return Task.FromResult(GenericResult<IReadOnlyList<ICalculationEntity>>.Success(result));
    }

    /// <summary>Registers a calculation entity by name and ID.</summary>
    public void Register(ICalculationEntity entity)
    {
        _byName[entity.Name] = entity;
        _byId[entity.Id] = entity;
    }

    /// <summary>Unregisters a calculation entity by name.</summary>
    public void UnregisterCalculation(string name)
    {
        if (_byName.TryGetValue(name, out var entity))
        {
            _byName.Remove(name);
            _byId.Remove(entity.Id);
        }
    }

    /// <summary>Returns true if a calculation entity is registered by name.</summary>
    public bool IsCalculationRegistered(string name)
    {
        return _byName.ContainsKey(name);
    }
}
