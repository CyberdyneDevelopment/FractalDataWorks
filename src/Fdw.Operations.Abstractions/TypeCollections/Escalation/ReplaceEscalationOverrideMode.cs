using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Abstractions.TypeCollections.Escalation;

/// <summary>
/// Replace - replace policy entirely with override.
/// </summary>
[TypeOption(typeof(EscalationOverrideModes), "Replace", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ReplaceEscalationOverrideMode : EscalationOverrideModeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReplaceEscalationOverrideMode"/> class.
    /// </summary>
    public ReplaceEscalationOverrideMode()
        : base(id: 1, name: "Replace")
    {
    }
}