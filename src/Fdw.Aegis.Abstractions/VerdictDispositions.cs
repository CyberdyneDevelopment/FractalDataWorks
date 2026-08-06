using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Aegis.Abstractions;

/// <summary>
/// Closed collection of verdict dispositions (Approve/Deny/Abstain/Pending). A TypeCollection, not
/// an enum — behavior (<see cref="IVerdictDisposition.IsTerminal"/>,
/// <see cref="IVerdictDisposition.AllowsInjection"/>) lives on each option.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(VerdictDispositionBase), typeof(IVerdictDisposition), typeof(VerdictDispositions))]
public abstract partial class VerdictDispositions : TypeCollectionBase<VerdictDispositionBase, IVerdictDisposition>
{
}
