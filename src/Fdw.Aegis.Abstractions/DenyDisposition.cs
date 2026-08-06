using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Aegis.Abstractions;

/// <summary>
/// A terminal, non-approving verdict. The fail-closed default for a new <see cref="Verdict"/>.
/// </summary>
[TypeOption(typeof(VerdictDispositions), "Deny", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DenyDisposition : VerdictDispositionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DenyDisposition"/> class.
    /// </summary>
    public DenyDisposition()
        : base(id: 2, name: "Deny", isTerminal: true, allowsInjection: false)
    {
    }
}
