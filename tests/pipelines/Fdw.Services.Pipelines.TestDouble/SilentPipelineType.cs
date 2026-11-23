using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Services.Pipelines.TestDouble;

/// <summary>
/// A pipeline type that sets no phase (FDW-6244): it registers nothing and no row provider under its name, so a host that attaches it is stopped at
/// Initialize with ImplementationProviderNotRegistered naming it. It runs Queued, so the run-mode checks do not refuse it.
/// </summary>
[ExcludeFromCodeCoverage]
[Implementation(typeof(PipelineTypes), OptionName)]
public sealed class SilentPipelineType : RecordingPipelineTypeBase<long, long>
{
    /// <summary>The implementation name a pipeline's domain row would give this option.</summary>
    public const string OptionName = "Silent";

    /// <summary>Initializes a new instance of the <see cref="SilentPipelineType"/> class. It sets no phase, on purpose.</summary>
    public SilentPipelineType()
        : base(OptionName, "Queued")
    {
    }
}
