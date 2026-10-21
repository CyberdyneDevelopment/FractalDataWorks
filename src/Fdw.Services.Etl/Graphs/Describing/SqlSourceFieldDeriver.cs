using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Etl.Graphs.Describing;

/// <summary>A SQL source's fields are what its script selects, which only a run can say, so its output has none known (FDW-1443).</summary>
[TypeOption(typeof(ComponentFieldDerivers), "SqlSource")]
public sealed class SqlSourceFieldDeriver : ComponentFieldDeriverBase
{
    /// <summary>Initializes a new instance of the <see cref="SqlSourceFieldDeriver"/> class.</summary>
    public SqlSourceFieldDeriver() : base("SqlSource")
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
