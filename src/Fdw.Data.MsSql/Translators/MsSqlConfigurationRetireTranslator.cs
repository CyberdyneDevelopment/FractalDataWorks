using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Data.MsSql.Logging;
using Fdw.Data.MsSql.Translators;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// Translates a ConfigurationRetireCommand to one T-SQL UPDATE that retires the matching current rows
/// and returns their key values.
/// </summary>
/// <remarks>
/// <para>
/// The rows are matched on the command's field values, or on the fields of the key it names -- both
/// read against the container's configuration, never assumed. The UPDATE outputs the fields that the
/// Foreign keys referencing this container point at, so the caller can retire the children of every
/// row it retired. A retire that matches on nothing is refused: it would retire the whole table.
/// </para>
/// </remarks>
[TypeOption(typeof(MsSqlDataCommandTranslators), "ConfigurationRetire", RestrictToCurrentCompilation = true)]
public sealed class MsSqlConfigurationRetireTranslator : MsSqlDataCommandTranslatorBase
{
    /// <summary>Initializes a new instance of the <see cref="MsSqlConfigurationRetireTranslator"/> class.</summary>
    public MsSqlConfigurationRetireTranslator()
        : base("ConfigurationRetire")
    {
    }

    /// <summary>Translates a ConfigurationRetireCommand to a T-SQL retire.</summary>
    /// <param name="command">The data command to translate.</param>
    /// <param name="container">The container with schema metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the SqlCommand.</returns>
    public override Task<IGenericResult<SqlCommand>> Translate(
        IDataCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken = default)
    {
        var log = NullLogger<MsSqlConfigurationRetireTranslator>.Instance;
        MsSqlConfigurationRetireTranslatorLog.Translating(log, container?.Name ?? "<null>");

        try
        {
            if (container is null)
                return Task.FromResult(GenericResult<SqlCommand>.Failure(MsSqlConfigurationRetireTranslatorLog.ContainerNull(log)));

            if (container.Path is not IDatabasePath dbPath)
                return Task.FromResult(GenericResult<SqlCommand>.Failure(
                    MsSqlConfigurationRetireTranslatorLog.InvalidContainerPath(log, container.Name)));

            if (command is not IConfigurationRetireCommand retire || container is not IDataContainer dataContainer)
                return Task.FromResult(GenericResult<SqlCommand>.Failure(
                    MsSqlConfigurationRetireTranslatorLog.NotARetireCommand(log, command.GetType().Name, container.Name)));

            var built = BuildRetireStatement(dbPath, dataContainer, retire);
            if (built.IsSuccess)
                MsSqlConfigurationRetireTranslatorLog.Translated(log, container.Name);

            return Task.FromResult(built);
        }
        catch (Exception ex)
        {
            return Task.FromResult(GenericResult<SqlCommand>.Failure(
                MsSqlConfigurationRetireTranslatorLog.RetireTranslationFailed(log, ex, container?.Name ?? "<null>", ex.Message)));
        }
    }

    private static IGenericResult<SqlCommand> BuildRetireStatement(
        IDatabasePath dbPath, IDataContainer container, IConfigurationRetireCommand retire)
    {
        var log = NullLogger<MsSqlConfigurationRetireTranslator>.Instance;
        if (string.IsNullOrEmpty(dbPath.Schema))
            return GenericResult<SqlCommand>.Failure(MsSqlConfigurationRetireTranslatorLog.NoSchemaDefined(log, container.Name));

        var match = MatchValues(container, retire);
        if (match.IsFailure) return match.ToNewResult<SqlCommand>();

        var returned = MsSqlConfigurationSaveTranslator.ReferencedFields(container);
        if (returned.IsFailure) return returned.ToNewResult<SqlCommand>();

        MsSqlConfigurationRetireTranslatorLog.RetirePlanned(
            log, container.Name, string.Join(", ", match.Value!.Keys), retire.MarkDeleted, string.Join(", ", returned.Value!));

        Func<string, string> qi = dbPath.Dialect.QuoteIdentifier;
        var p = dbPath.Dialect.ParameterPrefix;
        var deleted = retire.MarkDeleted ? $", {qi("IsDeleted")} = 1" : string.Empty;
        var sql = $@"UPDATE {qi(dbPath.Schema)}.{qi(container.Name)}
  SET {qi("IsCurrent")} = 0{deleted}, {qi("ModifyDate")} = SYSDATETIMEOFFSET()
  {MsSqlConfigurationSaveTranslator.Output(ConfigurationWriteResult.RetiredKind, "deleted", returned.Value!, qi)}
  WHERE {string.Join(" AND ", match.Value.Keys.Select(f => $"{qi(f)} = {p}{f}"))} AND {qi("IsCurrent")} = 1";

        var sqlCommand = CreateCommand(sql);
        foreach (var field in match.Value)
            AddParameter(sqlCommand, field.Key, field.Value);

        return GenericResult<SqlCommand>.Success(sqlCommand);
    }

    /// <summary>
    /// The fields to match and their values: the command's field values, or the fields of the key it
    /// names paired with its key values. Every field must be one the container declares.
    /// </summary>
    private static IGenericResult<IReadOnlyDictionary<string, object?>> MatchValues(
        IDataContainer container, IConfigurationRetireCommand retire)
    {
        var log = NullLogger<MsSqlConfigurationRetireTranslator>.Instance;
        var match = new Dictionary<string, object?>(StringComparer.Ordinal);

        if (retire.KeyType is { } keyType)
        {
            var key = container.Keys.FirstOrDefault(k =>
                k.ReferencedContainer is null && string.Equals(k.KeyType.Name, keyType.Name, StringComparison.Ordinal) && k.KeyFields.Count > 0);
            if (key is null)
                return GenericResult<IReadOnlyDictionary<string, object?>>.Failure(
                    MsSqlConfigurationRetireTranslatorLog.KeyNotDeclared(log, container.Name, keyType.Name));
            if (key.KeyFields.Count != retire.KeyValues.Count)
                return GenericResult<IReadOnlyDictionary<string, object?>>.Failure(
                    MsSqlConfigurationRetireTranslatorLog.KeyValueCountMismatch(log, container.Name, keyType.Name, key.KeyFields.Count, retire.KeyValues.Count));

            for (var i = 0; i < key.KeyFields.Count; i++)
                match[key.KeyFields[i].LocalField.Name] = retire.KeyValues[i];
        }
        else if (retire.FieldValues is { } fieldValues)
        {
            var declared = container.Schema?.Fields;
            foreach (var field in fieldValues)
            {
                if (declared is null || !declared.Any(f => string.Equals(f.Name, field.Key, StringComparison.Ordinal)))
                    return GenericResult<IReadOnlyDictionary<string, object?>>.Failure(
                        MsSqlConfigurationRetireTranslatorLog.MatchFieldNotDeclared(log, container.Name, field.Key));
                match[field.Key] = field.Value;
            }
        }

        return match.Count == 0
            ? GenericResult<IReadOnlyDictionary<string, object?>>.Failure(MsSqlConfigurationRetireTranslatorLog.NothingToMatch(log, container.Name))
            : GenericResult<IReadOnlyDictionary<string, object?>>.Success(match);
    }
}
