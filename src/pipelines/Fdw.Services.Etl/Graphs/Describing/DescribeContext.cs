using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Etl.Graphs.Describing;

/// <summary>
/// What describing a graph needs from outside it (FDW-1397): the fields of a data set, and (FDW-5523) the fields of a container.
/// </summary>
public sealed class DescribeContext
{
    /// <summary>Initializes a new instance of the <see cref="DescribeContext"/> class.</summary>
    /// <param name="dataSetFields">Reads the fields of a data set by name, in order.</param>
    /// <param name="containerFields">Reads the fields of a container a ContainerSource reads, in order; none when the caller has no way to read a container's stored fields, and then a container source's output is not known.</param>
    public DescribeContext(
        Func<string, CancellationToken, Task<IGenericResult<IReadOnlyList<GraphField>>>> dataSetFields,
        Func<ContainerFieldsRequest, CancellationToken, Task<IGenericResult<IReadOnlyList<GraphField>>>>? containerFields = null)
    {
        DataSetFields = dataSetFields;
        ContainerFields = containerFields;
    }

    /// <summary>
    /// Gets the problems of the design that describing found: a mapping from a field that does not arrive (FDW-2013). Describing adds to it as it goes.
    /// </summary>
    public List<GraphProblem> Problems { get; } = [];

    /// <summary>Gets the reader of a data set's fields.</summary>
    public Func<string, CancellationToken, Task<IGenericResult<IReadOnlyList<GraphField>>>> DataSetFields { get; }

    /// <summary>Gets the reader of a container's fields (FDW-5523), or none when the caller has none to give.</summary>
    public Func<ContainerFieldsRequest, CancellationToken, Task<IGenericResult<IReadOnlyList<GraphField>>>>? ContainerFields { get; }
}
