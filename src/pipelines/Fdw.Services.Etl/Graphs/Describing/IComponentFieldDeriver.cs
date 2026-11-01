using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Results;

namespace Fdw.Services.Etl.Graphs.Describing;

/// <summary>
/// Works out the fields at the output nodes of one kind of component from the fields at its input nodes and its settings (FDW-1397).
/// One deriver per <c>ComponentTypes</c> option, found by the option's name.
/// </summary>
public interface IComponentFieldDeriver : ITypeOption<int, IComponentFieldDeriver>
{
    /// <summary>Derives the fields at each output node.</summary>
    /// <param name="component">The stored component.</param>
    /// <param name="inputs">The fields at each input node that is linked, by node name; <see langword="null"/> where they cannot be known.</param>
    /// <param name="context">What describing needs from outside the graph.</param>
    /// <param name="cancellationToken">Cancels the derivation.</param>
    /// <returns>The fields at each output node, by node name; <see langword="null"/> where they cannot be known; or the failure to read a data set.</returns>
    Task<IGenericResult<IReadOnlyDictionary<string, IReadOnlyList<GraphField>?>>> Derive(
        PipelineComponentConfiguration component,
        IReadOnlyDictionary<string, IReadOnlyList<GraphField>?> inputs,
        DescribeContext context,
        CancellationToken cancellationToken = default);
}
