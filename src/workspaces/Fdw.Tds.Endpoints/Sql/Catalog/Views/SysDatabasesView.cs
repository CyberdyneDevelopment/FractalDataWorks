using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System;
using Fdw.Collections.Attributes;
using Fdw.Tds.Endpoints.Hosting;
using static Fdw.Tds.Endpoints.Sql.Catalog.CatalogColumn;

namespace Fdw.Tds.Endpoints.Sql.Catalog.Views;

/// <summary>
/// sys.databases: the databases a caller can name: master, and the one database this server serves, read-only and online.
/// </summary>
[TypeOption(typeof(CatalogViews), "SysDatabases", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SysDatabasesView : CatalogViewBase
{
    private static readonly IReadOnlyList<CatalogColumn> ViewColumns =
    [
        SysnameColumn("name"),
        IntColumn("database_id"),
        IntColumn("source_database_id", nullable: true),
        BinaryColumn("owner_sid", 85),
        DateTimeColumn("create_date"),
        TinyIntColumn("compatibility_level"),
        SysnameColumn("collation_name", nullable: true),
        TinyIntColumn("user_access", nullable: true),
        TextColumn("user_access_desc", 60, nullable: true),
        BitColumn("is_read_only", nullable: true),
        BitColumn("is_auto_close_on"),
        BitColumn("is_auto_shrink_on", nullable: true),
        TinyIntColumn("state", nullable: true),
        TextColumn("state_desc", 60, nullable: true),
        BitColumn("is_in_standby", nullable: true),
        BitColumn("is_cleanly_shutdown", nullable: true),
        TinyIntColumn("snapshot_isolation_state", nullable: true),
        TextColumn("snapshot_isolation_state_desc", 60, nullable: true),
        BitColumn("is_read_committed_snapshot_on", nullable: true),
        TinyIntColumn("recovery_model", nullable: true),
        TextColumn("recovery_model_desc", 60, nullable: true),
        BitColumn("is_ansi_nulls_on", nullable: true),
        BitColumn("is_quoted_identifier_on", nullable: true),
        BitColumn("is_fulltext_enabled", nullable: true),
        BitColumn("is_trustworthy_on", nullable: true),
        BitColumn("is_published"),
        BitColumn("is_subscribed"),
        BitColumn("is_merge_published"),
        BitColumn("is_distributor"),
        BitColumn("is_broker_enabled"),
        BitColumn("is_cdc_enabled"),
        BitColumn("is_encrypted", nullable: true),
        TinyIntColumn("containment", nullable: true),
        TextColumn("containment_desc", 60, nullable: true),
        IntColumn("target_recovery_time_in_seconds", nullable: true),
        BitColumn("is_query_store_on", nullable: true),
        BitColumn("is_ledger_on", nullable: true),
    ];

    /// <summary>Initializes a new instance of the <see cref="SysDatabasesView"/> class.</summary>
    public SysDatabasesView()
        : base(name: "SysDatabases", schema: "sys", viewName: "databases")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<CatalogColumn> Columns => ViewColumns;

    /// <inheritdoc/>
    public override IEnumerable<object?[]> Rows(SqlExecutionContext context, CatalogSnapshot catalog)
    {
        ArgumentNullException.ThrowIfNull(context);
        // The database came online with this process, so that is when sys.databases says it was created.
        using var process = System.Diagnostics.Process.GetCurrentProcess();
        var started = process.StartTime;
        var collation = context.Server.Collation.Name;
        yield return Database(Functions.Databases.Master, Functions.Databases.MasterId, started, collation);
        yield return Database(context.Server.DatabaseName, TdsServerIdentity.DatabaseId, started, collation);
    }

    // Online, read-only, simple recovery, open to everyone: what this server serves.
    private static object?[] Database(string name, int id, DateTime created, string collation)
        =>
        [
            name, id, null, null, created, (byte)160, collation, (byte)0, "MULTI_USER", true, false, false,
            (byte)0, "ONLINE", false, true, (byte)0, "OFF", false, (byte)3, "SIMPLE", true, true, false, false,
            false, false, false, false, false, false, false, (byte)0, "NONE", 60, false, false,
        ];
}
