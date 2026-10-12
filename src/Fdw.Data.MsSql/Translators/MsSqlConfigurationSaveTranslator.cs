using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;
using Fdw.Conventions;
using Fdw.Data.Abstractions;
using Fdw.Data.Abstractions.Mappers.PocoMappers;
using Fdw.Data.MsSql.Logging;
using Fdw.Data.MsSql.Results;
using Fdw.Data.MsSql.Translators;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// Translates a ConfigurationSaveCommand to a version-on-write T-SQL transaction that returns the key
/// values of the row it retires and of the row it inserts.
/// </summary>
/// <remarks>
/// <para>
/// Every name in the statement comes from the container's configuration:
/// <list type="bullet">
/// <item>The INSERT writes the record's mapped properties plus the command's field values, limited to
/// the fields the container declares as writable. A Foreign key field is one of those values; it is
/// never resolved here.</item>
/// <item>The UPDATE retires the current row the owner's Foreign key values match -- the field values
/// the provider passes -- together with the container's own Logical key where it declares one. An
/// implementation row is identified by its owner key alone; a child row among siblings by both.</item>
/// <item>Both OUTPUT the fields that the Foreign keys referencing this container point at
/// (<see cref="IDataContainer.ReferencingKeys"/>) -- the values the caller sets on the children.</item>
/// </list>
/// A save that carries neither owner key values nor a declared Logical key, or no value for a field of
/// the Logical key, fails: there is no fallback identity.
/// </para>
/// </remarks>
[TypeOption(typeof(MsSqlDataCommandTranslators), "ConfigurationSave", RestrictToCurrentCompilation = true)]
public sealed class MsSqlConfigurationSaveTranslator : MsSqlDataCommandTranslatorBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlConfigurationSaveTranslator"/> class.
    /// </summary>
    public MsSqlConfigurationSaveTranslator()
        : base("ConfigurationSave")
    {
    }

    /// <summary>
    /// Translates a ConfigurationSaveCommand to a T-SQL version-on-write transaction.
    /// </summary>
    /// <param name="command">The data command to translate.</param>
    /// <param name="container">The container with schema metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the SqlCommand.</returns>
    public override Task<IGenericResult<SqlCommand>> Translate(
        IDataCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken = default)
    {
        MsSqlConfigurationSaveTranslatorLog.Translating(
            NullLogger<MsSqlConfigurationSaveTranslator>.Instance, container?.Name ?? "<null>");

        try
        {
            if (container == null)
            {
                MsSqlConfigurationSaveTranslatorLog.ContainerNull(
                    NullLogger<MsSqlConfigurationSaveTranslator>.Instance);
                return Task.FromResult(
                    GenericResult<SqlCommand>.Failure(MsSqlDataResultCodes.ByName("ContainerNull")));
            }

            if (container.Path is not IDatabasePath dbPath)
            {
                MsSqlConfigurationSaveTranslatorLog.InvalidContainerPath(
                    NullLogger<MsSqlConfigurationSaveTranslator>.Instance, container.Name);
                return Task.FromResult(
                    GenericResult<SqlCommand>.Failure(MsSqlDataResultCodes.ByName("InvalidContainerPath")));
            }

            var dataObj = GetCommandData(command);
            if (dataObj == null)
            {
                MsSqlConfigurationSaveTranslatorLog.MissingInputData(
                    NullLogger<MsSqlConfigurationSaveTranslator>.Instance,
                    "ConfigurationSaveCommand", container.Name);
                return Task.FromResult(
                    GenericResult<SqlCommand>.Failure(
                        MsSqlDataResultCodes.ByName("MissingInputData"),
                        ResultDetails.Create("CommandType", "ConfigurationSaveCommand")));
            }

            if (command is not IConfigurationSaveCommand saveCommand)
            {
                MsSqlConfigurationSaveTranslatorLog.NotAConfigurationSaveCommand(
                    NullLogger<MsSqlConfigurationSaveTranslator>.Instance, command.GetType().Name);
                return Task.FromResult(
                    GenericResult<SqlCommand>.Failure(
                        MsSqlDataResultCodes.ByName("MissingInputData"),
                        ResultDetails.Create("CommandType", command.GetType().Name)));
            }

            if (container is not IDataContainer dataContainer)
            {
                return Task.FromResult(GenericResult<SqlCommand>.Failure(
                    MsSqlConfigurationSaveTranslatorLog.ContainerHasNoKeys(
                        NullLogger<MsSqlConfigurationSaveTranslator>.Instance, container.Name)));
            }

            var built = BuildVersionOnWriteSaveStatement(dbPath, dataContainer, dataObj, saveCommand.FieldValues, saveCommand.ReplacesCurrent);

            if (built.IsFailure)
                return Task.FromResult(built);

            MsSqlConfigurationSaveTranslatorLog.Translated(
                NullLogger<MsSqlConfigurationSaveTranslator>.Instance, container.Name, built.Value!.Parameters.Count);

            return Task.FromResult(built);
        }
        catch (Exception ex)
        {
            MsSqlConfigurationSaveTranslatorLog.SaveTranslationFailed(
                NullLogger<MsSqlConfigurationSaveTranslator>.Instance, ex, container?.Name ?? "<null>", ex.Message);
            return Task.FromResult(
                GenericResult<SqlCommand>.Failure(
                    MsSqlDataResultCodes.ByName("InsertTranslationFailed"),
                    ResultDetails.Create("ErrorMessage", ex.Message)));
        }
    }

    /// <summary>
    /// Fields the versioning writes itself, or that a record carries without their being columns.
    /// </summary>
    private static readonly HashSet<string> AlwaysExcluded = new(StringComparer.Ordinal)
    {
        "IsCurrent", "IsDeleted", "ServiceType", "SectionName"
    };

    /// <summary>
    /// Builds the version-on-write save: retire the current row the Logical key matches, insert the
    /// new one, and return the referenced key values of both.
    /// </summary>
    private static IGenericResult<SqlCommand> BuildVersionOnWriteSaveStatement(
        IDatabasePath dbPath,
        IDataContainer container,
        object data,
        IReadOnlyDictionary<string, object?> fieldValues,
        bool replacesCurrent)
    {
        var log = NullLogger<MsSqlConfigurationSaveTranslator>.Instance;
        if (string.IsNullOrEmpty(dbPath.Schema))
            return GenericResult<SqlCommand>.Failure(MsSqlConfigurationSaveTranslatorLog.NoSchemaDefined(log, container.Name));

        var mapper = PocoMapperCollection.ByName(data.GetType().Name);
        if (mapper == PocoMapperCollection.NotFound)
            return GenericResult<SqlCommand>.Failure(MsSqlConfigurationSaveTranslatorLog.NoPocoMapperRegistered(log, data.GetType().Name));

        var values = new Dictionary<string, object?>(mapper.MapToParameters(data), StringComparer.Ordinal);
        foreach (var field in fieldValues)
            values[field.Key] = field.Value;

        var columns = WrittenColumns(container, values);
        if (columns.IsFailure) return columns.ToNewResult<SqlCommand>();
        if (columns.Value!.Count == 0)
            return GenericResult<SqlCommand>.Failure(MsSqlConfigurationSaveTranslatorLog.NoInsertableColumns(log, data.GetType().Name, container.Name));

        // A row that replaces nothing -- a child written under its owner's new row -- is only inserted.
        var logical = replacesCurrent
            ? RetireMatchFields(container, fieldValues, values)
            : GenericResult<IReadOnlyList<string>>.Success([]);
        if (logical.IsFailure) return logical.ToNewResult<SqlCommand>();

        var returned = ReferencedFields(container);
        if (returned.IsFailure) return returned.ToNewResult<SqlCommand>();

        MsSqlConfigurationSaveTranslatorLog.SavePlanned(
            log, container.Name, string.Join(", ", logical.Value!), string.Join(", ", columns.Value), string.Join(", ", returned.Value!));

        var sqlCommand = CreateCommand(SaveSql(dbPath, container.Name, columns.Value, logical.Value!, returned.Value!));
        foreach (var name in columns.Value)
            AddParameter(sqlCommand, name, values[name]);

        return GenericResult<SqlCommand>.Success(sqlCommand);
    }

    private static string SaveSql(
        IDatabasePath dbPath, string table, IReadOnlyList<string> columns, IReadOnlyList<string> logical, IReadOnlyList<string> returned)
    {
        Func<string, string> qi = dbPath.Dialect.QuoteIdentifier;
        var p = dbPath.Dialect.ParameterPrefix;
        var target = $"{qi(dbPath.Schema!)}.{qi(table)}";

        var insert = $@"INSERT INTO {target} ({string.Join(", ", columns.Select(qi))}, {qi("IsCurrent")}, {qi("IsDeleted")})
    {Output(ConfigurationWriteResult.WrittenKind, "inserted", returned, qi)}
    VALUES ({string.Join(", ", columns.Select(c => p + c))}, 1, 0)";
        if (logical.Count == 0)
            return insert;

        var match = string.Join(" AND ", logical.Select(f => $"{qi(f)} = {p}{f}"));
        return $@"BEGIN TRANSACTION
  UPDATE {target} SET {qi("IsCurrent")} = 0, {qi("ModifyDate")} = SYSDATETIMEOFFSET()
    {Output(ConfigurationWriteResult.RetiredKind, "deleted", returned, qi)}
    WHERE {match} AND {qi("IsCurrent")} = 1
  {insert}
COMMIT";
    }

    // Every OUTPUT row says whether it reports a retired row or the inserted one, so the connection
    // splits the result on that column and never on the statement's shape. A container nothing
    // references still returns one row per row written.
    internal static string Output(string kind, string source, IReadOnlyList<string> fields, Func<string, string> qi)
        => $"OUTPUT '{kind}' AS {qi(ConfigurationWriteResult.KindColumn)}"
           + string.Concat(fields.Select(f => $", {source}.{qi(f)}"));

    /// <summary>
    /// The fields the Foreign keys referencing this container point at: the key values its children
    /// are filled from. Read from the container's configuration, never named here.
    /// </summary>
    internal static IGenericResult<IReadOnlyList<string>> ReferencedFields(IDataContainer container)
    {
        var referencing = container.ReferencingKeys;
        if (referencing.IsFailure) return referencing.ToNewResult<IReadOnlyList<string>>();

        var fields = new List<string>();
        foreach (var binding in referencing.Value!)
        {
            foreach (var keyField in binding.Key.KeyFields)
            {
                if (keyField.ReferencedField is not { } referenced)
                    return GenericResult<IReadOnlyList<string>>.Failure(
                        MsSqlConfigurationSaveTranslatorLog.ReferencedFieldNotResolved(
                            NullLogger<MsSqlConfigurationSaveTranslator>.Instance, binding.Key.KeyName, binding.Owner.Name, container.Name));
                if (!fields.Contains(referenced.Name, StringComparer.Ordinal))
                    fields.Add(referenced.Name);
            }
        }

        return GenericResult<IReadOnlyList<string>>.Success(fields);
    }

    /// <summary>
    /// The fields that identify the current row this save replaces: the owner's Foreign key fields the
    /// provider passed, then the fields of the container's own Logical key if it declares one, each of
    /// which must have a value on this save.
    /// </summary>
    private static IGenericResult<IReadOnlyList<string>> RetireMatchFields(
        IDataContainer container, IReadOnlyDictionary<string, object?> ownerKeyValues, Dictionary<string, object?> values)
    {
        var log = NullLogger<MsSqlConfigurationSaveTranslator>.Instance;
        var fields = new List<string>(ownerKeyValues.Keys);

        var key = container.Keys.FirstOrDefault(k =>
            k.ReferencedContainer is null && string.Equals(k.KeyType.Name, "Logical", StringComparison.Ordinal) && k.KeyFields.Count > 0);
        if (key is not null)
        {
            foreach (var keyField in key.KeyFields)
            {
                var name = keyField.LocalField.Name;
                if (!values.TryGetValue(name, out var value) || value is null)
                    return GenericResult<IReadOnlyList<string>>.Failure(
                        MsSqlConfigurationSaveTranslatorLog.LogicalKeyValueMissing(log, container.Name, key.KeyName, name));
                if (!fields.Contains(name, StringComparer.Ordinal))
                    fields.Add(name);
            }
        }

        return fields.Count == 0
            ? GenericResult<IReadOnlyList<string>>.Failure(MsSqlConfigurationSaveTranslatorLog.LogicalKeyNotDeclared(log, container.Name))
            : GenericResult<IReadOnlyList<string>>.Success(fields);
    }

    /// <summary>
    /// The fields the INSERT writes: those the container declares writable that this save has a value
    /// for. A NOT NULL field whose value is null is left out so its column default applies.
    /// </summary>
    private static IGenericResult<IReadOnlyList<string>> WrittenColumns(
        IDataContainer container, Dictionary<string, object?> values)
    {
        var fields = container.Schema?.Fields;
        if (fields is not { Count: > 0 })
            return GenericResult<IReadOnlyList<string>>.Failure(
                MsSqlConfigurationSaveTranslatorLog.ContainerFieldsNotDeclared(
                    NullLogger<MsSqlConfigurationSaveTranslator>.Instance, container.Name));

        var columns = new List<string>();
        foreach (var field in fields)
        {
            if (field.IsSystemProvided || field.IsIdentity || field.IsComputed) continue;
            if (AlwaysExcluded.Contains(field.Name)) continue;
            if (!values.TryGetValue(field.Name, out var value)) continue;
            if (value is null && !field.IsNullable) continue;
            columns.Add(field.Name);
        }

        return GenericResult<IReadOnlyList<string>>.Success(columns);
    }
}
