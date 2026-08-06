using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Abstractions.TypeCollections.Escalation;

/// <summary>
/// Default - use default policy without modifications.
/// </summary>
[TypeOption(typeof(EscalationOverrideModes), "Default", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DefaultEscalationOverrideMode : EscalationOverrideModeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultEscalationOverrideMode"/> class.
    /// </summary>
    public DefaultEscalationOverrideMode()
        : base(id: 0, name: "Default")
    {
    }
}