using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Transforms;

namespace Fdw.Services.Etl.Graphs.Describing;

/// <summary>
/// Says what a stage-backed tool puts on its output node, and on its error output when it names one.
/// </summary>
/// <remarks>
/// A tool's output is the input with the changes its stage kind makes; a kind says its own change by overriding
/// <see cref="Output"/>. The error output carries the rows the tool rejected: the input fields plus the component and the reason.
/// </remarks>
public abstract class StageToolFieldDeriverBase : ComponentFieldDeriverBase
{
    /// <summary>Initializes a new instance of the <see cref="StageToolFieldDeriverBase"/> class.</summary>
    /// <param name="name">The tool's name.</param>
    protected StageToolFieldDeriverBase(string name) : base(name)
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
        table["output"] = Output(component.Stages.Count == 1 ? component.Stages[0] : null, input);
        if (component.ErrorOutput is { } error)
        {
            table[error] = input is null
                ? null
                : [.. input, new GraphField("ErrorComponent", "String"), new GraphField("ErrorReason", "String")];
        }

        return Answer(table);
    }

    /// <summary>Gives the fields on the tool's output node.</summary>
    /// <param name="stage">The tool's stage, or <see langword="null"/> when it could not be read yet (a setting still being filled in).</param>
    /// <param name="input">The fields on the input node, or <see langword="null"/> when they are not known.</param>
    /// <returns>The fields, or <see langword="null"/> when they are not known.</returns>
    protected abstract IReadOnlyList<GraphField>? Output(PipelineTransformConfiguration? stage, IReadOnlyList<GraphField>? input);
}
