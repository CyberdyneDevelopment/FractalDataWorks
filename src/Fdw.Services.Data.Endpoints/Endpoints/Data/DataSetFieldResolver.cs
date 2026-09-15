using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data;

namespace Fdw.Services.Data.Endpoints;

/// <summary>Resolves a data set and one of its fields by name, for the stand-in and samples endpoints.</summary>
internal static class DataSetFieldResolver
{
    public static async Task<IGenericResult<(IDataSetImplementationConfiguration DataSet, DataSetFieldConfiguration Field)?>> Resolve(
        DataSetConfigurationProvider dataSets, string dataSetName, string fieldName, CancellationToken ct)
    {
        var dataSet = await dataSets.Get(dataSetName, ct).ConfigureAwait(false);
        if (dataSet.IsFailure) return dataSet.ToNewResult<(IDataSetImplementationConfiguration, DataSetFieldConfiguration)?>();
        if (dataSet.Value is null) return GenericResult<(IDataSetImplementationConfiguration, DataSetFieldConfiguration)?>.Success(null);

        var field = dataSet.Value.Fields.FirstOrDefault(f => string.Equals(f.Name, fieldName, StringComparison.OrdinalIgnoreCase));
        return GenericResult<(IDataSetImplementationConfiguration, DataSetFieldConfiguration)?>.Success(
            field is null ? null : (dataSet.Value, field));
    }
}
