using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Etl.Graphs.Describing;

/// <summary>A merge join produces the left fields and the right fields with the prefix before their names.</summary>
[TypeOption(typeof(ComponentFieldDerivers), "MergeJoin")]
public sealed class MergeJoinFieldDeriver : ComponentFieldDeriverBase
{
    /// <summary>Initializes a new instance of the <see cref="MergeJoinFieldDeriver"/> class.</summary>
    public MergeJoinFieldDeriver() : base("MergeJoin")
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
        inputs.TryGetValue("left", out var left);
        inputs.TryGetValue("right", out var right);
        table["output"] = left is null || right is null || component.RightPrefix is not { } prefix
            ? null
            : [.. left, .. right.Select(field => new GraphField(string.Concat(prefix, field.Name), field.Type, field.Origin))];
        return Answer(table);
    }
}
