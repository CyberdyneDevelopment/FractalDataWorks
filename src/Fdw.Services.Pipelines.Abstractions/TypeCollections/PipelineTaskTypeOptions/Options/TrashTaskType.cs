using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Transformations.Abstractions;

namespace Fdw.Services.Pipelines.Abstractions.TypeCollections.PipelineTaskTypeOptions.Options;

/// <summary>
/// Pipeline task type for silently discarding rejected or erroneous records.
/// </summary>
/// <remarks>
/// <para>
/// Trash nodes accept connections from any task's reject/error output. At runtime (Wave 0c),
/// records routed here are counted (<c>DiscardedCount</c> metric), buffered for inspection,
/// and optionally written to a log sink.
/// </para>
/// <para>
/// Wave 0a scope: type is scaffolded so users can place it on the canvas. Runtime routing
/// is implemented in Wave 0c.
/// </para>
/// </remarks>
[TypeOption(typeof(PipelineTaskTypes), "Trash")]
[ExcludeFromCodeCoverage]
public sealed class TrashTaskType : PipelineTaskTypeBase
{
    /// <summary>Initializes a new instance of <see cref="TrashTaskType"/>.</summary>
    public TrashTaskType()
        : base(
            id: 4,
            name: "Trash",
            configurationFields:
            [
                new TransformFieldDescriptor(
                    Key: "BufferSize",
                    Label: "Buffer size",
                    Placeholder: "100",
                    InputKind: TransformFieldKinds.Text),
                new TransformFieldDescriptor(
                    Key: "LogSinkName",
                    Label: "Log sink name",
                    Placeholder: "Leave empty for memory-only",
                    InputKind: TransformFieldKinds.Text),
            ])
    {
    }
}
