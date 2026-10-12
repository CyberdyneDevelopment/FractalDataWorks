using System;
using System.Collections.Generic;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.Logging;
using Fdw.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Commands.Data;

/// <summary>
/// Retires the current configuration rows that match, marking them deleted when asked, and returns
/// the key values of every row it retired.
/// </summary>
/// <remarks>
/// A delete marks the rows deleted. A save retires the children of the version it replaces without
/// deleting them, because the new version re-inserts the children it still has.
/// </remarks>
[TypeOption(typeof(DataCommands), "ConfigurationRetire", RestrictToCurrentCompilation = true)]
public sealed class ConfigurationRetireCommand
    : DataCommandBase<ConfigurationWriteResult, IReadOnlyDictionary<string, object?>>, IConfigurationRetireCommand
{
    private static readonly IReadOnlyDictionary<string, object?> NoFieldValues =
        new Dictionary<string, object?>(0, StringComparer.Ordinal);

    /// <summary>Initializes a retire of the rows whose fields hold these values.</summary>
    /// <param name="fieldValues">The field values to match, keyed by field name.</param>
    /// <param name="markDeleted">Whether the retired rows are marked deleted.</param>
    public ConfigurationRetireCommand(IReadOnlyDictionary<string, object?> fieldValues, bool markDeleted)
        : base("ConfigurationRetire", fieldValues)
    {
        FieldValues = fieldValues;
        KeyValues = [];
        MarkDeleted = markDeleted;
        ConfigurationRetireCommandLog.FieldMatchCreated(
            NullLogger<ConfigurationRetireCommand>.Instance, string.Join(", ", fieldValues.Keys), markDeleted);
    }

    /// <summary>Initializes a retire of the row whose declared key of <paramref name="keyType"/> holds these values.</summary>
    /// <param name="keyType">The type of the declared key to match on.</param>
    /// <param name="keyValues">The key's field values, in key-field order.</param>
    /// <param name="markDeleted">Whether the retired row is marked deleted.</param>
    public ConfigurationRetireCommand(KeyTypeBase keyType, IReadOnlyList<object?> keyValues, bool markDeleted)
        : base("ConfigurationRetire", NoFieldValues)
    {
        KeyType = keyType;
        KeyValues = keyValues;
        MarkDeleted = markDeleted;
        ConfigurationRetireCommandLog.KeyMatchCreated(
            NullLogger<ConfigurationRetireCommand>.Instance, keyType.Name, keyValues.Count, markDeleted);
    }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, object?>? FieldValues { get; }

    /// <inheritdoc/>
    public KeyTypeBase? KeyType { get; }

    /// <inheritdoc/>
    public IReadOnlyList<object?> KeyValues { get; }

    /// <inheritdoc/>
    public bool MarkDeleted { get; }
}
