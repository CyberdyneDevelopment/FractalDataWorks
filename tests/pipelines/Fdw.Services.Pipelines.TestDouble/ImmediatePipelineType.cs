using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Services.Pipelines.TestDouble;

/// <summary>
/// A pipeline type whose run mode, <c>AtOnce</c>, is a declared <c>PipelineRunModes</c> option that FDW ships no runner for (FDW-6244), so a host that
/// runs pipelines and registers no AtOnce runner is stopped at Initialize naming it.
/// </summary>
[ExcludeFromCodeCoverage]
[Implementation(typeof(PipelineTypes), OptionName)]
public sealed class ImmediatePipelineType : RecordingPipelineTypeBase<string, string>
{
    /// <summary>The implementation name a pipeline's domain row would give this option.</summary>
    public const string OptionName = "Immediate";

    /// <summary>The run mode a run of this type is carried out in.</summary>
    public const string RunMode = "AtOnce";

    /// <summary>Initializes a new instance of the <see cref="ImmediatePipelineType"/> class.</summary>
    public ImmediatePipelineType()
        : base(OptionName, RunMode)
    {
        Registration((builder, loggerFactory) => Record(this, builder));
        Initialization((host, loggerFactory) => RegisterRow(this, host, loggerFactory));
    }
}
