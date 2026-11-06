using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Abstractions;

/// <summary>Phase 3: Initialize (wire factories and providers after Build).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ServiceTypePhases), "Initialize")]
public sealed class InitializePhase : ServiceTypePhaseBase
{
    /// <summary>Initializes a new instance of the <see cref="InitializePhase"/> class.</summary>
    public InitializePhase()
        : base(3, "Initialize")
    {
    }
}
