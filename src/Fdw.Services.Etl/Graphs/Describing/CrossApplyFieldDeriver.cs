using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Etl.Graphs.Describing;

/// <summary>A cross apply produces its input's fields and the field that takes each value; a delimited field yields text.</summary>
[TypeOption(typeof(ComponentFieldDerivers), "CrossApply")]
public sealed class CrossApplyFieldDeriver : ComponentFieldDeriverBase
{
    /// <summary>Initializes a new instance of the <see cref="CrossApplyFieldDeriver"/> class.</summary>
    public CrossApplyFieldDeriver() : base("CrossApply")
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
        inputs.TryGetValue("input", out var input);
        table["output"] = input is null || component.TargetName is not { } target
            ? null
            : [.. input, new GraphField(target, component.Delimiter is null ? null : "String")];
        return Answer(table);
    }
}
