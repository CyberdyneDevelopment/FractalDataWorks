using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Descriptions.Abstractions;

/// <summary>A pipeline's description: what the pipeline is for, in the words of whoever made it (FDW-7323). Read by whoever holds <c>pipelines:read</c>, the permission the pipeline routes ask. The pipeline is the thing described, so the type has no owner: a pipeline is removed with its own descriptions.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DescriptionTypes), OptionName)]
public sealed class PipelineDescriptionType : DescriptionTypeBase
{
    /// <summary>The type's name, which the pipeline provider writes and reads its descriptions under.</summary>
    public const string OptionName = "Pipeline";

    /// <summary>Initializes a new instance of the <see cref="PipelineDescriptionType"/> class.</summary>
    public PipelineDescriptionType()
        : base(OptionName, "pipelines:read", hasOwner: false)
    {
    }
}
