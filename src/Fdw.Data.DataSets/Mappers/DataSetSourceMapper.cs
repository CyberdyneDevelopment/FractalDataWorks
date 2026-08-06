using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Data.DataSets;

/// <summary>
/// TypeOption that represents a source binding to another DataSet within the same compound/federated composition.
/// This is a stub implementation — full execution requires the upstream DataSet execution engine which resolves
/// rows by executing the source DataSet and applying join predicates configured on the parent DataSetSource.
/// </summary>
[TypeOption(typeof(DataSetSourceMapperTypes), "DataSet")]
public sealed class DataSetSourceMapper : DataSetSourceMapperTypeBase
{
    // Why: TypeOptions are singletons discovered by source generation — they have no DI-injected logger.
    // NullLogger ensures MessageLogging methods can create IGenericMessage instances for results.
    // The message content is still returned in the IGenericResult for the caller to observe.
    private static readonly ILogger Logger = NullLogger.Instance;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataSetSourceMapper"/> class.
    /// </summary>
    public DataSetSourceMapper()
        : base(
            id: 2,
            name: "DataSet",
            displayName: "DataSet",
            description: "Resolves rows from another DataSet within the same compound/federated composition.",
            category: "Mapper")
    {
    }

    /// <inheritdoc/>
    public override Task<IGenericResult<IReadOnlyList<Dictionary<string, object?>>>> MapRecords(
        DataSetSourceMapperContext context,
        CancellationToken cancellationToken = default)
    {
        // Why: DataSet mapper resolves via compound/federated join. The runtime expects this
        // to return rows from the source DataSet; actual data resolution happens at composition time.
        // This is a placeholder — the compound execution engine calls the upstream DataSet
        // and extracts matching rows based on join predicates configured on the parent DataSetSource.
        return Task.FromResult<IGenericResult<IReadOnlyList<Dictionary<string, object?>>>>(
            GenericResult<IReadOnlyList<Dictionary<string, object?>>>.Failure(
                DataSetSourceMapperLog.DataSetMapperNotYetImplemented(Logger, Name)));
    }
}
