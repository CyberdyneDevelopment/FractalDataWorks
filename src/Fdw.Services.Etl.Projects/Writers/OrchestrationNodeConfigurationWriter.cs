using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Projects.Abstractions;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;
using Fdw.Services.Etl.Projects.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Projects.Writers;

/// <summary>
/// Validates and persists <see cref="OrchestrationNodeImplementationConfiguration"/> records.
/// Calls FluentValidation before persist, and invalidates the "pipe.OrchestrationNode" cache tag
/// after successful save or delete.
/// </summary>
public sealed class OrchestrationNodeConfigurationWriter
{
    private readonly IOrchestrationNodeConfigurationProvider _provider;
    private readonly IValidator<OrchestrationNodeImplementationConfiguration> _validator;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrchestrationNodeConfigurationWriter"/> class.
    /// </summary>
    public OrchestrationNodeConfigurationWriter(
        IOrchestrationNodeConfigurationProvider provider,
        IValidator<OrchestrationNodeImplementationConfiguration> validator,
        ILogger<OrchestrationNodeConfigurationWriter>? logger = null)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _logger = logger ?? NullLogger<OrchestrationNodeConfigurationWriter>.Instance;
    }

    /// <summary>
    /// Validates and persists the orchestration node configuration.
    /// </summary>
    public Task<IGenericResult<OrchestrationNodeImplementationConfiguration>> Save(
        OrchestrationNodeImplementationConfiguration config,
        CancellationToken cancellationToken = default)
        => Write(config, mayCreate: true, cancellationToken);

    /// <summary>
    /// Validates and persists a change to an orchestration node that exists; a node removed since it was read is not brought back.
    /// </summary>
    public Task<IGenericResult<OrchestrationNodeImplementationConfiguration>> Update(
        OrchestrationNodeImplementationConfiguration config,
        CancellationToken cancellationToken = default)
        => Write(config, mayCreate: false, cancellationToken);

    private async Task<IGenericResult<OrchestrationNodeImplementationConfiguration>> Write(
        OrchestrationNodeImplementationConfiguration config,
        bool mayCreate,
        CancellationToken cancellationToken)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));

        // Run FluentValidation before touching the database.
        var validationResult = await _validator.ValidateAsync(config, cancellationToken).ConfigureAwait(false);
        if (!validationResult.IsValid)
        {
            var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            return GenericResult<OrchestrationNodeImplementationConfiguration>.Failure(
                OrchestrationNodeConfigurationLog.ValidationFailed(_logger, "OrchestrationNode", config.Name, errors));
        }

        try
        {
            var saveResult = mayCreate
                ? await _provider.Save(config, "OrchestrationNode", config.Implementation, config.Name, cancellationToken).ConfigureAwait(false)
                : await _provider.Update(config, "OrchestrationNode", config.Implementation, config.Name, cancellationToken).ConfigureAwait(false);
            if (!saveResult.IsSuccess)
                return saveResult.ToNewResult<OrchestrationNodeImplementationConfiguration>();

            OrchestrationNodeConfigurationLog.NodeSaved(_logger, config.Name, config.Id);
            return GenericResult<OrchestrationNodeImplementationConfiguration>.Success(config);
        }
        catch (Exception ex)
        {
            return GenericResult<OrchestrationNodeImplementationConfiguration>.Failure(
                OrchestrationNodeConfigurationLog.NodeSaveFailed(_logger, ex, config.Name, ex.Message));
        }
    }

    /// <summary>
    /// Soft-deletes an orchestration node configuration by its logical identifier.
    /// </summary>
    public async Task<IGenericResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var deleteResult = await _provider.Delete(id, cancellationToken).ConfigureAwait(false);
            if (!deleteResult.IsSuccess)
                return deleteResult;

            return GenericResult.Success();
        }
        catch (Exception ex)
        {
            return GenericResult.Failure(
                OrchestrationNodeConfigurationLog.NodeDeleteFailed(_logger, ex, id, ex.Message));
        }
    }
}
