using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Services.Etl.Flow;

namespace Fdw.Services.Etl.Graphs.Planning;

/// <summary>
/// A data set source (FDW-6970): never pushed into a store with other reads, as it is read through its data set's own read. For a combiner across stores
/// it is a read of that data set.
/// </summary>
[TypeOption(typeof(PushdownParts), "DataSetSource")]
public sealed class DataSetSourcePushdownPart : PushdownPartBase
{
    /// <summary>Initializes a new instance of the <see cref="DataSetSourcePushdownPart"/> class.</summary>
    public DataSetSourcePushdownPart() : base("DataSetSource", false, FlowGatewaySource.OutputName, string.Empty)
    {
    }

    /// <inheritdoc />
    public override Task<string> ReadStore(PushdownScope scope, PipelineComponentConfiguration component, CancellationToken cancellationToken = default) =>
        Task.FromResult(component.DataSetName is { Length: > 0 } dataSet ? $"data set {dataSet}" : string.Empty);

    /// <inheritdoc />
    public override Task<PushdownAttempt> Resolve(
        PushdownScope scope, PipelineComponentConfiguration component, bool top, CancellationToken cancellationToken = default) =>
        Task.FromResult(PushdownAttempt.Stays("it reads a data set, which is read through that data set's own read"));
}
