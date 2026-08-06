using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Aegis.Abstractions;

/// <summary>
/// A non-terminal verdict: the evaluator declines to decide (e.g. an agent approver passing to a
/// human). Not injection-permitting — a further decision is required before any action proceeds.
/// </summary>
[TypeOption(typeof(VerdictDispositions), "Abstain", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class AbstainDisposition : VerdictDispositionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AbstainDisposition"/> class.
    /// </summary>
    public AbstainDisposition()
        : base(id: 3, name: "Abstain", isTerminal: false, allowsInjection: false)
    {
    }
}
