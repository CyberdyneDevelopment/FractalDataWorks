using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Services.Pipelines.TestDouble;

/// <summary>
/// A test pipeline type whose run mode is none of the <c>PipelineRunModes</c> options (FDW-6244), so a host that attaches it is stopped at Initialize. It
/// is also the second closing of <see cref="RecordingPipelineTypeBase{TIn, TOut}"/>, beside <see cref="EchoPipelineType"/>.
/// </summary>
[ExcludeFromCodeCoverage]
[Implementation(typeof(PipelineTypes), OptionName)]
public sealed class UnroutedPipelineType : RecordingPipelineTypeBase<int, string>
{
    /// <summary>The name a pipeline's domain row would give this implementation.</summary>
    public const string OptionName = "Unrouted";

    /// <summary>A run mode no <c>PipelineRunModes</c> option declares.</summary>
    public const string RunMode = "Teleported";

    /// <summary>Initializes a new instance of the <see cref="UnroutedPipelineType"/> class.</summary>
    public UnroutedPipelineType()
        : base(OptionName, RunMode)
    {
        Registration((builder, loggerFactory) => Record(this, builder));
        Initialization((host, loggerFactory) => RegisterRow(this, host, loggerFactory));
    }
}
