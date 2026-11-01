using System;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Data.MsSql.Logging;
using Fdw.Data.MsSql.Results;
using Fdw.Data.MsSql.Translators;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Sql;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// Translates a nearest search (<see cref="INearestCommand"/>) into T-SQL over a <c>vector</c> column (FDW-1354):
/// <code>
/// SELECT TOP (@take) [Id], [Text], VECTOR_DISTANCE('Cosine', [Vector], CAST(@vector AS VECTOR(3))) AS [Distance]
/// FROM [tc].[ConversationPassage]
/// WHERE [Vector] IS NOT NULL
/// ORDER BY VECTOR_DISTANCE('Cosine', [Vector], CAST(@vector AS VECTOR(3)))
/// </code>
/// </summary>
/// <remarks>
/// The columns are those a query returns by default, less the vector measured: the distance stands for it. The distance is
/// always the last column, and the connection reads it by position, so a field of the row named Distance is never taken
/// for it. The vector travels as its JSON text, cast to a vector of its own length; a column of another length is SQL
/// Server's refusal, by name. SQL Server names its metrics as <see cref="VectorMetrics"/> does, in any case.
/// </remarks>
[TypeOption(typeof(MsSqlVectorizedDataCommandTranslators), "Nearest", RestrictToCurrentCompilation = true)]
public sealed class MsSqlNearestTranslator : MsSqlVectorizedDataCommandTranslatorBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlNearestTranslator"/> class.
    /// </summary>
    public MsSqlNearestTranslator()
        : base("Nearest")
    {
    }

    /// <inheritdoc/>
    public override Task<IGenericResult<SqlCommand>> Translate(
        IVectorizedDataCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TranslateNearest(command, container));

    private static IGenericResult<SqlCommand> TranslateNearest(IVectorizedDataCommand command, IStorageContainer container)
    {
        var logger = NullLogger<MsSqlNearestTranslator>.Instance;
        if (command is not INearestCommand nearest)
        {
            return GenericResult<SqlCommand>.Failure(MsSqlTranslatorLog.InvalidCommandType(
                logger, nameof(MsSqlNearestTranslator), nameof(INearestCommand), command.GetType().Name));
        }

        if (container is null)
            return GenericResult<SqlCommand>.Failure(MsSqlNearestTranslatorLog.ContainerNull(logger));
        if (container.Path is not IDatabasePath path)
            return GenericResult<SqlCommand>.Failure(MsSqlNearestTranslatorLog.InvalidContainerPath(logger, container.Name));

        MsSqlNearestTranslatorLog.Translating(logger, container.Name, nearest.VectorField);
        var field = container.Schema.Fields.FirstOrDefault(f => string.Equals(f.Name, nearest.VectorField, StringComparison.OrdinalIgnoreCase));
        if (field is null)
            return Refused(logger, container.Name, $"'{nearest.VectorField}' is not a field of '{container.Name}'");

        var reason = Refusal(nearest, field);
        if (reason is not null)
            return Refused(logger, container.Name, reason);

        try
        {
            return GenericResult<SqlCommand>.Success(Statement(nearest, field, container, path));
        }
        catch (Exception ex)
        {
            MsSqlNearestTranslatorLog.TranslationFailed(logger, ex, container.Name, ex.Message);
            return GenericResult<SqlCommand>.Failure(
                MsSqlDataResultCodes.ByName("NearestTranslationFailed"), ResultDetails.Create("ErrorMessage", ex.Message));
        }
    }

    private static IGenericResult<SqlCommand> Refused(ILogger logger, string container, string reason)
    {
        MsSqlNearestTranslatorLog.InvalidOptions(logger, container, reason);
        return GenericResult<SqlCommand>.Failure(MsSqlDataResultCodes.ByName("InvalidNearestOptions"), ResultDetails.Create("Reason", reason));
    }

    // Why the search cannot be built as asked, or none when it can.
    private static string? Refusal(INearestCommand nearest, IField field)
    {
        if (field.FieldType.ClrType != typeof(float[]))
            return $"'{field.Name}' is a {field.FieldType.TypeName} field, not a vector field";
        if (nearest.Vector.Count == 0)
            return "the vector has no numbers";
        if (nearest.Take < 1)
            return "it asks for no rows";
        if (nearest.Metric == VectorMetrics.NotFound)
            return "it names no metric";

        // Why letters only: the metric's name is written into the statement, and every metric SQL Server has is a word.
        return nearest.Metric.Name.All(char.IsLetter) ? null : $"'{nearest.Metric.Name}' is not a metric SQL Server measures";
    }

    private static SqlCommand Statement(INearestCommand nearest, IField field, IStorageContainer container, IDatabasePath path)
    {
        var dialect = path.Dialect;
        var vectorColumn = dialect.QuoteIdentifier(field.Name);
        var columns = container.Schema
            .GetReturnedFields(includeMetadata: false, includeDescriptions: false, includeKeys: false)
            .Where(f => !string.Equals(f.Name, field.Name, StringComparison.OrdinalIgnoreCase))
            .Select(f => dialect.QuoteIdentifier(f.Name))
            .ToList();
        var take = $"{dialect.ParameterPrefix}take";
        var vector = $"{dialect.ParameterPrefix}vector";
        var distance = string.Create(CultureInfo.InvariantCulture,
            $"VECTOR_DISTANCE('{nearest.Metric.Name}', {vectorColumn}, CAST({vector} AS VECTOR({nearest.Vector.Count})))");

        var command = new SqlCommand(string.Create(CultureInfo.InvariantCulture,
            $"SELECT TOP ({take}) {string.Concat(columns.Select(c => c + ", "))}{distance} AS {dialect.QuoteIdentifier("Distance")} FROM {SqlNames.QualifiedTable(path)} WHERE {vectorColumn} IS NOT NULL ORDER BY {distance}"));
        command.Parameters.Add(new SqlParameter(take, SqlDbType.Int) { Value = nearest.Take });
        command.Parameters.Add(new SqlParameter(vector, SqlDbType.NVarChar, -1) { Value = DataTypes.Vector.Format(nearest.Vector.ToArray()) });

        MsSqlNearestTranslatorLog.Translated(NullLogger<MsSqlNearestTranslator>.Instance, container.Name, nearest.Metric.Name, nearest.Take, columns.Count);
        return command;
    }
}
