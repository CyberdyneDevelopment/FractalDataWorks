using Fdw.Configuration;
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
public class DataSetConfigurationProvider : ImplementationConfigurationProviderBase<IDataSetImplementationConfiguration>
{


    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="DataSetConfigurationProvider"/> class.</summary>
    public DataSetConfigurationProvider(
        ILogger<DataSetConfigurationProvider>? logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName = "data")
        : base(logger ?? NullLogger<DataSetConfigurationProvider>.Instance,
               gatewayProvider,
               dataStoreName, pathName,
               "DataSet")
    {
        _logger = logger ?? NullLogger<DataSetConfigurationProvider>.Instance;
    }

    // ============================================================================
    // Get overrides — compose FieldMappings after base populates Sources
    // ============================================================================

    /// <inheritdoc />
    public override async Task<IGenericResult<DataSetImplementationConfiguration>> Get(string name, CancellationToken ct = default)
    {
        var result = await base.Get(name, ct).ConfigureAwait(false);
        if (result.IsSuccess && result.Value is not null)
        {
            var mappingResult = await PopulateFieldMappings(result.Value, ct).ConfigureAwait(false);
            if (!mappingResult.IsSuccess)
                return mappingResult.ToNewResult<DataSetImplementationConfiguration>();
        }
        return result;
    }

    /// <inheritdoc />
    public override async Task<IGenericResult<DataSetImplementationConfiguration>> Get(Guid id, CancellationToken ct = default)
    {
        var result = await base.Get(id, ct).ConfigureAwait(false);
        if (result.IsSuccess && result.Value is not null)
        {
            var mappingResult = await PopulateFieldMappings(result.Value, ct).ConfigureAwait(false);
            if (!mappingResult.IsSuccess)
                return mappingResult.ToNewResult<DataSetImplementationConfiguration>();
        }
        return result;
    }

    /// <inheritdoc />
    public override async Task<IGenericResult<IReadOnlyList<DataSetImplementationConfiguration>>> Get(CancellationToken ct = default)
    {
        var result = await base.Get(ct).ConfigureAwait(false);
        if (result.IsSuccess && result.Value is not null)
        {
            foreach (var config in result.Value)
            {
                var mappingResult = await PopulateFieldMappings(config, ct).ConfigureAwait(false);
                if (!mappingResult.IsSuccess)
                    return mappingResult.ToNewResult<IReadOnlyList<DataSetImplementationConfiguration>>();
            }
        }
        return result;
    }


    // ============================================================================
    // Field mapping composition
    // ============================================================================

    private const string FieldMappingContainer = "DataSetFieldMapping";


    // ============================================================================
    // Fields read/write
    // ============================================================================

    private const string FieldContainer = "DataSetField";



}
