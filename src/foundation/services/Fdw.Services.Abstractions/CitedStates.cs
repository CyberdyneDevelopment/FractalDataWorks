using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Abstractions;

/// <summary>
/// Where a member a record cites stands in its own domain (FDW-6221): there now, retired, or never there.
/// </summary>
/// <remarks>
/// A record that cites a member refuses a retired one for its own reason, apart from one that was never there, because the caller does different things about
/// each: a retired member is brought back, an absent one is a mistaken id. <see cref="UncheckedCitedState"/> is what a citation that asks nobody answers.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(CitedStateBase), typeof(ICitedState), typeof(CitedStates))]
public abstract partial class CitedStates : TypeCollectionBase<CitedStateBase, ICitedState>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
