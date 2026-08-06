using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Calculations;
using Fdw.Services.Calculations.Abstractions;
using Fdw.Services.Calculations.Abstractions.CalculationSources;
using Fdw.Services.Calculations.Configuration;
using Fdw.Services.Calculations.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Calculations;

/// <summary>
/// Default implementation of <see cref="ICalculationEntityService"/>.
/// All persistence is routed through <see cref="CalculationConfigurationProvider"/>'s keystone
/// Get/Save: a single Get composes the full aggregate (header + Inputs + Steps + Formula/Windowed
/// typed body) and a single Save cascade-persists it. There is no hand-assembly, no Row types, and
/// no per-type typed-body load/save.
/// </summary>
#pragma warning disable MA0051
public sealed class CalculationEntityService : ICalculationEntityService
#pragma warning restore MA0051
{
    private readonly CalculationConfigurationProvider _provider;
    private readonly ICalculationInputResolver _inputResolver;
    private readonly ILogger<CalculationEntityService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="CalculationEntityService"/>.
    /// </summary>
    public CalculationEntityService(
        CalculationConfigurationProvider provider,
        ICalculationInputResolver inputResolver,
        ILogger<CalculationEntityService>? logger)
    {
        _provider = provider;
        _inputResolver = inputResolver;
        _logger = logger ?? NullLogger<CalculationEntityService>.Instance;
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<ICalculationEntity>> GetCalculation(
        string name,
        CancellationToken cancellationToken = default)
    {
        try
        {
            CalculationEntityLog.GetCalculationStarted(_logger, name);

            var result = await _provider.Get(name, cancellationToken).ConfigureAwait(false);
            if (!result.IsSuccess)
                return result.ToNewResult<ICalculationEntity>();

            if (result.Value is null)
                return GenericResult<ICalculationEntity>.Failure(
                    CalculationEntityLog.CalculationNotFound(_logger, name));

            CalculationEntityLog.GetCalculationSucceeded(_logger, name);
            return GenericResult<ICalculationEntity>.Success(MapToEntity(result.Value));
        }
        catch (Exception ex)
        {
            return GenericResult<ICalculationEntity>.Failure(
                CalculationEntityLog.CalculationLoadFailed(_logger, ex, name));
        }
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<ICalculationEntity>> GetCalculationById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var idString = id.ToString();
        try
        {
            CalculationEntityLog.GetCalculationStarted(_logger, idString);

            var result = await _provider.Get(id, cancellationToken).ConfigureAwait(false);
            if (!result.IsSuccess)
                return result.ToNewResult<ICalculationEntity>();

            if (result.Value is null)
                return GenericResult<ICalculationEntity>.Failure(
                    CalculationEntityLog.CalculationNotFound(_logger, idString));

            var entity = MapToEntity(result.Value);
            CalculationEntityLog.GetCalculationSucceeded(_logger, entity.Name);
            return GenericResult<ICalculationEntity>.Success(entity);
        }
        catch (Exception ex)
        {
            return GenericResult<ICalculationEntity>.Failure(
                CalculationEntityLog.CalculationLoadFailed(_logger, ex, idString));
        }
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<IReadOnlyList<ICalculationEntity>>> ListCalculations(
        CancellationToken cancellationToken = default)
    {
        try
        {
            CalculationEntityLog.ListCalculationsStarted(_logger);

            var headersResult = await _provider.Get(cancellationToken).ConfigureAwait(false);
            if (!headersResult.IsSuccess)
                return headersResult.ToNewResult<IReadOnlyList<ICalculationEntity>>();

            var headers = headersResult.Value ?? [];
            var entities = new List<ICalculationEntity>(headers.Count);
            foreach (var header in headers)
            {
                // Why: the list read returns headers only; compose each aggregate by Id so callers get the
                // full entity (Inputs + typed body), matching the prior per-row BuildEntity behaviour.
                var full = await _provider.Get(header.Id, cancellationToken).ConfigureAwait(false);
                if (!full.IsSuccess)
                    return full.ToNewResult<IReadOnlyList<ICalculationEntity>>();
                if (full.Value is not null)
                    entities.Add(MapToEntity(full.Value));
            }

            CalculationEntityLog.ListCalculationsSucceeded(_logger, entities.Count);
            return GenericResult<IReadOnlyList<ICalculationEntity>>.Success(entities);
        }
        catch (Exception ex)
        {
            return GenericResult<IReadOnlyList<ICalculationEntity>>.Failure(
                CalculationEntityLog.ListCalculationsFailed(_logger, ex));
        }
    }

    /// <inheritdoc/>
    public Task<IGenericResult> ValidateCalculation(
        ICalculationEntity entity,
        CancellationToken cancellationToken = default)
    {
        try
        {
            CalculationEntityLog.ValidateCalculationStarted(_logger, entity.Name);

            if (CalculationEntityTypes.ByName(entity.CalculationEntityType) == CalculationEntityTypes.NotFound)
            {
                return Task.FromResult<IGenericResult>(
                    GenericResult.Failure(
                        CalculationEntityLog.CalculationValidationFailed(
                            _logger,
                            entity.Name,
                            $"Unknown calculation entity type '{entity.CalculationEntityType}'")));
            }

            CalculationEntityLog.ValidateCalculationPassed(_logger, entity.Name);
            return Task.FromResult<IGenericResult>(GenericResult.Success());
        }
        catch (Exception ex)
        {
            return Task.FromResult<IGenericResult>(
                GenericResult.Failure(
                    CalculationEntityLog.ValidateCalculationFailed(_logger, ex, entity.Name)));
        }
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<ICalculationEntity>> CreateCalculation(
        string name,
        string? description,
        string calculationEntityType,
        IReadOnlyList<CalculationInput> inputs,
        CalculationOutputSpec output,
        IGenericConfiguration? typedConfiguration = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            CalculationEntityLog.CreateCalculationStarted(_logger, name);

            var build = BuildAggregate(Guid.Empty, name, description, calculationEntityType, inputs, output, true, typedConfiguration);
            if (!build.IsSuccess)
                return build.ToNewResult<ICalculationEntity>();

            var saveResult = await _provider.Save(build.Value!, cancellationToken).ConfigureAwait(false);
            if (!saveResult.IsSuccess)
                return saveResult.ToNewResult<ICalculationEntity>();

            CalculationEntityLog.CreateCalculationSucceeded(_logger, name, saveResult.Value!.Id);
            return GenericResult<ICalculationEntity>.Success(MapToEntity(saveResult.Value));
        }
        catch (Exception ex)
        {
            return GenericResult<ICalculationEntity>.Failure(
                CalculationEntityLog.CreateCalculationFailed(_logger, ex, name));
        }
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<ICalculationEntity>> UpdateCalculation(
        Guid id,
        string name,
        string? description,
        string calculationEntityType,
        IReadOnlyList<CalculationInput> inputs,
        CalculationOutputSpec output,
        bool isEnabled,
        IGenericConfiguration? typedConfiguration = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            CalculationEntityLog.UpdateCalculationStarted(_logger, id);

            var build = BuildAggregate(id, name, description, calculationEntityType, inputs, output, isEnabled, typedConfiguration);
            if (!build.IsSuccess)
                return build.ToNewResult<ICalculationEntity>();

            // Why: Save alone now version-on-writes (mints a new RowId) and cascades the WHOLE
            // aggregate on every write — the prior Delete-then-Save was a workaround for a Save
            // that used to update in place. Against the now fail-loud Delete (Delete errors when
            // the record doesn't exist instead of silently succeeding), that workaround would abort
            // every update outright.
            var saveResult = await _provider.Save(build.Value!, cancellationToken).ConfigureAwait(false);
            if (!saveResult.IsSuccess)
                return saveResult.ToNewResult<ICalculationEntity>();

            CalculationEntityLog.UpdateCalculationSucceeded(_logger, id);
            return GenericResult<ICalculationEntity>.Success(MapToEntity(saveResult.Value!));
        }
        catch (Exception ex)
        {
            return GenericResult<ICalculationEntity>.Failure(
                CalculationEntityLog.UpdateCalculationFailed(_logger, ex, id));
        }
    }

    /// <inheritdoc/>
    public async Task<IGenericResult> DeleteCalculation(
        Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            CalculationEntityLog.DeleteCalculationStarted(_logger, id);

            var deleteResult = await _provider.Delete(id, cancellationToken).ConfigureAwait(false);
            if (!deleteResult.IsSuccess)
                return deleteResult;

            CalculationEntityLog.DeleteCalculationSucceeded(_logger, id);
            return GenericResult.Success();
        }
        catch (Exception ex)
        {
            return GenericResult.Failure(
                CalculationEntityLog.DeleteCalculationFailed(_logger, ex, id));
        }
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<string>> ExecuteCalculation(
        string calculationName,
        ICalculationContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            CalculationEntityLog.ExecuteCalculationStarted(_logger, calculationName);

            var entityResult = await GetCalculation(calculationName, cancellationToken).ConfigureAwait(false);
            if (entityResult.IsFailure)
                return entityResult.ToNewResult<string>();

            var entity = entityResult.Value!;
            var entityType = CalculationEntityTypes.ByName(entity.CalculationEntityType);
            if (entityType == CalculationEntityTypes.NotFound)
            {
                return GenericResult<string>.Failure(
                    CalculationEntityLog.CalculationValidationFailed(
                        _logger,
                        calculationName,
                        $"Unknown calculation entity type '{entity.CalculationEntityType}'"));
            }

            var inputsResult = await _inputResolver.Resolve(entity.Inputs, context, cancellationToken).ConfigureAwait(false);
            if (inputsResult.IsFailure)
                return inputsResult.ToNewResult<string>();

            var executeResult = await entityType.Execute(entity, inputsResult.Value!, context, cancellationToken).ConfigureAwait(false);
            if (executeResult.IsFailure)
                return executeResult;

            CalculationEntityLog.ExecuteCalculationSucceeded(_logger, calculationName);
            return executeResult;
        }
        catch (Exception ex)
        {
            return GenericResult<string>.Failure(
                CalculationEntityLog.ExecuteCalculationFailed(_logger, ex, calculationName));
        }
    }

    // Why: builds the CalculationEntityConfiguration aggregate (header + Inputs + typed body) from request
    // primitives. The cascade sets each input's CalculationEntityId FK; the provider Save stamps the typed
    // body's FK. Fails loud when the entity type is unknown or an input omits its required Kind (NO FALLBACKS).
    private IGenericResult<CalculationEntityConfiguration> BuildAggregate(
        Guid id,
        string name,
        string? description,
        string calculationEntityType,
        IReadOnlyList<CalculationInput> inputs,
        CalculationOutputSpec output,
        bool isEnabled,
        IGenericConfiguration? typedConfiguration)
    {
        if (CalculationEntityTypes.ByName(calculationEntityType) == CalculationEntityTypes.NotFound)
            return GenericResult<CalculationEntityConfiguration>.Failure(
                CalculationEntityLog.TypedConfigurationSaveUnknownType(_logger, calculationEntityType));

        var inputRecords = new List<CalculationEntityInputRecord>(inputs.Count);
        for (var i = 0; i < inputs.Count; i++)
        {
            var input = inputs[i];
            // Why: InputKind is the required discriminator — a fabricated default is a silent fallback.
            if (input.Kind is null)
                return GenericResult<CalculationEntityConfiguration>.Failure(
                    CalculationEntityLog.CalculationValidationFailed(
                        _logger, input.InputAlias, "Calculation input Kind is required"));

            inputRecords.Add(new CalculationEntityInputRecord
            {
                InputAlias = input.InputAlias,
                InputKind = input.Kind.Name,
                DataSetName = input.DataSetName,
                ConnectionName = input.ConnectionName,
                ContainerPath = input.ContainerPath,
                ScalarValueTypeName = input.ScalarValue?.ValueType?.Name,
                ScalarValue = input.ScalarValue?.SerializedValue,
                Ordinal = i,
                IsCurrent = true,
                IsDeleted = false
            });
        }

        ICalculationTypedConfiguration? typedBody = null;
        if (typedConfiguration is not null)
        {
            // Why: a calc typed body must implement ICalculationTypedConfiguration (Formula/Windowed). A
            // non-conforming body is a defect, not a silently-dropped value (NO FALLBACKS).
            if (typedConfiguration is not ICalculationTypedConfiguration tc)
                return GenericResult<CalculationEntityConfiguration>.Failure(
                    CalculationEntityLog.CalculationValidationFailed(
                        _logger, name,
                        $"Typed configuration '{typedConfiguration.GetType().Name}' does not implement ICalculationTypedConfiguration"));
            tc.Id = Guid.Empty;
            typedBody = tc;
        }

        return GenericResult<CalculationEntityConfiguration>.Success(new CalculationEntityConfiguration
        {
            Id = id,
            Name = name,
            Description = description,
            CalculationEntityType = calculationEntityType,
            // Why: stamps provenance — this is the one built-in write path for calc.CalculationEntity,
            // so every row it persists is owned by the Configuration source. Couples to the generated
            // option's own Name rather than a bare "Configuration" literal (NO FALLBACKS).
            CalculationSource = CalculationSourceTypes.Configuration.Name,
            OutputDataSetName = output.OutputDataSetName,
            ResultFieldName = output.ResultFieldName,
            ResultDataTypeName = output.ResultDataTypeName,
            IsEnabled = isEnabled,
            Inputs = inputRecords,
            Configuration = typedBody
        });
    }

    private static CalculationEntity MapToEntity(CalculationEntityConfiguration config)
    {
        var output = new CalculationOutputSpec
        {
            OutputDataSetName = config.OutputDataSetName ?? string.Empty,
            ResultFieldName = config.ResultFieldName ?? string.Empty,
            ResultDataTypeName = config.ResultDataTypeName
        };

        return new CalculationEntity
        {
            Id = config.Id,
            Name = config.Name,
            Description = config.Description,
            CalculationEntityType = config.CalculationEntityType,
            CalculationSource = config.CalculationSource,
            Inputs = config.Inputs.Select(MapInputRecordToModel).ToList(),
            // Why: steps compose as part of the aggregate; carry them on the runtime entity as their
            // composed config (Fields/Operands included). Execution does not consume them yet (out of scope).
            Steps = config.Steps.Cast<IGenericConfiguration>().ToList(),
            Output = output,
            IsEnabled = config.IsEnabled,
            TypedConfiguration = config.Configuration
        };
    }

    private static CalculationInput MapInputRecordToModel(CalculationEntityInputRecord record)
    {
        CalculationScalarValue? scalarValue = null;
        if (record.ScalarValueTypeName is not null && record.ScalarValue is not null)
        {
            scalarValue = new CalculationScalarValue
            {
                ValueType = ScalarValueTypes.ByName(record.ScalarValueTypeName),
                SerializedValue = record.ScalarValue
            };
        }

        return new CalculationInput
        {
            Kind = CalculationInputKinds.ByName(record.InputKind),
            DataSetName = record.DataSetName,
            ConnectionName = record.ConnectionName,
            ContainerPath = record.ContainerPath,
            ScalarValue = scalarValue,
            InputAlias = record.InputAlias
        };
    }
}
