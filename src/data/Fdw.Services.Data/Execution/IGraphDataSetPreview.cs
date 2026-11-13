using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Services.Data.Execution;

/// <summary>
/// Reads the rows of a Graph data set draft that is not stored (FDW-5454).
/// </summary>
/// <remarks>
/// The draft is run as a read of a stored one is, through the gateway it is handed, so every source is read for the caller with that caller's row rules,
/// but with a cap on the rows each source gives and, when asked, at a component's output in the middle of the graph with only what feeds it run. Nothing
/// is stored and no run record is kept. A run that cannot finish answers a named failure and never the rows read so far. It is separate from
/// <see cref="IGraphDataSetRunner"/>, which the gateway holds for a stored data set.
/// </remarks>
public interface IGraphDataSetPreview
{
    /// <summary>Reads the rows of a draft.</summary>
    /// <param name="gateway">The gateway the draft's sources are read through.</param>
    /// <param name="draft">The Graph data set draft, read from the body of a create request.</param>
    /// <param name="request">The cap on the rows each source gives and where to read.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The rows, or the named failure that says why the preview cannot be answered.</returns>
    Task<IGenericResult<IReadOnlyList<IDictionary<string, object?>>>> Preview(
        IDataGateway gateway, IGraphDataSetConfiguration draft, GraphPreviewRequest request, CancellationToken cancellationToken = default);
}
