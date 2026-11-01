using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Etl.Graphs.Describing;

/// <summary>A lookup produces its input's fields and the columns it brings; the unmatched output carries the input's fields as they were.</summary>
[TypeOption(typeof(ComponentFieldDerivers), "Lookup")]
public sealed class LookupFieldDeriver : ComponentFieldDeriverBase
{
    /// <summary>Initializes a new instance of the <see cref="LookupFieldDeriver"/> class.</summary>
    public LookupFieldDeriver() : base("Lookup")
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
        inputs.TryGetValue("reference", out var reference);
        var bring = Mapped(component, "bring", null, reference);
        table["output"] = input is null ? null : [.. input, .. bring];
        if (component.UnmatchedOutput is { } unmatched)
        {
            table[unmatched] = input;
        }

        return Answer(table);
    }
}
