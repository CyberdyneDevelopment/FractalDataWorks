using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Etl.Graphs.Describing;

/// <summary>
/// A union all produces the fields of its first input as that input sees them, which are the mapped fields when the input maps (FDW-2013). A field
/// keeps its origin only when every input has that field with the same origin (FDW-2157): the rows of the others come from elsewhere.
/// </summary>
[TypeOption(typeof(ComponentFieldDerivers), "UnionAll")]
public sealed class UnionAllFieldDeriver : ComponentFieldDeriverBase
{
    /// <summary>Initializes a new instance of the <see cref="UnionAllFieldDeriver"/> class.</summary>
    public UnionAllFieldDeriver() : base("UnionAll")
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
        var ordered = inputs.Keys.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToList();
        var first = ordered.FirstOrDefault();
        table["output"] = first is null || inputs[first] is not { } fields
            ? null
            : [.. fields.Select(field => ordered.All(name => Agrees(inputs[name], field)) ? field : field with { Origin = null })];

        return Answer(table);
    }

    // An input agrees when its fields are known and hold the same field with the same origin; an input whose fields are unknown cannot say.
    private static bool Agrees(IReadOnlyList<GraphField>? input, GraphField field) =>
        input is not null
        && input.Any(other => string.Equals(other.Name, field.Name, StringComparison.OrdinalIgnoreCase) && Equals(other.Origin, field.Origin));
}
