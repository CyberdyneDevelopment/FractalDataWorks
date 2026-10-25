using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.Logging;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Commands.Data;

/// <summary>
/// Retires the current configuration rows that match and returns the key values of every row it
/// retired.
/// </summary>
/// <remarks>
/// A delete marks the rows deleted and leaves them current. A save retires the children of the version
/// it replaces without deleting them, because the new version re-inserts the children it still has.
/// </remarks>
[TypeOption(typeof(DataCommands), "ConfigurationRetire", RestrictToCurrentCompilation = true)]
public sealed class ConfigurationRetireCommand
    : ConfigurationWriteCommand<IReadOnlyDictionary<string, object?>>, IConfigurationRetireCommand
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

    /// <inheritdoc/>
    public override IGenericResult<IReadOnlyList<ExpandedCommand>> Expand(IStorageContainer container)
    {
        var log = NullLogger<ConfigurationRetireCommand>.Instance;
        if (container is not IDataContainer dataContainer)
            return Refused(ConfigurationWriteLog.ContainerHasNoKeys(log, CommandType, container.Name));

        var match = Match(dataContainer);
        if (match.IsFailure)
            return match.ToNewResult<IReadOnlyList<ExpandedCommand>>();

        var returned = ReferencedFields(dataContainer);
        if (returned.IsFailure)
            return returned.ToNewResult<IReadOnlyList<ExpandedCommand>>();

        ConfigurationWriteLog.RetirePlanned(log, container.Name, string.Join(", ", match.Value!.Keys), MarkDeleted, string.Join(", ", returned.Value!));
        return GenericResult<IReadOnlyList<ExpandedCommand>>.Success(
        [
            ReadCurrentRows(match.Value, returned.Value!, notDeleted: MarkDeleted),
            FlagCurrentRows(match.Value, MarkDeleted ? DeletedColumn : CurrentColumn, flagValue: MarkDeleted),
        ]);
    }

    /// <inheritdoc/>
    public override IGenericResult<ConfigurationWriteResult> Assemble(IReadOnlyList<ExpansionAnswer> answers)
        => answers.Count != 2 || answers[0].Rows is null
            ? GenericResult<ConfigurationWriteResult>.Failure(
                ConfigurationWriteLog.AnswersNotAssemblable(NullLogger<ConfigurationRetireCommand>.Instance, CommandType, answers.Count))
            : GenericResult<ConfigurationWriteResult>.Success(new ConfigurationWriteResult(answers[0].Rows!, null));

    // The fields to match and their values: the command's field values, or the fields of the key it names paired with its key
    // values. Every field must be one the container declares, and every one must have a value.
    private IGenericResult<IReadOnlyDictionary<string, object?>> Match(IDataContainer container)
    {
        var log = NullLogger<ConfigurationRetireCommand>.Instance;
        var match = new Dictionary<string, object?>(StringComparer.Ordinal);
        if (KeyType is { } keyType)
        {
            var key = container.Keys.FirstOrDefault(k =>
                k.ReferencedContainer is null && string.Equals(k.KeyType.Name, keyType.Name, StringComparison.Ordinal) && k.KeyFields.Count > 0);
            if (key is null)
                return GenericResult<IReadOnlyDictionary<string, object?>>.Failure(ConfigurationWriteLog.KeyNotDeclared(log, container.Name, keyType.Name));
            if (key.KeyFields.Count != KeyValues.Count)
                return GenericResult<IReadOnlyDictionary<string, object?>>.Failure(
                    ConfigurationWriteLog.KeyValueCountMismatch(log, container.Name, keyType.Name, key.KeyFields.Count, KeyValues.Count));

            for (var i = 0; i < key.KeyFields.Count; i++)
                match[key.KeyFields[i].LocalField.Name] = KeyValues[i];
        }
        else if (FieldValues is { } fieldValues)
        {
            var declared = container.Schema.Fields;
            foreach (var field in fieldValues)
            {
                if (!declared.Any(f => string.Equals(f.Name, field.Key, StringComparison.Ordinal)))
                    return GenericResult<IReadOnlyDictionary<string, object?>>.Failure(ConfigurationWriteLog.MatchFieldNotDeclared(log, container.Name, field.Key));
                match[field.Key] = field.Value;
            }
        }

        if (match.Count == 0)
            return GenericResult<IReadOnlyDictionary<string, object?>>.Failure(ConfigurationWriteLog.NothingToMatch(log, container.Name));

        var empty = match.FirstOrDefault(m => m.Value is null);
        return empty.Key is not null
            ? GenericResult<IReadOnlyDictionary<string, object?>>.Failure(ConfigurationWriteLog.MatchValueMissing(log, CommandType, container.Name, empty.Key))
            : GenericResult<IReadOnlyDictionary<string, object?>>.Success(match);
    }
}
