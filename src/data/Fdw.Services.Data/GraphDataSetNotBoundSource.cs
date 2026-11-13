using System.Collections.Generic;

namespace Fdw.Services.Data;

/// <summary>An intended source of an abstract Graph data set, as the check before save reports it: not bound yet (FDW-5290).</summary>
/// <remarks>It is information, never a problem: an intention does not block a save and does not make the data set readable.</remarks>
/// <param name="Path">Where: <c>intendedSources/{index}</c>.</param>
/// <param name="IntentKind">The name of the <c>GraphIntentKinds</c> option the intent states.</param>
/// <param name="Name">The data store or data set the intent names.</param>
/// <param name="Exists">Whether the data store or data set exists today. An intended source that does not exist yet is still only "not bound".</param>
public sealed record GraphDataSetNotBoundSource(IReadOnlyList<string> Path, string IntentKind, string Name, bool Exists);
