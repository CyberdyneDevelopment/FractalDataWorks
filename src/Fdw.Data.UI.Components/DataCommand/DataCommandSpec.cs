using System.Collections.Generic;

namespace Fdw.Data.UI.Components.DataCommand;

/// <summary>
/// Serializable command specification that the pipeline builder writes to
/// <c>task.Configuration["CommandSpec"]</c> and that the pipeline runtime deserializes
/// at execution time to construct the appropriate <c>IDataCommand</c>.
/// </summary>
/// <remarks>
/// This is a discriminated-union-by-convention: only the fields relevant to the
/// capability kind are populated. Unused fields remain at their defaults.
/// The JSON shape is stable — adding new fields is backward-compatible because
/// JSON deserialization ignores unknown/missing members.
/// </remarks>
public sealed class DataCommandSpec
{
    /// <summary>Gets or sets the command kind discriminator.</summary>
    public string Kind { get; set; } = "Query";

    // ── Query / Read ──────────────────────────────────────────────────────────

    /// <summary>
    /// Primary FROM source for Query commands.
    /// Also used as the source container for BulkInsert/BulkUpsert source mapping.
    /// </summary>
    public DataCommandFrom? From { get; set; }

    /// <summary>JOIN clauses for Query commands. Empty list means single-table query.</summary>
    public IList<DataCommandJoin> Joins { get; set; } = new List<DataCommandJoin>();

    /// <summary>
    /// Selected fields for Query/Insert commands.
    /// For Query: the projected output columns. For Insert: the columns being inserted.
    /// Empty list means SELECT * / all fields.
    /// </summary>
    public IList<DataCommandField> Fields { get; set; } = new List<DataCommandField>();

    /// <summary>
    /// Filter expression for Query, Update, and Upsert match conditions.
    /// </summary>
    public DataCommandFilter? Filter { get; set; }

    /// <summary>Paging specification for Query commands.</summary>
    public DataCommandPaging? Paging { get; set; }

    /// <summary>Sort specification for Query commands.</summary>
    public IList<DataCommandSort> Sort { get; set; } = new List<DataCommandSort>();

    // ── Write ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Target container for Insert, Update, Upsert, BulkInsert, and BulkUpsert commands.
    /// </summary>
    public DataCommandTarget? Target { get; set; }

    /// <summary>
    /// SET clauses for Update and Upsert commands.
    /// Each clause assigns a value expression to a target field.
    /// </summary>
    public IList<DataCommandSetClause> Set { get; set; } = new List<DataCommandSetClause>();

    /// <summary>
    /// Per-field value entries for Insert commands (field name to literal value or expression).
    /// </summary>
    public IList<DataCommandValueEntry> Values { get; set; } = new List<DataCommandValueEntry>();

    /// <summary>
    /// Key fields used for Upsert and BulkUpsert match conditions.
    /// Values are field names (not fully-qualified).
    /// </summary>
    public IList<string> KeyFields { get; set; } = new List<string>();

    /// <summary>
    /// Batch size for BulkInsert and BulkUpsert commands.
    /// 0 means use the connection's default batch size at runtime.
    /// </summary>
    public int BatchSize { get; set; }
}
