using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Abstractions;

/// <summary>Phase 1: Configure (bind options and register required services before Build).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ServiceTypePhases), "Configure")]
public sealed class ConfigurePhase : ServiceTypePhaseBase
{
    /// <summary>Initializes a new instance of the <see cref="ConfigurePhase"/> class.</summary>
    public ConfigurePhase()
        : base(1, "Configure")
    {
    }
}
