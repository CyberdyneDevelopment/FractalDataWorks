using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Abstractions;

/// <summary>Phase 2: Register (register services before Build).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ServiceTypePhases), "Register")]
public sealed class RegisterPhase : ServiceTypePhaseBase
{
    /// <summary>Initializes a new instance of the <see cref="RegisterPhase"/> class.</summary>
    public RegisterPhase()
        : base(2, "Register")
    {
    }
}
