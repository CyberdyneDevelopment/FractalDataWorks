using System;
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
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// Translates TruncateCommand to an unconditional T-SQL DELETE statement (empties the container).
/// </summary>
/// <remarks>
/// <para>
/// Emits <c>DELETE FROM &lt;table&gt;</c> with NO WHERE clause. This is the explicit "empty the sink"
/// intent (pipeline TruncateBeforeLoad) — distinct from <c>DeleteCommand</c>, whose translator REQUIRES
/// a filter to guard against accidental where-less deletes.
/// </para>
/// <para>
/// DELETE — not <c>TRUNCATE TABLE</c> — is emitted on purpose: TRUNCATE requires ALTER/DDL permission
/// and fails on tables referenced by a foreign key, whereas an unconditional DELETE works with a plain
/// DELETE grant on any table.
/// </para>
/// </remarks>
[TypeOption(typeof(MsSqlDataCommandTranslators), "Truncate", RestrictToCurrentCompilation = true)]
public sealed class MsSqlTruncateTranslator : MsSqlDataCommandTranslatorBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlTruncateTranslator"/> class.
    /// </summary>
    public MsSqlTruncateTranslator()
        : base("Truncate")
    {
    }

    /// <summary>
    /// Translates a TruncateCommand to an unconditional T-SQL DELETE statement.
    /// </summary>
    public override Task<IGenericResult<SqlCommand>> Translate(
        IDataCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken = default)
    {
        MsSqlTruncateTranslatorLog.Translating(
            NullLogger<MsSqlTruncateTranslator>.Instance, container?.Name ?? "<null>");

        try
        {
            if (container == null)
            {
                MsSqlTruncateTranslatorLog.ContainerNull(NullLogger<MsSqlTruncateTranslator>.Instance);
                return Task.FromResult(
                    GenericResult<SqlCommand>.Failure(MsSqlDataResultCodes.ByName("ContainerNull")));
            }

            if (container.Path is not IDatabasePath dbPath)
            {
                MsSqlTruncateTranslatorLog.InvalidContainerPath(
                    NullLogger<MsSqlTruncateTranslator>.Instance, container.Name);
                return Task.FromResult(
                    GenericResult<SqlCommand>.Failure(MsSqlDataResultCodes.ByName("InvalidContainerPath")));
            }

            var sqlCommand = CreateCommand($"DELETE FROM {BuildQualifiedTableName(dbPath)}");

            MsSqlTruncateTranslatorLog.Translated(NullLogger<MsSqlTruncateTranslator>.Instance, container.Name);

            return Task.FromResult(GenericResult<SqlCommand>.Success(sqlCommand));
        }
        catch (Exception ex)
        {
            MsSqlTruncateTranslatorLog.TruncateTranslationFailed(
                NullLogger<MsSqlTruncateTranslator>.Instance, ex, container?.Name ?? "<null>", ex.Message);
            return Task.FromResult(
                GenericResult<SqlCommand>.Failure(
                    MsSqlDataResultCodes.ByName("DeleteTranslationFailed"),
                    ResultDetails.Create("ErrorMessage", ex.Message)));
        }
    }
}
