using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Services.Pipelines.TestDouble;

/// <summary>A test pipeline type whose run mode is declared (<see cref="UnrunPipelineRunMode"/>) and which no runner carries out (FDW-6141).</summary>
[ExcludeFromCodeCoverage]
[Implementation(typeof(PipelineTypes), OptionName)]
public sealed class UnrunPipelineType : RecordingPipelineTypeBase<long, string>
{
    public const string OptionName = "Unrun";

    public const string RunMode = "Unrun";

    public UnrunPipelineType()
        : base(OptionName, RunMode)
    {
        Registration((builder, loggerFactory) => Record(this, builder));
        Initialization((host, loggerFactory) => RegisterRow(this, host, loggerFactory));
    }
}
