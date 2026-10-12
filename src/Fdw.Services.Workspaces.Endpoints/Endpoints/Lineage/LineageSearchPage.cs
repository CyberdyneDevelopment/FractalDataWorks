using System.Collections.Generic;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>One page of lineage search matches, and the workspace they were searched in, when they were searched in one.</summary>
/// <param name="Items">The matches on this page, the closest names first.</param>
/// <param name="Skip">How many matches precede this page.</param>
/// <param name="Take">How many matches this page holds at most.</param>
/// <param name="HasMore">Whether matches follow this page.</param>
/// <param name="Root">The step that roots every match's path: the workspace the search was scoped to, or nothing when it was not scoped.</param>
internal sealed record LineageSearchPage(List<LineageNode> Items, int Skip, int Take, bool HasMore, LineagePathStepDto? Root);
