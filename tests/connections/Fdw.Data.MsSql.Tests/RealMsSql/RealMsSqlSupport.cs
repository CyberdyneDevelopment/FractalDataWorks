using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Data.MsSql.Results;
using Fdw.Data.MsSql.Tests.TestSupport;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Microsoft.Data.SqlClient;

namespace Fdw.Data.MsSql.Tests.RealMsSql;

/// <summary>
/// Helpers the acceptance tests share: building a container double for a table the test created, and running a
/// command through its own translator against the real server.
/// </summary>
internal static class RealMsSqlSupport
{
    internal static Mock<IDataContainer> Container(RealMsSqlFixture fixture, string table, params TestContainers.Column[] columns) =>
        TestContainers.Create(fixture.Schema, table, "Table", columns);

    internal static Dictionary<string, object?> Row(params (string Column, object? Value)[] values)
    {
        var row = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var (column, value) in values)
            row[column] = value;
        return row;
    }

    /// <summary>Translates <paramref name="command"/> with the translator registered for its verb and runs it; returns the rows it reports.</summary>
    internal static async Task<int> Run(RealMsSqlFixture fixture, IDataCommand command, IDataContainer container)
    {
        var translated = await Translate(command, container, CancellationToken.None);
        translated.IsSuccess.ShouldBeTrue($"translation failed: {translated.Code?.Name}");

        await using var connection = await fixture.Open();
        var sql = translated.Value!;
        sql.Connection = connection;
        return await sql.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    internal static Task<IGenericResult<SqlCommand>> Translate(IDataCommand command, IStorageContainer container, CancellationToken cancellationToken) =>
        MsSqlDataCommandTranslators.ByName(command.CommandType).Translate(command, container, cancellationToken);

    /// <summary>A transactional connection over one real SQL connection: each transaction it begins runs translated commands on a SqlTransaction.</summary>
    internal static Mock<ITransactionalDataConnection> TransactionalConnection(SqlConnection connection)
    {
        var mock = new Mock<ITransactionalDataConnection>();
        mock.Setup(c => c.BeginTransaction(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => GenericResult<IDataConnectionTransaction>.Success(
                new SqlServerTestTransaction(connection, connection.BeginTransaction())));
        return mock;
    }

    private sealed class SqlServerTestTransaction : IDataConnectionTransaction
    {
        private readonly SqlConnection _connection;
        private readonly SqlTransaction _transaction;

        internal SqlServerTestTransaction(SqlConnection connection, SqlTransaction transaction)
        {
            _connection = connection;
            _transaction = transaction;
            IsActive = true;
        }

        public bool IsActive { get; private set; }

        public async Task<IGenericResult<T>> Execute<T>(IDataCommand command, IDataContainer container, CancellationToken cancellationToken = default)
        {
            var translated = await Translate(command, container, cancellationToken);
            if (!translated.IsSuccess)
                return translated.ToNewResult<T>();

            var sql = translated.Value!;
            sql.Connection = _connection;
            sql.Transaction = _transaction;
            try
            {
                var rows = await sql.ExecuteNonQueryAsync(cancellationToken);
                return (IGenericResult<T>)(object)GenericResult<int>.Success(rows);
            }
            catch (SqlException ex)
            {
                return GenericResult<T>.Failure(
                    MsSqlDataResultCodes.ByName("UpsertTranslationFailed"),
                    ResultDetails.Create("ErrorMessage", ex.Message));
            }
        }

        public async Task<IGenericResult> Execute(IDataCommand command, IDataContainer container, CancellationToken cancellationToken = default) =>
            await Execute<int>(command, container, cancellationToken);

        public Task<IGenericResult> Commit(CancellationToken cancellationToken = default)
        {
            _transaction.Commit();
            IsActive = false;
            return Task.FromResult(GenericResult.Success());
        }

        public Task<IGenericResult> Rollback(CancellationToken cancellationToken = default)
        {
            _transaction.Rollback();
            IsActive = false;
            return Task.FromResult(GenericResult.Success());
        }

        public ValueTask DisposeAsync()
        {
            if (IsActive)
                _transaction.Rollback();
            IsActive = false;
            _transaction.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
