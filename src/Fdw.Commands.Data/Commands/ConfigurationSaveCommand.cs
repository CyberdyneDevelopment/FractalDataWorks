using System;
using System.Collections.Generic;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Commands.Data;

/// <summary>
/// Version-on-write save of one configuration row. Retires the current version its container's
/// Logical key matches, inserts the new version, and returns the key values of both.
/// </summary>
/// <typeparam name="T">The type of configuration record to save.</typeparam>
/// <remarks>
/// The row is the record's mapped properties plus <see cref="FieldValues"/>. A Foreign key field is
/// one of those values -- an owner sets it from its own key values, a record carries the ones it
/// cites -- so the translator writes it as it arrives and never resolves it.
/// </remarks>
[TypeOption(typeof(DataCommands), "ConfigurationSave", RestrictToCurrentCompilation = true)]
public sealed class ConfigurationSaveCommand<T> : DataCommandBase<ConfigurationWriteResult, T>, IConfigurationSaveCommand
    where T : class
{
    private static readonly IReadOnlyDictionary<string, object?> NoFieldValues =
        new Dictionary<string, object?>(0, StringComparer.Ordinal);

    /// <summary>Initializes a save of a record whose fields are all mapped properties.</summary>
    /// <param name="data">The record to save.</param>
    /// <param name="replacesCurrent">Whether the save retires a current version of the row before inserting.</param>
    public ConfigurationSaveCommand(T data, bool replacesCurrent)
        : this(data, NoFieldValues, replacesCurrent)
    {
    }

    /// <summary>Initializes a save of a record plus field values its properties do not carry.</summary>
    /// <param name="data">The record to save.</param>
    /// <param name="fieldValues">Field values written with the record, keyed by field name.</param>
    /// <param name="replacesCurrent">Whether the save retires a current version of the row before inserting.</param>
    public ConfigurationSaveCommand(T data, IReadOnlyDictionary<string, object?> fieldValues, bool replacesCurrent)
        : base("ConfigurationSave", data)
    {
        FieldValues = fieldValues;
        ReplacesCurrent = replacesCurrent;

        ConfigurationSaveCommandLog.CommandCreated(NullLogger<ConfigurationSaveCommand<T>>.Instance, typeof(T).Name);
        if (FieldValues.Count > 0)
        {
            ConfigurationSaveCommandLog.FieldValuesIncluded(
                NullLogger<ConfigurationSaveCommand<T>>.Instance, typeof(T).Name, string.Join(", ", FieldValues.Keys));
        }
    }

    /// <inheritdoc/>
    public Type ConfigurationType => typeof(T);

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, object?> FieldValues { get; }

    /// <inheritdoc/>
    public bool ReplacesCurrent { get; }
}
