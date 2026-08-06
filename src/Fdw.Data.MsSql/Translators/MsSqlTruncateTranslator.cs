using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Data.MsSql.Results;
using Fdw.Data.MsSql.Translators;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Microsoft.Data.SqlClient;

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
        try
        {
            if (container == null)
            {
                return Task.FromResult(
                    GenericResult<SqlCommand>.Failure(MsSqlDataResultCodes.ByName("ContainerNull")));
            }

            if (container.Path is not IDatabasePath dbPath)
            {
                return Task.FromResult(
                    GenericResult<SqlCommand>.Failure(MsSqlDataResultCodes.ByName("InvalidContainerPath")));
            }

            // Why: empty the whole container — an unconditional DELETE (no WHERE). DELETE rather than
            // TRUNCATE TABLE so the operation needs only a DELETE grant and tolerates FK references.
            return Task.FromResult(
                GenericResult<SqlCommand>.Success(CreateCommand($"DELETE FROM {BuildQualifiedTableName(dbPath)}")));
        }
        catch (Exception ex)
        {
            return Task.FromResult(
                GenericResult<SqlCommand>.Failure(
                    MsSqlDataResultCodes.ByName("DeleteTranslationFailed"),
                    ResultDetails.Create("ErrorMessage", ex.Message)));
        }
    }
}
