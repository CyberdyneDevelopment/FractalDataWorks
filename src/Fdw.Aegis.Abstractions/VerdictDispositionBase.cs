using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Aegis.Abstractions;

/// <summary>
/// CRTP base class for <see cref="IVerdictDisposition"/> options. Each concrete disposition supplies
/// its id, name, and the two behavior flags — no switch statement anywhere resolves them.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class VerdictDispositionBase : TypeOptionBase<VerdictDispositionBase>, IVerdictDisposition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VerdictDispositionBase"/> class.
    /// </summary>
    /// <param name="name">The disposition name.</param>
    /// <param name="isTerminal">Whether this disposition is a final answer.</param>
    /// <param name="allowsInjection">Whether this disposition permits secret injection.</param>
    protected VerdictDispositionBase(string name, bool isTerminal, bool allowsInjection)
        : base(name)
    {
        IsTerminal = isTerminal;
        AllowsInjection = allowsInjection;
    }

    /// <inheritdoc />
    public bool IsTerminal { get; }

    /// <inheritdoc />
    public bool AllowsInjection { get; }
}
