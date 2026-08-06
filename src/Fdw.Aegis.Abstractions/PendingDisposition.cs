using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Aegis.Abstractions;

/// <summary>
/// A non-terminal verdict: the request is enqueued and awaiting a decision (Phase 2 human-in-the-loop).
/// Not injection-permitting.
/// </summary>
[TypeOption(typeof(VerdictDispositions), "Pending", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class PendingDisposition : VerdictDispositionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PendingDisposition"/> class.
    /// </summary>
    public PendingDisposition()
        : base(id: 4, name: "Pending", isTerminal: false, allowsInjection: false)
    {
    }
}
