using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Etl.Graphs.Describing;

/// <summary>A split passes the fields of its input through every output, the unmatched output included.</summary>
[TypeOption(typeof(ComponentFieldDerivers), "ConditionalSplit")]
public sealed class ConditionalSplitFieldDeriver : ComponentFieldDeriverBase
{
    /// <summary>Initializes a new instance of the <see cref="ConditionalSplitFieldDeriver"/> class.</summary>
    public ConditionalSplitFieldDeriver() : base("ConditionalSplit")
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
        foreach (var output in component.SplitOutputs)
        {
            table[output.OutputName] = input;
        }

        if (component.UnmatchedOutput is { } unmatched)
        {
            table[unmatched] = input;
        }

        return Answer(table);
    }
}
