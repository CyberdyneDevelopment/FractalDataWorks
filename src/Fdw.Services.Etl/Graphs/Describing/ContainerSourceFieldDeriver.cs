using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Etl.Graphs.Describing;

/// <summary>A container source's fields are the container's, which are not read until the run.</summary>
[TypeOption(typeof(ComponentFieldDerivers), "ContainerSource")]
public sealed class ContainerSourceFieldDeriver : ComponentFieldDeriverBase
{
    /// <summary>Initializes a new instance of the <see cref="ContainerSourceFieldDeriver"/> class.</summary>
    public ContainerSourceFieldDeriver() : base("ContainerSource")
    {
    }

    /// <inheritdoc />
    public override Task<IGenericResult<IReadOnlyDictionary<string, IReadOnlyList<GraphField>?>>> Derive(
        PipelineComponentConfiguration component,
        IReadOnlyDictionary<string, IReadOnlyList<GraphField>?> inputs,
        DescribeContext context,
        CancellationToken cancellationToken = default)
    {
        var table = NewTable();
        table["output"] = null;
        return Answer(table);
    }
}
