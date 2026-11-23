using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Services.Pipelines.TestDouble;

/// <summary>
/// A closed option of <see cref="PipelineTypes"/> over the abstract generic <see cref="RecordingPipelineTypeBase{TIn, TOut}"/> (FDW-6244). It lives in
/// its own assembly because the Implementation module initializer reads referenced assemblies only.
/// </summary>
[ExcludeFromCodeCoverage]
[Implementation(typeof(PipelineTypes), OptionName)]
public sealed class EchoPipelineType : RecordingPipelineTypeBase<string, int>
{
    /// <summary>The implementation name this option registers under.</summary>
    public const string OptionName = "Echo";

    /// <summary>The run mode a run of this type is carried out in.</summary>
    public const string RunMode = "Queued";

    /// <summary>Initializes a new instance of the <see cref="EchoPipelineType"/> class.</summary>
    public EchoPipelineType()
        : base(OptionName, RunMode)
    {
        Registration((builder, loggerFactory) => Record(this, builder));
        Initialization((host, loggerFactory) => RegisterRow(this, host, loggerFactory));
    }
}
