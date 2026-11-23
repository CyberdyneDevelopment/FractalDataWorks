using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Descriptions.Abstractions;

/// <summary>A pipeline parameter's description: what a run may give it, in the words of whoever declared it (FDW-7323). Read by whoever holds <c>pipelines:read</c>, the permission the pipeline routes ask. A parameter belongs to its pipeline, which names it, so the type has an owner: the parameters of a pipeline are kept in step with it and removed with it.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DescriptionTypes), OptionName)]
public sealed class PipelineParameterDescriptionType : DescriptionTypeBase
{
    /// <summary>The type's name, which the pipeline provider writes and reads its descriptions under.</summary>
    public const string OptionName = "PipelineParameter";

    /// <summary>Initializes a new instance of the <see cref="PipelineParameterDescriptionType"/> class.</summary>
    public PipelineParameterDescriptionType()
        : base(OptionName, "pipelines:read", hasOwner: true)
    {
    }
}
