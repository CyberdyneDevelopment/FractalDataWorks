using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Commands.Abstractions;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Commands.Data;

/// <summary>
/// Configuration delete command for soft-delete via version-on-write pattern.
/// Marks the current row as non-current and inserts a new row with IsDeleted = 1.
/// Returns the number of affected rows.
/// </summary>
/// <remarks>
/// <para>
/// This command implements the version-on-write soft-delete pattern used by all FDW configuration tables:
/// <list type="bullet">
/// <item>UPDATE: Sets IsCurrent = 0 on the existing current row</item>
/// <item>INSERT: Copies the row with IsCurrent = 1, IsDeleted = 1</item>
/// </list>
/// </para>
/// <para>
/// Example usage:
/// <code>
/// var call = new DataGatewayCall(
///     new ConfigurationDeleteCommand(logicalId),
///     new DataStoreTarget("ConfigurationDb", "cfg", "DataSet"));
/// var result = await gateway.Execute&lt;int&gt;(call, ct);
/// </code>
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataCommands), "ConfigurationDelete", RestrictToCurrentCompilation = true)]
public sealed class ConfigurationDeleteCommand : DataCommandBase<int, Guid>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationDeleteCommand"/> class.
    /// </summary>
    /// <param name="logicalId">The logical Id of the configuration entity to soft-delete.</param>
    public ConfigurationDeleteCommand(Guid logicalId)
        : base("ConfigurationDelete", logicalId)
    {
        ConfigurationDeleteCommandLog.CommandCreated(NullLogger<ConfigurationDeleteCommand>.Instance, logicalId);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationDeleteCommand"/> class that retires every
    /// row belonging to an owner, rather than one row identified by its own Id.
    /// </summary>
    /// <param name="ownerLogicalId">The OWNER's logical Id — the value the owner FK resolves to.</param>
    /// <param name="ownerForeignKeyColumn">
    /// The LOGICAL owner FK column name (e.g. <c>MsSqlConnectionId</c>), the same name the save cascade
    /// stamps. The translator maps it to the physical <c>{Owner}RowId</c> and resolves the owner's current
    /// RowId by subquery, exactly as the save translator does.
    /// </param>
    /// <remarks>
    /// Why: a KVP property-collection row (e.g. <c>conn.MsSqlConnectionAuthentication</c>) has no durable
    /// Id of its own — its identity is (owner, Name), which is why the save translator versions it on that
    /// natural key. Keyed only by <c>[Id]</c>, the delete could create such rows and never retire them, so
    /// deleting a connection left its authentication rows live under a deleted owner.
    /// </remarks>
    public ConfigurationDeleteCommand(Guid ownerLogicalId, string ownerForeignKeyColumn)
        : base("ConfigurationDelete", ownerLogicalId)
    {
        if (string.IsNullOrEmpty(ownerForeignKeyColumn))
        {
            // Why: reported defect (see logging-pass report) — this constructor throws instead of
            // returning IGenericResult. Logged here per scope; the throw below is left in place.
            ConfigurationDeleteCommandLog.OwnerForeignKeyColumnMissing(NullLogger<ConfigurationDeleteCommand>.Instance);
            throw new ArgumentException("Owner foreign key column is required for a scoped configuration delete.", nameof(ownerForeignKeyColumn));
        }

        OwnerForeignKeyColumn = ownerForeignKeyColumn;
        ConfigurationDeleteCommandLog.ScopedCommandCreated(
            NullLogger<ConfigurationDeleteCommand>.Instance, ownerLogicalId, ownerForeignKeyColumn);
    }

    /// <summary>
    /// Gets the logical owner FK column when this command retires an owner's whole child set,
    /// or <see langword="null"/> when it retires a single row by its own <c>[Id]</c>.
    /// </summary>
    public string? OwnerForeignKeyColumn { get; }
}
