using System.Collections.Generic;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// What a pushed query does with a row whose stated side is absent (FDW-6969): the row of a full join that its left side had none for, or the row
/// of a lookup that found no reference row.
/// </summary>
/// <param name="PresenceField">The field the query adds to say whether the side is present: a key column of that side, null when it is absent. It never leaves the component.</param>
/// <param name="Dropped">The fields an absent side does not give the row: the fields a lookup brings; none for a full join, whose absent side stays as nulls.</param>
/// <param name="SentLast">True when the row is held and sent after every other row, as a full join sends the right rows nothing matched.</param>
/// <param name="Output">The output an absent row leaves by, for a lookup with an unmatched output; null otherwise.</param>
/// <param name="Keys">The fields of the row a lookup's failure names the key by; empty for a full join.</param>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed record FlowPushedAbsence(string PresenceField, IReadOnlyList<string> Dropped, bool SentLast, string? Output, IReadOnlyList<string> Keys);
