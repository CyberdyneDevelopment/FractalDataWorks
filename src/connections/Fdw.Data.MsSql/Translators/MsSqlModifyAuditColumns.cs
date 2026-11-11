using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Abstractions;

namespace Fdw.Data.MsSql.Translators;

/// <summary>
/// The DDL convention's Modify* audit columns and the expressions their column defaults use.
/// </summary>
/// <remarks>
/// Why: the audit block is DB-filled and never app-set (system-provided), so an INSERT leaves it to the column defaults. An
/// in-place UPDATE fires no default, so without a restamp ModifyDate/ModifyBy keep their insert-time values forever. Restamping
/// with the same expressions an INSERT would use keeps the columns honest. The set is the convention's closed list, matched by
/// name. The single-row and the bulk update both restamp them.
/// </remarks>
internal static class MsSqlModifyAuditColumns
{
    /// <summary>The audit columns and the expression each is restamped with.</summary>
    internal static readonly (string Column, string Expression)[] All =
    [
        ("ModifyDate", "SYSDATETIMEOFFSET()"),
        ("ModifyBy", "SUSER_SNAME()"),
        ("ModifyOnBehalfOf", "SUSER_SNAME()"),
    ];

    /// <summary>Returns whether <paramref name="column"/> is one of the audit columns.</summary>
    internal static bool Is(string column)
        => All.Any(audit => string.Equals(audit.Column, column, StringComparison.OrdinalIgnoreCase));

    /// <summary>Lists the audit columns the container declares, with the expression each is restamped with.</summary>
    internal static IReadOnlyList<(string Column, string Expression)> Declared(IStorageContainer container)
        => All.Where(audit => container.Schema.Fields.Any(field => string.Equals(field.Name, audit.Column, StringComparison.OrdinalIgnoreCase))).ToList();
}
