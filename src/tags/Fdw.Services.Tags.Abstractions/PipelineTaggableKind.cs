using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Tags.Abstractions;

/// <summary>A Pipeline that carries tags in <c>pipe.PipelineTag</c>. A thing of this kind sits in a workspace, so an organization or workspace tag is checked against its place.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(TaggableKinds), "Pipeline")]
public sealed class PipelineTaggableKind : TaggableKindBase
{
    /// <summary>Initializes a new instance of the <see cref="PipelineTaggableKind"/> class.</summary>
    public PipelineTaggableKind()
        : base("Pipeline", "pipe", "PipelineTag", "PipelineId", "Pipeline")
    {
    }
}
