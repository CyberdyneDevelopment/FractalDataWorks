using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Services.Pipelines.TestDouble;

/// <summary>
/// A pipeline type whose runs are for a person (FDW-6137), as the Interactive type's are: a start of it that acts as no person is refused. It sets no phase, as <see cref="SilentPipelineType"/> does, and runs
/// Queued, so the run-mode checks do not refuse it.
/// </summary>
[ExcludeFromCodeCoverage]
[Implementation(typeof(PipelineTypes), OptionName)]
public sealed class PersonOnlyPipelineType : RecordingPipelineTypeBase<long, long>
{
    /// <summary>The implementation name a pipeline's domain row would give this option.</summary>
    public const string OptionName = "PersonOnly";

    /// <summary>Initializes a new instance of the <see cref="PersonOnlyPipelineType"/> class. It sets no phase, on purpose.</summary>
    public PersonOnlyPipelineType()
        : base(OptionName, "Queued")
    {
    }

    /// <inheritdoc />
    public override bool RequiresPerson => true;
}
