using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Commands.Abstractions;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Commands.Data;

/// <summary>
/// Configuration save command for version-on-write upsert operations.
/// Marks the current row as non-current and inserts a new current version.
/// Returns the number of affected rows.
/// </summary>
/// <typeparam name="T">The type of configuration entity to save.</typeparam>
/// <remarks>
/// <para>
/// This command implements the version-on-write pattern used by all FDW configuration tables:
/// <list type="bullet">
/// <item>UPDATE: Sets IsCurrent = 0 on the existing current row (if any)</item>
/// <item>INSERT: Inserts a new row with IsCurrent = 1, IsDeleted = 0</item>
/// </list>
/// </para>
/// <para>
/// When T has a [ManagedConfiguration] attribute with a non-null ParentTableName, the DataGateway
/// detects this via <see cref="IConfigurationSaveCommand.ConfigurationType"/> and dispatches
/// the save as a cascade: parent row first, then child row, wrapped in a single transaction.
/// </para>
/// <para>
/// Example usage:
/// <code>
/// var call = new DataGatewayCall(
///     new ConfigurationSaveCommand&lt;DataSetConfiguration&gt;(config),
///     new DataStoreTarget("ConfigurationDb", "cfg", "DataSet"));
/// var result = await gateway.Execute&lt;int&gt;(call, ct);
/// </code>
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataCommands), "ConfigurationSave", RestrictToCurrentCompilation = true)]
public sealed class ConfigurationSaveCommand<T> : DataCommandBase<int, T>, IConfigurationSaveCommand
    where T : class
{
    // Why: the ONLY sanctioned default — an empty-collection sentinel (not a value fallback) so the
    // single-arg constructor need not allocate a new dictionary per ordinary (non-KVP-child) save.
    private static readonly IReadOnlyDictionary<string, object?> EmptyReadOnlyDictionary =
        new Dictionary<string, object?>(0, StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationSaveCommand{T}"/> class.
    /// </summary>
    /// <param name="data">The configuration entity to save.</param>
    public ConfigurationSaveCommand(T data)
        : this(data, EmptyReadOnlyDictionary)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationSaveCommand{T}"/> class with extra
    /// column=value pairs to merge into the INSERT beyond the POCO's mapped columns (e.g. a KVP
    /// child row's logical owner FK, which has no corresponding property on <typeparamref name="T"/>).
    /// </summary>
    /// <param name="data">The configuration entity to save.</param>
    /// <param name="additionalColumnValues">Extra column=value pairs merged into the INSERT.</param>
    public ConfigurationSaveCommand(T data, IReadOnlyDictionary<string, object?> additionalColumnValues)
        : base("ConfigurationSave", data)
    {
        AdditionalColumnValues = additionalColumnValues ?? EmptyReadOnlyDictionary;

        ConfigurationSaveCommandLog.CommandCreated(NullLogger<ConfigurationSaveCommand<T>>.Instance, typeof(T).Name);
        if (AdditionalColumnValues.Count > 0)
        {
            ConfigurationSaveCommandLog.AdditionalColumnsIncluded(
                NullLogger<ConfigurationSaveCommand<T>>.Instance, typeof(T).Name, AdditionalColumnValues.Count);
        }
    }

    /// <inheritdoc/>
    // Why: Exposes the closed generic T as a runtime Type so the cascade handler in
    // DataGatewayService can call ConfigurationTypes.ByName(type.Name) without needing
    // an open-generic branch per T. No reflection on the caller side.
    public Type ConfigurationType => typeof(T);

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, object?> AdditionalColumnValues { get; }
}
