using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Data;

/// <summary>
/// Reads a stored data set deep enough to know what it reads (FDW-5299): the one read the check of nested data sets and the status of a Graph over data sets share.
/// </summary>
internal static class GraphDataSetDefinitions
{
    /// <summary>Reads a data set by name; a Graph data set is read whole.</summary>
    /// <param name="dataSets">The data set domain's configuration provider.</param>
    /// <param name="name">The data set's name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The data set, a success with no value when it does not exist, or the failure of the read.</returns>
    /// <remarks>
    /// A Graph data set's stages sit a level below its components, and a lookup or compute table in them names a data set the engine reads, so a Graph is read whole. Every other
    /// kind holds what it reads on its own rows, one level down, so one read of that depth is enough; why not WholeAggregate for every name: the kind is only known once read.
    /// </remarks>
    internal static async Task<IGenericResult<IDataSetImplementationConfiguration>> Read(
        IDataSetConfigurationProvider dataSets, string name, CancellationToken cancellationToken)
    {
        var read = await dataSets.Get(name, ConfigurationLevels.Children, cancellationToken).ConfigureAwait(false);
        return read.IsFailure || read.Value is not IGraphDataSetConfiguration
            ? read
            : await dataSets.Get(name, ConfigurationLevels.WholeAggregate, cancellationToken).ConfigureAwait(false);
    }
}
