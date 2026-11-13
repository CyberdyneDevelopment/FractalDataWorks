using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>Resolves a data set and one or all of its fields by name, for the stand-in and samples endpoints.</summary>
internal static class DataSetFieldResolver
{
    public static async Task<IGenericResult<(IDataSetImplementationConfiguration DataSet, DataSetFieldConfiguration Field)?>> Resolve(
        IDataSetConfigurationProvider dataSets, string dataSetName, string fieldName, Func<IDataSetImplementationConfiguration, string, ILogger, IGenericResult> access, ILogger logger, CancellationToken ct)
    {
        var dataSet = await dataSets.Get(dataSetName, ct).ConfigureAwait(false);
        if (dataSet.IsFailure) return dataSet.ToNewResult<(IDataSetImplementationConfiguration, DataSetFieldConfiguration)?>();
        if (dataSet.Value is null) return GenericResult<(IDataSetImplementationConfiguration, DataSetFieldConfiguration)?>.Success(null);

        // Why before the field is looked for: a kind that has no rows of this use for its fields refuses the use by name, however the field is spelled (FDW-5505).
        var allowed = access(dataSet.Value, fieldName, logger);
        if (allowed.IsFailure) return allowed.ToNewResult<(IDataSetImplementationConfiguration, DataSetFieldConfiguration)?>();

        var field = dataSet.Value.Fields.FirstOrDefault(f => string.Equals(f.Name, fieldName, StringComparison.OrdinalIgnoreCase));
        return GenericResult<(IDataSetImplementationConfiguration, DataSetFieldConfiguration)?>.Success(
            field is null ? null : (dataSet.Value, field));
    }

    /// <summary>Resolves a data set and returns all of its fields in ordinal order, or null when the data set does not exist.</summary>
    public static async Task<IGenericResult<IReadOnlyList<DataSetFieldConfiguration>?>> ResolveAll(
        IDataSetConfigurationProvider dataSets, string dataSetName, Func<IDataSetImplementationConfiguration, ILogger, IGenericResult> access, ILogger logger, CancellationToken ct)
    {
        var dataSet = await dataSets.Get(dataSetName, ct).ConfigureAwait(false);
        if (dataSet.IsFailure) return dataSet.ToNewResult<IReadOnlyList<DataSetFieldConfiguration>?>();
        if (dataSet.Value is null) return GenericResult<IReadOnlyList<DataSetFieldConfiguration>?>.Success(null);

        var allowed = access(dataSet.Value, logger);
        if (allowed.IsFailure) return allowed.ToNewResult<IReadOnlyList<DataSetFieldConfiguration>?>();

        return GenericResult<IReadOnlyList<DataSetFieldConfiguration>?>.Success(dataSet.Value.Fields.OrderBy(f => f.Ordinal).ToList());
    }
}
