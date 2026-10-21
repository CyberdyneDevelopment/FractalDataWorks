using System.Collections.Generic;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>One thing of the catalog doing something with another, as the stored settings say it (FDW-1683).</summary>
/// <param name="FromKind">The kind of the thing that does it.</param>
/// <param name="FromName">Its name.</param>
/// <param name="Relation">What it does: one of <see cref="CatalogRelations"/>.</param>
/// <param name="ToKind">The kind of the thing it does it with.</param>
/// <param name="ToName">Its name.</param>
/// <param name="ToParents">The names the other sits under, outermost first.</param>
internal sealed record CatalogEdge(
    string FromKind, string FromName, string Relation, string ToKind, string ToName, IReadOnlyList<string> ToParents);
