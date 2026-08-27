using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Conventions;
using Fdw.Data.DataSets;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Commands;
using Fdw.Services.Data.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using CmdBuilders = Fdw.Commands.Data.Extensions;

namespace Fdw.Services.Data;

/// <summary>
/// Domain-specific configuration provider for DataSet configurations.
/// Overrides Get/GetAll to assemble the DataSet → DataSetSource/Field/KeyField hierarchy
/// after base resolution, and composes FieldMappings onto each source.
/// </summary>
public class DataSetConfigurationProvider : ImplementationConfigurationProviderBase<DataSetConfiguration, DataSetConfigurationCommand>
{
    /// <summary>
    /// Registers the DataSetConfigurationProvider with DI, targeting this domain's own default
    /// location. To override, call <c>SetConfiguration</c> on the resolved singleton.
    /// </summary>
    public static void RegisterDomainConfiguration(IServiceCollection services)
    {
        // Why named here rather than defaulted on the constructor: a defaulted connection is one a
        // caller inherits without saying so, and this provider is registered directly rather than by
        // a collection that would otherwise name it.
        const string dataStoreName = "PlatformConfiguration";
        const string pathName = "data";

        services.TryAddSingleton<DataSetConfigurationProvider>(sp =>
            new DataSetConfigurationProvider(
                sp.GetService<ILogger<DataSetConfigurationProvider>>(),
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                dataStoreName, pathName));
        services.TryAddSingleton<ImplementationConfigurationProviderBase<DataSetConfiguration, DataSetConfigurationCommand>>(
            sp => sp.GetRequiredService<DataSetConfigurationProvider>());
        services.TryAddSingleton<IServiceConfigurationProvider<DataSetConfiguration>>(
            sp => sp.GetRequiredService<DataSetConfigurationProvider>());
    }

    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="DataSetConfigurationProvider"/> class.</summary>
    public DataSetConfigurationProvider(
        ILogger<DataSetConfigurationProvider>? logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName = "data")
        : base(logger ?? NullLogger<DataSetConfigurationProvider>.Instance,
               gatewayProvider,
               dataStoreName, pathName)
    {
        _logger = logger ?? NullLogger<DataSetConfigurationProvider>.Instance;
    }

    // ============================================================================
    // Get overrides — compose FieldMappings after base populates Sources
    // ============================================================================

    /// <inheritdoc />
    public override async Task<IGenericResult<DataSetConfiguration>> Get(string name, CancellationToken ct = default)
    {
        var result = await base.Get(name, ct).ConfigureAwait(false);
        if (result.IsSuccess && result.Value is not null)
        {
            var mappingResult = await PopulateFieldMappings(result.Value, ct).ConfigureAwait(false);
            if (!mappingResult.IsSuccess)
                return mappingResult.ToNewResult<DataSetConfiguration>();
        }
        return result;
    }

    /// <inheritdoc />
    public override async Task<IGenericResult<DataSetConfiguration>> Get(Guid id, CancellationToken ct = default)
    {
        var result = await base.Get(id, ct).ConfigureAwait(false);
        if (result.IsSuccess && result.Value is not null)
        {
            var mappingResult = await PopulateFieldMappings(result.Value, ct).ConfigureAwait(false);
            if (!mappingResult.IsSuccess)
                return mappingResult.ToNewResult<DataSetConfiguration>();
        }
        return result;
    }

    /// <inheritdoc />
    public override async Task<IGenericResult<IReadOnlyList<DataSetConfiguration>>> Get(CancellationToken ct = default)
    {
        var result = await base.Get(ct).ConfigureAwait(false);
        if (result.IsSuccess && result.Value is not null)
        {
            foreach (var config in result.Value)
            {
                var mappingResult = await PopulateFieldMappings(config, ct).ConfigureAwait(false);
                if (!mappingResult.IsSuccess)
                    return mappingResult.ToNewResult<IReadOnlyList<DataSetConfiguration>>();
            }
        }
        return result;
    }

    // Why there is no Save override: it existed only to force base.Save down its INSERT branch, because the
    // base cascaded children on INSERT alone — so a DataSet had to be tombstoned and re-created to get its
    // Sources/Fields/KeyFields rewritten. The base now has ONE write path (version-on-write, cascade always),
    // so the delete-then-save is not just redundant but wrong: it retired the aggregate before every save,
    // and against the now fail-loud Delete it would abort any save that supplied an Id for a record that did
    // not exist yet.

    // ============================================================================
    // Field mapping composition
    // ============================================================================

    private const string FieldMappingContainer = "DataSetFieldMapping";

    /// <summary>
    /// Queries DataSetFieldMapping rows for each source in the config and populates
    /// <see cref="DataSetSourceConfiguration.FieldMappings"/> and
    /// <see cref="DataSetSourceConfiguration.FieldMappingIds"/> in-place.
    /// Returns a failure result if any gatewayProvider query fails — the caller propagates this so the
    /// composed DataSet config is never returned with silently empty field mappings.
    /// </summary>
    private async Task<IGenericResult<bool>> PopulateFieldMappings(DataSetConfiguration config, CancellationToken ct)
    {
        if (config.Sources == null || config.Sources.Count == 0)
            return GenericResult<bool>.Success(true);

        DataSetConfigurationProviderLog.ChildHierarchyLoaded(_logger, config.Name, config.Sources.Count);

        foreach (var source in config.Sources)
        {
            var command = new QueryCommandBuilder<DataSetFieldMappingConfiguration>(DataStoreName, PathName, FieldMappingContainer)
                .Where("DataSetSourceId", source.Id)
                .Where("IsCurrent", true)
                .Where("IsDeleted", false)
                .Build();

            var gateway = Gateway();
            if (gateway.IsFailure) return gateway.ToNewResult<bool>();

            var result = await gateway.Value!.Execute<IEnumerable<DataSetFieldMappingConfiguration>>(command, ct).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                // Why: fail loud — returning a composed config with silently empty FieldMappings would
                // mask a gatewayProvider error. The caller's Get override propagates this failure so the
                // caller receives a non-success result rather than a partially-composed DataSet.
                return GenericResult<bool>.Failure(
                    DataSetConfigurationProviderLog.FieldMappingQueryFailed(
                        _logger, source.SourceName, config.Name,
                        new InvalidOperationException(result.CurrentMessage)));
            }

            var mappings = result.Value?.ToList() ?? [];
            source.FieldMappingIds = mappings.Select(m => m.Id).ToList();
            // Why: FieldMappings is logical→physical; only include entries where PhysicalFieldName
            // is non-null (SourceKind='DataStore'). SourceKind='DataSet' or 'Calculation' bindings
            // have null PhysicalFieldName and are resolved by the execution layer, not field rename.
            source.FieldMappings = mappings
                .Where(m => !string.IsNullOrEmpty(m.PhysicalFieldName))
                .ToDictionary(m => m.LogicalFieldName, m => m.PhysicalFieldName!, StringComparer.Ordinal);

            DataSetConfigurationProviderLog.FieldMappingsLoaded(_logger, source.SourceName, mappings.Count);
        }

        var totalMappings = config.Sources.Sum(s => s.FieldMappingIds?.Count ?? 0);
        DataSetConfigurationProviderLog.HierarchyAssembled(_logger, config.Name, config.Sources.Count, totalMappings);
        return GenericResult<bool>.Success(true);
    }

    // ============================================================================
    // Fields read/write
    // ============================================================================

    private const string FieldContainer = "DataSetField";

    /// <summary>
    /// Reads the current field schema for a DataSet from ConfigurationDb.
    /// </summary>
    /// <param name="dataSetId">The DataSet logical identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Ordered list of field definitions, or a failure result.</returns>
    public virtual async Task<IGenericResult<IReadOnlyList<DataSetFieldDefinition>>> GetFields(
        Guid dataSetId, CancellationToken cancellationToken = default)
    {
        DataSetConfigurationProviderLog.GetFieldsTrace(_logger, dataSetId);

        var command = new QueryCommandBuilder<DataFieldConfiguration>(DataStoreName, PathName, FieldContainer)
            .Where("DataSetId", dataSetId)
            .Where("IsCurrent", true)
            .Where("IsDeleted", false)
            .OrderBy("Ordinal")
            .Build();

        var gateway = Gateway();
        if (gateway.IsFailure) return gateway.ToNewResult<IReadOnlyList<DataSetFieldDefinition>>();

        var result = await gateway.Value!.Execute<IEnumerable<DataFieldConfiguration>>(command, cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return GenericResult<IReadOnlyList<DataSetFieldDefinition>>.Failure(
                DataSetConfigurationProviderLog.GetFieldsFailed(_logger, dataSetId,
                    new InvalidOperationException(result.CurrentMessage)));
        }

        // Why: DataFieldConfiguration uses Name/TypeName matching the DDL columns [Name] and TypeName.
        // DataSetFieldDefinition uses FieldName/ScalarTypeName as the public contract. Project here so
        // callers never see the database column mismatch.
        var fields = result.Value!.Select(c => new DataSetFieldDefinition
        {
            DataSetId = c.DataSetId,
            FieldName = c.Name,
            ScalarTypeName = c.TypeName,
            IsNullable = !c.IsRequired,
            Ordinal = c.Ordinal,
            Description = c.Description
        }).ToList();

        DataSetConfigurationProviderLog.GetFieldsLoaded(_logger, dataSetId, fields.Count);
        return GenericResult<IReadOnlyList<DataSetFieldDefinition>>.Success(fields);
    }

    /// <summary>
    /// Saves the field schema for a DataSet using version-on-write semantics.
    /// Existing current rows are retired (IsCurrent=false) before new rows are inserted.
    /// </summary>
    /// <param name="dataSetId">The DataSet logical identifier.</param>
    /// <param name="fields">The new field definitions to persist.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success, or a failure result with the cause.</returns>
    [ConventionOverride(MaxMethodLines = 60)]
    public virtual async Task<IGenericResult> SaveFields(
        Guid dataSetId,
        IReadOnlyList<DataSetFieldDefinition> fields,
        CancellationToken cancellationToken = default)
    {
        DataSetConfigurationProviderLog.SaveFieldsTrace(_logger, dataSetId, fields.Count);

        // Version-on-write step 1: retire existing current field rows
        var retireCommand = CmdBuilders.Update.In<DataFieldConfiguration>(FieldContainer)
            .DataStore(DataStoreName).Path(PathName)
            .Where("DataSetId", dataSetId)
            .Where("IsCurrent", true)
            .Where("IsDeleted", false)
            .Value(new DataFieldConfiguration { DataSetId = dataSetId, IsCurrent = false });

        var gatewayForSave = Gateway();
        if (gatewayForSave.IsFailure) return gatewayForSave;

        var retireResult = await gatewayForSave.Value!.Execute<int>(retireCommand, cancellationToken)
            .ConfigureAwait(false);

        if (!retireResult.IsSuccess)
        {
            return GenericResult.Failure(
                DataSetConfigurationProviderLog.RetireFieldsFailed(_logger, dataSetId,
                    new InvalidOperationException(retireResult.CurrentMessage)));
        }

        if (fields.Count == 0)
        {
            DataSetConfigurationProviderLog.SaveFieldsSaved(_logger, dataSetId);
            return GenericResult.Success();
        }

        // Why: DataFieldConfiguration columns are Name/TypeName; DataSetFieldDefinition uses FieldName/ScalarTypeName.
        // Project the inverse mapping here so the DDL columns receive correct values.
        // Id is app-minted (Guid.CreateVersion7()) per DDL convention — no DEFAULT on the Id column.
        //
        // Why: persist via ConfigurationSaveCommand<T> (not a raw insert). data.DataSetField.DataSetRowId
        // is the NOT NULL physical FK to data.DataSet(RowId); only MsSqlConfigurationSaveTranslator
        // resolves it from the logical DataSetId via subquery on insert. A raw insert omits it (SQL 515).
        foreach (var f in fields)
        {
            var record = new DataFieldConfiguration
            {
                Id = Guid.CreateVersion7(),
                DataSetId = dataSetId,
                Name = f.FieldName,
                TypeName = f.ScalarTypeName,
                IsRequired = !f.IsNullable,
                Ordinal = f.Ordinal,
                Description = f.Description,
                IsCurrent = true,
                IsDeleted = false
            };
            var insertResult = await gatewayForSave.Value!.Execute<int>(
                new ConfigurationSaveCommand<DataFieldConfiguration>(record),
                new DataStoreTarget(DataStoreName, PathName, FieldContainer), cancellationToken)
                .ConfigureAwait(false);
            if (!insertResult.IsSuccess)
            {
                return GenericResult.Failure(
                    DataSetConfigurationProviderLog.InsertFieldsFailed(_logger, dataSetId,
                        new InvalidOperationException(insertResult.CurrentMessage)));
            }
        }

        DataSetConfigurationProviderLog.SaveFieldsSaved(_logger, dataSetId);
        return GenericResult.Success();
    }

    /// <inheritdoc />
    /// <remarks>
    /// A DataSet has no parent. DataSetConfiguration is declared with no parent type — a top-level
    /// named configuration like a Connection or a Pipeline — so there is nothing to look for and this
    /// does not look.
    ///
    /// data.DataSet does carry a foreign key, to data.DataSetCategory. A category is something a
    /// dataset cites, not something it belongs to, and the base cannot tell those apart from the
    /// constraint alone: it read the citation as a parent, built a parent-join query, and refused to
    /// resolve any dataset by name.
    /// </remarks>
    protected override IGenericResult<ParentJoinInfo> ResolveParentJoin()
        => GenericResult<ParentJoinInfo>.Success(ParentJoinInfo.None);
}
