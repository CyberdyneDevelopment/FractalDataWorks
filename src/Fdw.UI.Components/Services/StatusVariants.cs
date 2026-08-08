using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Services;

/// <summary>
/// TypeCollection for semantic status variants. Every badge the UI draws takes its colour from a
/// variant registered here and is rendered through <see cref="Primitives.Badge"/>; no page carries a
/// badge class literal.
/// </summary>
/// <remarks>
/// Why a TypeCollection and not a private table: the tone set has to be open. A downstream skin adds a
/// tone by declaring another <c>[TypeOption]</c> against this collection and rebrands a shipped one
/// with <c>[Replaces]</c>. The five members are the whole badge vocabulary the pages emit — b-ok,
/// b-fail, b-warn, b-run, b-idle — so a page chooses a tone and never a class.
/// </remarks>
[TypeCollection(typeof(StatusVariantBase), typeof(IStatusVariant), typeof(StatusVariants))]
[ExcludeFromCodeCoverage]
public abstract partial class StatusVariants : TypeCollectionBase<StatusVariantBase, IStatusVariant> { }
