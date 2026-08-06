using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Abstractions.TypeCollections.Escalation;

/// <summary>
/// Merge - merge additional recipients into existing policy.
/// </summary>
[TypeOption(typeof(EscalationOverrideModes), "Merge", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MergeEscalationOverrideMode : EscalationOverrideModeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MergeEscalationOverrideMode"/> class.
    /// </summary>
    public MergeEscalationOverrideMode()
        : base(id: 2, name: "Merge")
    {
    }
}