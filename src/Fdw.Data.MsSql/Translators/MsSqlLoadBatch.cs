namespace Fdw.Data.MsSql.Translators;

/// <summary>
/// Wraps a multi-statement load in the batch shape every load translator shares: it opens a transaction only when
/// the caller has none (so inside a unit of work it joins the caller's), drops its temporary table on every exit,
/// and rethrows any error so the caller sees the real SQL error.
/// </summary>
internal static class MsSqlLoadBatch
{
    /// <summary>
    /// Wraps <paramref name="body"/>. Row counts reported by ExecuteNonQuery are those of the statements in the body
    /// that run while NOCOUNT is off.
    /// </summary>
    /// <param name="declarations">DECLARE statements that must precede the transaction (may be empty).</param>
    /// <param name="body">The statements to run.</param>
    /// <param name="tempTable">The temporary table to drop, or <see langword="null"/> for none.</param>
    /// <param name="useTransaction">Whether the body needs a transaction of its own when the caller has none.</param>
    internal static string Wrap(string declarations, string body, string? tempTable, bool useTransaction)
    {
        var dropOnSuccess = tempTable is null ? string.Empty : $"DROP TABLE {tempTable}; ";
        var dropOnFailure = tempTable is null
            ? string.Empty
            : $"IF OBJECT_ID('tempdb..{tempTable}') IS NOT NULL DROP TABLE {tempTable}; ";
        var begin = useTransaction ? "IF @fdw_own = 1 BEGIN TRANSACTION; " : string.Empty;
        var commit = useTransaction ? "IF @fdw_own = 1 COMMIT TRANSACTION; " : string.Empty;
        var rollback = useTransaction ? "IF @fdw_own = 1 AND @@TRANCOUNT > 0 ROLLBACK TRANSACTION; " : string.Empty;

        return "SET NOCOUNT ON; "
               + "DECLARE @fdw_own bit = CASE WHEN @@TRANCOUNT = 0 THEN 1 ELSE 0 END; "
               + declarations
               + begin
               + "BEGIN TRY "
               + body
               + "SET NOCOUNT ON; "
               + dropOnSuccess
               + commit
               + "END TRY BEGIN CATCH "
               + rollback
               + dropOnFailure
               + "THROW; END CATCH;";
    }
}
