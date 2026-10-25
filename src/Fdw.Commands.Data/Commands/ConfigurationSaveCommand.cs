using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.Logging;
using Fdw.Data.Abstractions;
using Fdw.Data.Abstractions.Mappers.PocoMappers;
using Fdw.Results;
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
/// cites -- so the insert writes it as it arrives and never resolves it. The save expands into a query of
/// the current rows it replaces, the close of those rows, and the insert of the new version with the
/// referenced key columns returned; a save that replaces nothing is only the insert.
/// </remarks>
[TypeOption(typeof(DataCommands), "ConfigurationSave", RestrictToCurrentCompilation = true)]
public sealed class ConfigurationSaveCommand<T> : ConfigurationWriteCommand<T>, IConfigurationSaveCommand
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

    /// <inheritdoc/>
    public override IGenericResult<IReadOnlyList<ExpandedCommand>> Expand(IStorageContainer container)
    {
        var log = NullLogger<ConfigurationSaveCommand<T>>.Instance;
        if (container is not IDataContainer dataContainer)
            return Refused(ConfigurationWriteLog.ContainerHasNoKeys(log, CommandType, container.Name));

        var mapper = PocoMapperCollection.ByName(typeof(T).Name);
        if (mapper == PocoMapperCollection.NotFound)
            return Refused(ConfigurationWriteLog.NoPocoMapperRegistered(log, typeof(T).Name));

        var values = new Dictionary<string, object?>(mapper.MapToParameters(Data), StringComparer.Ordinal);
        foreach (var field in FieldValues)
            values[field.Key] = field.Value;

        var row = WrittenRow(dataContainer, values);
        if (row.IsFailure)
            return row.ToNewResult<IReadOnlyList<ExpandedCommand>>();
        if (row.Value!.Count == 0)
            return Refused(ConfigurationWriteLog.NoInsertableColumns(log, typeof(T).Name, container.Name));

        var returned = ReferencedFields(dataContainer);
        if (returned.IsFailure)
            return returned.ToNewResult<IReadOnlyList<ExpandedCommand>>();

        var steps = new List<ExpandedCommand>();
        var matched = new List<string>();
        if (ReplacesCurrent)
        {
            var match = Match(dataContainer, values);
            if (match.IsFailure)
                return match.ToNewResult<IReadOnlyList<ExpandedCommand>>();

            matched.AddRange(match.Value!.Keys);
            steps.Add(ReadCurrentRows(match.Value, returned.Value!, notDeleted: false));
            steps.Add(FlagCurrentRows(match.Value, CurrentColumn, flagValue: false));
        }

        ConfigurationWriteLog.SavePlanned(log, container.Name, string.Join(", ", matched), string.Join(", ", row.Value.Keys), string.Join(", ", returned.Value!));
        steps.Add(new ExpandedCommand(
            new InsertCommand<Dictionary<string, object?>>(row.Value) { ReturnColumns = returned.Value!.Count > 0 ? returned.Value : null },
            AnswersRows: returned.Value.Count > 0));
        return GenericResult<IReadOnlyList<ExpandedCommand>>.Success(steps);
    }

    /// <inheritdoc/>
    public override IGenericResult<ConfigurationWriteResult> Assemble(IReadOnlyList<ExpansionAnswer> answers)
    {
        var log = NullLogger<ConfigurationSaveCommand<T>>.Instance;
        if (answers.Count != (ReplacesCurrent ? 3 : 1) || (ReplacesCurrent && answers[0].Rows is null))
            return GenericResult<ConfigurationWriteResult>.Failure(ConfigurationWriteLog.AnswersNotAssemblable(log, CommandType, answers.Count));

        var retired = ReplacesCurrent ? answers[0].Rows! : [];
        var insert = answers[^1];
        if (insert.Rows is null)
            return GenericResult<ConfigurationWriteResult>.Success(new ConfigurationWriteResult(retired, new Dictionary<string, object?>(0, StringComparer.Ordinal)));

        return insert.Rows.Count == 0
            ? GenericResult<ConfigurationWriteResult>.Failure(ConfigurationWriteLog.InsertAnsweredNoRow(log))
            : GenericResult<ConfigurationWriteResult>.Success(new ConfigurationWriteResult(retired, insert.Rows[0]));
    }

    // The fields that identify the current row a save replaces: the owner's Foreign key fields the provider passed, then the
    // fields of the container's own Logical key if it declares one, each of which must have a value.
    private IGenericResult<IReadOnlyDictionary<string, object?>> Match(IDataContainer container, Dictionary<string, object?> values)
    {
        var log = NullLogger<ConfigurationSaveCommand<T>>.Instance;
        var names = new List<string>(FieldValues.Keys);
        var key = container.Keys.FirstOrDefault(k =>
            k.ReferencedContainer is null && string.Equals(k.KeyType.Name, "Logical", StringComparison.Ordinal) && k.KeyFields.Count > 0);
        if (key is not null)
        {
            foreach (var keyField in key.KeyFields)
            {
                var name = keyField.LocalField.Name;
                if (!values.TryGetValue(name, out var value) || value is null)
                    return GenericResult<IReadOnlyDictionary<string, object?>>.Failure(
                        ConfigurationWriteLog.LogicalKeyValueMissing(log, container.Name, key.KeyName, name));
                if (!names.Contains(name, StringComparer.Ordinal))
                    names.Add(name);
            }
        }

        if (names.Count == 0)
            return GenericResult<IReadOnlyDictionary<string, object?>>.Failure(ConfigurationWriteLog.LogicalKeyNotDeclared(log, container.Name));

        var match = new Dictionary<string, object?>(names.Count, StringComparer.Ordinal);
        foreach (var name in names)
        {
            if (!values.TryGetValue(name, out var value) || value is null)
                return GenericResult<IReadOnlyDictionary<string, object?>>.Failure(ConfigurationWriteLog.MatchValueMissing(log, CommandType, container.Name, name));
            match[name] = value;
        }

        return GenericResult<IReadOnlyDictionary<string, object?>>.Success(match);
    }

    // The row the insert writes: the fields the container declares writable that this save has a value for. A NOT NULL field
    // whose value is null is left out so its column default applies. The versioning columns and the two fields a record
    // carries without their being columns are never written from the record; the save writes the versioning columns itself.
    private IGenericResult<Dictionary<string, object?>> WrittenRow(IDataContainer container, Dictionary<string, object?> values)
    {
        var fields = container.Schema.Fields;
        if (fields.Count == 0)
            return GenericResult<Dictionary<string, object?>>.Failure(
                ConfigurationWriteLog.ContainerFieldsNotDeclared(NullLogger<ConfigurationSaveCommand<T>>.Instance, CommandType, container.Name));

        var row = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var field in fields)
        {
            if (field.IsSystemProvided || field.IsIdentity || field.IsComputed || IsVersioningOrCarried(field.Name))
                continue;
            if (!values.TryGetValue(field.Name, out var value) || (value is null && !field.IsNullable))
                continue;
            row[field.Name] = value;
        }

        // The new version is the current one and is not deleted. A version column the container does not declare is refused by name.
        foreach (var (column, value) in new[] { (CurrentColumn, true), (DeletedColumn, false) })
        {
            if (!fields.Any(f => string.Equals(f.Name, column, StringComparison.Ordinal)))
                return GenericResult<Dictionary<string, object?>>.Failure(
                    ConfigurationWriteLog.VersionColumnNotDeclared(NullLogger<ConfigurationSaveCommand<T>>.Instance, CommandType, container.Name, column));
            row[column] = value;
        }

        return GenericResult<Dictionary<string, object?>>.Success(row);
    }

    private static bool IsVersioningOrCarried(string name)
        => string.Equals(name, CurrentColumn, StringComparison.Ordinal)
            || string.Equals(name, DeletedColumn, StringComparison.Ordinal)
            || string.Equals(name, "ServiceType", StringComparison.Ordinal)
            || string.Equals(name, "SectionName", StringComparison.Ordinal);
}
