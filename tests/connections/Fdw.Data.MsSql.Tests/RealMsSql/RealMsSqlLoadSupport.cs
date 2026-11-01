using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.MsSql.Results;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Etl.Pipelines.Loads;
using Microsoft.Data.SqlClient;
using static Fdw.Data.MsSql.Tests.RealMsSql.RealMsSqlSupport;

namespace Fdw.Data.MsSql.Tests.RealMsSql;

/// <summary>
/// What the pipeline-load acceptance tests share (FDW-1109): containers with typed fields over tables and procedures the
/// test created, a transactional connection that runs the real translators on a real SQL transaction (bulk inserts included),
/// resolvers that answer data set and data store lookups, and a gateway that serves the extract and writes the load and the
/// watermark to the real database.
/// </summary>
internal static class RealMsSqlLoadSupport
{
    /// <summary>The most rows a bulk insert runs serially in these tests, as a connection's MaxSerialInsert states (FDW-1152).</summary>
    internal const int MaxSerialInsert = 1000;

    /// <summary>A column of a test container.</summary>
    /// <param name="Name">The column (or procedure parameter) name.</param>
    /// <param name="ClrType">The CLR type of its values.</param>
    /// <param name="IsIdentity">Whether the database numbers it.</param>
    internal sealed record Col(string Name, Type ClrType, bool IsIdentity = false);

    internal static Col Int(string name, bool identity = false) => new(name, typeof(int), identity);

    internal static Col Text(string name) => new(name, typeof(string));

    internal static Col Moment(string name) => new(name, typeof(DateTimeOffset));

    /// <summary>A container over a table or a procedure of the run's schema, whose data store sits on <paramref name="connectionId"/>.</summary>
    internal static IDataContainer Container(
        RealMsSqlFixture fixture, string name, Guid connectionId, string containerType, params Col[] columns)
    {
        var fields = columns.Select(column =>
        {
            var type = new Mock<IFieldType>();
            type.SetupGet(t => t.ClrType).Returns(column.ClrType);
            type.SetupGet(t => t.TypeName).Returns(column.ClrType.Name);
            var field = new Mock<IField>();
            field.SetupGet(f => f.Name).Returns(column.Name);
            field.SetupGet(f => f.FieldType).Returns(type.Object);
            field.SetupGet(f => f.IsNullable).Returns(true);
            field.SetupGet(f => f.IsIdentity).Returns(column.IsIdentity);
            field.SetupGet(f => f.IsComputed).Returns(false);
            return field.Object;
        }).ToArray();

        var schema = new Mock<IContainerSchema>();
        schema.SetupGet(s => s.Fields).Returns(fields);
        var kind = new Mock<IContainerType>();
        kind.SetupGet(t => t.Name).Returns(containerType);
        var store = new Mock<IDataStore>();
        store.SetupGet(s => s.ConnectionId).Returns(connectionId);
        var parent = new Mock<IDataNodePath>();
        parent.SetupGet(p => p.Store).Returns(store.Object);

        var container = new Mock<IDataContainer>();
        container.SetupGet(c => c.Name).Returns(name);
        container.SetupGet(c => c.Path).Returns(new DatabasePath(string.Empty, fixture.Schema, name));
        container.SetupGet(c => c.Schema).Returns(schema.Object);
        container.SetupGet(c => c.ContainerType).Returns(kind.Object);
        container.SetupGet(c => c.Parent).Returns(parent.Object);
        return container.Object;
    }

    /// <summary>
    /// Runs a command the way the MsSql connection and its unit of work do (FDW-1152): a command that asks for the bulk route is
    /// copied with SqlBulkCopy, on the transaction when there is one; anything else is translated and run as SQL.
    /// </summary>
    internal static async Task<IGenericResult<int>> RunOn(
        IDataCommand command, IStorageContainer container, SqlConnection connection, SqlTransaction? transaction, CancellationToken cancellationToken)
    {
        try
        {
            if (command is IDatabaseConnectionCommand routed && routed.ReroutesToBulk(MaxSerialInsert))
            {
                var plan = MsSqlInsertTranslator.Plan(command, container);
                return plan.IsSuccess
                    ? GenericResult<int>.Success(await MsSqlBulkCopy.Write(plan.Value!, connection, transaction, cancellationToken))
                    : plan.ToNewResult<int>();
            }

            var translated = await Translate(command, container, cancellationToken);
            if (!translated.IsSuccess)
                return translated.ToNewResult<int>();

            var sql = translated.Value!;
            sql.Connection = connection;
            sql.Transaction = transaction;
            return GenericResult<int>.Success(await sql.ExecuteNonQueryAsync(cancellationToken));
        }
        catch (SqlException ex)
        {
            return GenericResult<int>.Failure(MsSqlDataResultCodes.ByName("UpsertTranslationFailed"), ResultDetails.Create("ErrorMessage", ex.Message));
        }
    }

    /// <summary>A transactional connection over one real SQL connection, running the real translators.</summary>
    internal static ITransactionalDataConnection TransactionalConnection(SqlConnection connection)
    {
        var mock = new Mock<ITransactionalDataConnection>();
        mock.Setup(c => c.BeginTransaction(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => GenericResult<IDataConnectionTransaction>.Success(new LoadTransaction(connection, connection.BeginTransaction())));
        return mock.Object;
    }

    /// <summary>A data set provider that resolves each data set to one source over its container of the data store "Main", path "dbo".</summary>
    internal static IDataSetConfigurationProvider DataSetProviderOf(params (string Name, string Container)[] sets)
    {
        var provider = new Mock<IDataSetConfigurationProvider>();
        provider
            .Setup(p => p.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataSetImplementationConfiguration>.Failure(new GenericMessage("data set not found")));
        foreach (var (name, containerName) in sets)
        {
            var configuration = new Mock<IDataSetImplementationConfiguration>();
            configuration.SetupGet(c => c.Sources).Returns(new List<DataSetSourceConfiguration> { new() { DataStoreName = "Main", PathValue = "dbo", ContainerName = containerName } });
            provider
                .Setup(p => p.Get(name, It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<IDataSetImplementationConfiguration>.Success(configuration.Object));
        }

        return provider.Object;
    }

    /// <summary>A data store provider that serves the given containers of the data store "Main", path "dbo", by name.</summary>
    internal static IDataStoreProvider DataStoreProviderOf(params IDataContainer[] containers)
    {
        var provider = new Mock<IDataStoreProvider>();
        provider
            .Setup(p => p.Get(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataContainer>.Failure(new GenericMessage("container not found")));
        foreach (var container in containers)
        {
            provider
                .Setup(p => p.Get("Main", "dbo", container.Name, It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<IDataContainer>.Success(container));
        }

        return provider.Object;
    }

    /// <summary>A connection provider that hands out one connection.</summary>
    internal static IConnectionProvider ConnectionProviderOf(Guid id, IGenericConnection connection)
    {
        var provider = new Mock<IConnectionProvider>();
        provider
            .Setup(p => p.Get(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IGenericConnection>.Success(connection));
        return provider.Object;
    }

    /// <summary>A connection type that lists the named command capabilities.</summary>
    internal static IConnectionType ConnectionType(IEnumerable<string> capabilities)
    {
        var type = new Mock<IConnectionType>();
        type.SetupGet(t => t.SupportedCommands).Returns(capabilities.Select(name => (ICommandCapabilityType)CommandCapabilityTypes.ByName(name)).ToList());
        return type.Object;
    }

    internal static readonly string[] AllCapabilities =
        ["SoftDelete", "UnitOfWork"];

    /// <summary>Answers every connection id with one connection type.</summary>
    internal sealed class SingleTypeResolver(IConnectionType type) : IConnectionTypeResolver
    {
        public Task<IGenericResult<IConnectionType>> ForConnection(Guid connectionId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IGenericResult<IConnectionType>>(GenericResult<IConnectionType>.Success(type));
    }

    private sealed class LoadTransaction : IDataConnectionTransaction
    {
        private readonly SqlConnection _connection;
        private readonly SqlTransaction _transaction;

        internal LoadTransaction(SqlConnection connection, SqlTransaction transaction)
        {
            _connection = connection;
            _transaction = transaction;
            IsActive = true;
        }

        public bool IsActive { get; private set; }

        public async Task<IGenericResult<T>> Execute<T>(IDataCommand command, IDataContainer container, CancellationToken cancellationToken = default)
        {
            var ran = await RunOn(command, container, _connection, _transaction, cancellationToken);
            return ran.IsSuccess ? (IGenericResult<T>)(object)ran : ran.ToNewResult<T>();
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

    /// <summary>
    /// The gateway the pipeline reads and writes through: the extract is rows read from the real source table, the pipeline's
    /// own load is translated and run on the real database, and the watermark is kept in a real table. A SQL error is a
    /// failed result, as the real connection reports it.
    /// </summary>
    internal sealed class RealGateway
    {
        private readonly RealMsSqlFixture _fixture;
        private readonly IReadOnlyList<Dictionary<string, object?>> _extract;
        private readonly Dictionary<string, IDataContainer> _byDataSet;
        private readonly string _watermarkTable;

        internal RealGateway(
            RealMsSqlFixture fixture,
            IReadOnlyList<Dictionary<string, object?>> extract,
            Dictionary<string, IDataContainer> byDataSet,
            string watermarkTable)
        {
            _fixture = fixture;
            _extract = extract;
            _byDataSet = byDataSet;
            _watermarkTable = watermarkTable;

            var gateway = new Mock<IDataGateway>();
            gateway
                .Setup(g => g.Execute<IEnumerable<Dictionary<string, object?>>>(It.IsAny<IDataCommand>(), It.IsAny<DataSetTarget>(), It.IsAny<CancellationToken>()))
                .Returns((IDataCommand command, DataSetTarget target, CancellationToken token) =>
                {
                    var paging = ((IQueryCommand)command).Paging;
                    var skip = paging is null ? 0 : paging.Skip;
                    var take = paging is { Take: { } taken } ? taken : int.MaxValue;
                    var page = _extract.Skip(skip).Take(take).ToList();
                    return Task.FromResult<IGenericResult<IEnumerable<Dictionary<string, object?>>>>(
                        GenericResult<IEnumerable<Dictionary<string, object?>>>.Success(page));
                });
            gateway
                .Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataSetTarget>(), It.IsAny<CancellationToken>()))
                .Returns((IDataCommand command, DataSetTarget target, CancellationToken token) => RunOwnLoad(command, target.DataSet, token));
            gateway
                .Setup(g => g.Execute<IEnumerable<EtlPipelineWatermarkRecord>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
                .Returns((IDataCommand command, DataStoreTarget target, CancellationToken token) => ReadWatermark());
            gateway
                .Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
                .Returns((IDataCommand command, DataStoreTarget target, CancellationToken token) => WriteWatermark(command));

            var provider = new Mock<IDataGatewayProvider>();
            provider.Setup(p => p.ByName("Main")).Returns(gateway.Object);
            Provider = provider.Object;
        }

        internal IDataGatewayProvider Provider { get; }

        private async Task<IGenericResult<int>> RunOwnLoad(IDataCommand command, string dataSet, CancellationToken token)
        {
            await using var connection = await _fixture.Open();
            return await RunOn(command, _byDataSet[dataSet], connection, null, token);
        }

        private async Task<IGenericResult<IEnumerable<EtlPipelineWatermarkRecord>>> ReadWatermark()
        {
            var rows = await _fixture.Rows($"SELECT Id, PipelineId, TenantId, LastSuccessfulRun, ExecutionId FROM [{_fixture.Schema}].[{_watermarkTable}]");
            IEnumerable<EtlPipelineWatermarkRecord> records = rows.Select(row => new EtlPipelineWatermarkRecord
            {
                Id = (Guid)row["Id"]!,
                PipelineId = (Guid)row["PipelineId"]!,
                TenantId = (Guid)row["TenantId"]!,
                LastSuccessfulRun = (DateTimeOffset)row["LastSuccessfulRun"]!,
                ExecutionId = (Guid)row["ExecutionId"]!,
            }).ToList();
            return GenericResult<IEnumerable<EtlPipelineWatermarkRecord>>.Success(records);
        }

        private async Task<IGenericResult<int>> WriteWatermark(IDataCommand command)
        {
            var (record, insert) = command switch
            {
                InsertCommand<EtlPipelineWatermarkRecord> add => (add.Data.Single(), true),
                UpdateCommand<EtlPipelineWatermarkRecord> change => (change.Data, false),
                _ => throw new InvalidOperationException($"unexpected watermark command {command.GetType().Name}"),
            };
            await using var connection = await _fixture.Open();
            await using var sql = connection.CreateCommand();
            sql.CommandText = insert
                ? $"INSERT INTO [{_fixture.Schema}].[{_watermarkTable}] (Id, PipelineId, TenantId, LastSuccessfulRun, ExecutionId) VALUES (@Id, @PipelineId, @TenantId, @Run, @ExecutionId)"
                : $"UPDATE [{_fixture.Schema}].[{_watermarkTable}] SET LastSuccessfulRun = @Run, ExecutionId = @ExecutionId WHERE Id = @Id";
            sql.Parameters.AddWithValue("@Id", record.Id);
            sql.Parameters.AddWithValue("@PipelineId", record.PipelineId);
            sql.Parameters.AddWithValue("@TenantId", record.TenantId);
            sql.Parameters.AddWithValue("@Run", record.LastSuccessfulRun);
            sql.Parameters.AddWithValue("@ExecutionId", record.ExecutionId);
            return GenericResult<int>.Success(await sql.ExecuteNonQueryAsync());
        }
    }
}
