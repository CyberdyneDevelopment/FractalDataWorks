using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Pipelines.Abstractions.Runs;

namespace Fdw.Services.Pipelines.TestDouble;

/// <summary>
/// A run mode no host carries out (FDW-6141): the run-mode checks need a declared run mode with no runner, now that FDW ships a runner for every run mode it declares.
/// It is declared here, in the test double, where the collection's generator registers it; no host registers a runner for it.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(PipelineRunModes), "Unrun")]
public sealed class UnrunPipelineRunMode : PipelineRunModeBase
{
    /// <summary>Initializes a new instance of the <see cref="UnrunPipelineRunMode"/> class.</summary>
    public UnrunPipelineRunMode()
        : base("Unrun", "A run mode no runner carries out; a test double, never used by a host.")
    {
    }
}
