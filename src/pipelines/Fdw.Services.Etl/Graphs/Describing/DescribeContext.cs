using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Etl.Graphs.Describing;

/// <summary>
/// What describing a graph needs from outside it (FDW-1397): the fields of a data set.
/// </summary>
public sealed class DescribeContext
{
    /// <summary>Initializes a new instance of the <see cref="DescribeContext"/> class.</summary>
    /// <param name="dataSetFields">Reads the fields of a data set by name, in order.</param>
    public DescribeContext(Func<string, CancellationToken, Task<IGenericResult<IReadOnlyList<GraphField>>>> dataSetFields)
    {
        DataSetFields = dataSetFields ?? throw new ArgumentNullException(nameof(dataSetFields));
    }

    /// <summary>
    /// Gets the problems of the design that describing found: a mapping from a field that does not arrive (FDW-2013). Describing adds to it as it goes.
    /// </summary>
    public List<GraphProblem> Problems { get; } = [];

    /// <summary>Gets the reader of a data set's fields.</summary>
    public Func<string, CancellationToken, Task<IGenericResult<IReadOnlyList<GraphField>>>> DataSetFields { get; }
}
