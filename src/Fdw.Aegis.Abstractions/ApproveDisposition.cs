using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Aegis.Abstractions;

/// <summary>
/// A terminal, injection-permitting verdict. The only disposition for which
/// <see cref="IVerdictDisposition.AllowsInjection"/> is <see langword="true"/>.
/// </summary>
[TypeOption(typeof(VerdictDispositions), "Approve", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ApproveDisposition : VerdictDispositionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApproveDisposition"/> class.
    /// </summary>
    public ApproveDisposition()
        : base(id: 1, name: "Approve", isTerminal: true, allowsInjection: true)
    {
    }
}
