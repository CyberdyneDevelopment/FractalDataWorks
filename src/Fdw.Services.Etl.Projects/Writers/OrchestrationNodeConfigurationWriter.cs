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
/// Validates and persists <see cref="OrchestrationNodeConfiguration"/> records.
/// Calls FluentValidation before persist, and invalidates the "pipe.OrchestrationNode" cache tag
/// after successful save or delete.
/// </summary>
public sealed class OrchestrationNodeConfigurationWriter
{
    private readonly IOrchestrationNodeConfigurationProvider _provider;
    private readonly IValidator<OrchestrationNodeConfiguration> _validator;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrchestrationNodeConfigurationWriter"/> class.
    /// </summary>
    public OrchestrationNodeConfigurationWriter(
        IOrchestrationNodeConfigurationProvider provider,
        IValidator<OrchestrationNodeConfiguration> validator,
        ILogger<OrchestrationNodeConfigurationWriter>? logger = null)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _logger = logger ?? NullLogger<OrchestrationNodeConfigurationWriter>.Instance;
    }

    /// <summary>
    /// Validates and persists the orchestration node configuration.
    /// </summary>
    public async Task<IGenericResult<OrchestrationNodeConfiguration>> Save(
        OrchestrationNodeConfiguration config,
        CancellationToken cancellationToken = default)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));

        // Run FluentValidation before touching the database.
        var validationResult = await _validator.ValidateAsync(config, cancellationToken).ConfigureAwait(false);
        if (!validationResult.IsValid)
        {
            var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            return GenericResult<OrchestrationNodeConfiguration>.Failure(
                OrchestrationNodeConfigurationLog.ValidationFailed(_logger, "OrchestrationNode", config.Name, errors));
        }

        try
        {
            var saveResult = await _provider.Save(config, cancellationToken).ConfigureAwait(false);
            if (!saveResult.IsSuccess)
                return saveResult.ToNewResult<OrchestrationNodeConfiguration>();

            OrchestrationNodeConfigurationLog.NodeSaved(_logger, config.Name, config.Id);
            return GenericResult<OrchestrationNodeConfiguration>.Success(saveResult.Value!);
        }
        catch (Exception ex)
        {
            return GenericResult<OrchestrationNodeConfiguration>.Failure(
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
