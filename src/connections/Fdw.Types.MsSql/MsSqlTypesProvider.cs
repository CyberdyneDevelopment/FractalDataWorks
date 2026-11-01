using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Fdw.Results;
using Fdw.Types;
using Fdw.Types.MsSql.Logging;

namespace Fdw.Types.MsSql;

/// <summary>
/// SQL Server implementation of <see cref="ITypesProvider"/>.
/// </summary>
public sealed class MsSqlTypesProvider : ITypesProvider
{
    private readonly string _connectionString;
    private readonly ILogger<MsSqlTypesProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlTypesProvider"/> class.
    /// </summary>
    /// <param name="connectionString">The SQL Server connection string.</param>
    /// <param name="logger">The logger instance.</param>
    public MsSqlTypesProvider(string connectionString, ILogger<MsSqlTypesProvider> logger)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<IReadOnlyList<TypeCollectionMetadata>>> GetCollections(CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, Name, FullName, CollectionKind, ServiceCategory, AssemblyName
                FROM types.TypeCollection
                WHERE IsCurrent = 1
                ORDER BY Name";

            var collections = new List<TypeCollectionMetadata>();

            using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var collection = new TypeCollectionMetadata
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    FullName = reader.GetString(2),
                    CollectionKind = CollectionKinds.ByName(reader.GetString(3)) ?? CollectionKinds.Immutable,
                    ServiceCategory = await reader.IsDBNullAsync(4, cancellationToken).ConfigureAwait(false) ? null : reader.GetString(4),
                    AssemblyQualifiedName = await reader.IsDBNullAsync(5, cancellationToken).ConfigureAwait(false) ? null : reader.GetString(5)
                };

                collections.Add(collection);
            }

            return GenericResult<IReadOnlyList<TypeCollectionMetadata>>.Success(collections);
        }
        catch (SqlException ex)
        {
            return GenericResult<IReadOnlyList<TypeCollectionMetadata>>.Failure(
                MsSqlTypesProviderLog.GetCollectionsFailed(_logger, ex, ex.Message));
        }
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<TypeCollectionMetadata>> GetCollection(string name, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, Name, FullName, CollectionKind, ServiceCategory, AssemblyName
                FROM types.TypeCollection
                WHERE Name = @Name AND IsCurrent = 1";

            command.Parameters.AddWithValue("@Name", name);

            using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var collection = new TypeCollectionMetadata
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    FullName = reader.GetString(2),
                    CollectionKind = CollectionKinds.ByName(reader.GetString(3)) ?? CollectionKinds.Immutable,
                    ServiceCategory = await reader.IsDBNullAsync(4, cancellationToken).ConfigureAwait(false) ? null : reader.GetString(4),
                    AssemblyQualifiedName = await reader.IsDBNullAsync(5, cancellationToken).ConfigureAwait(false) ? null : reader.GetString(5)
                };

                return GenericResult<TypeCollectionMetadata>.Success(collection);
            }

            return GenericResult<TypeCollectionMetadata>.Failure(
                MsSqlTypesResultCodes.ByName("CollectionNotFound"));
        }
        catch (SqlException ex)
        {
            return GenericResult<TypeCollectionMetadata>.Failure(
                MsSqlTypesProviderLog.GetCollectionFailed(_logger, ex, ex.Message));
        }
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<IReadOnlyList<TypeOptionMetadata>>> GetOptions(int collectionId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, TypeCollectionId, Name, FullTypeName, Category, Description
                FROM types.TypeOption
                WHERE TypeCollectionId = @CollectionId AND IsCurrent = 1
                ORDER BY Name";

            command.Parameters.AddWithValue("@CollectionId", collectionId);

            var options = new List<TypeOptionMetadata>();

            using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var option = new TypeOptionMetadata
                {
                    Id = reader.GetInt32(0),
                    TypeCollectionId = reader.GetInt32(1),
                    Name = reader.GetString(2),
                    FullTypeName = reader.GetString(3),
                    Category = await reader.IsDBNullAsync(4, cancellationToken).ConfigureAwait(false) ? null : reader.GetString(4),
                    Description = await reader.IsDBNullAsync(5, cancellationToken).ConfigureAwait(false) ? null : reader.GetString(5)
                };

                options.Add(option);
            }

            return GenericResult<IReadOnlyList<TypeOptionMetadata>>.Success(options);
        }
        catch (SqlException ex)
        {
            return GenericResult<IReadOnlyList<TypeOptionMetadata>>.Failure(
                MsSqlTypesProviderLog.GetOptionsFailed(_logger, ex, collectionId, ex.Message));
        }
    }

    /// <inheritdoc/>
    public async Task<IGenericResult> SaveCollection(TypeCollectionMetadata collection, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            using var command = connection.CreateCommand();
            command.CommandText = @"
                MERGE types.TypeCollection AS target
                USING (SELECT @Id AS Id) AS source
                ON target.Id = source.Id
                WHEN MATCHED THEN
                    UPDATE SET
                        Name = @Name,
                        FullName = @FullName,
                        CollectionKind = @CollectionKind,
                        ServiceCategory = @ServiceCategory,
                        AssemblyName = @AssemblyName,
                        ModifyDate = SYSDATETIMEOFFSET()
                WHEN NOT MATCHED THEN
                    INSERT (Id, Name, FullName, CollectionKind, ServiceCategory, AssemblyName, IsCurrent, CreateDate)
                    VALUES (@Id, @Name, @FullName, @CollectionKind, @ServiceCategory, @AssemblyName, 1, SYSDATETIMEOFFSET());";

            command.Parameters.AddWithValue("@Id", collection.Id);
            command.Parameters.AddWithValue("@Name", collection.Name);
            command.Parameters.AddWithValue("@FullName", collection.FullName);
            command.Parameters.AddWithValue("@CollectionKind", collection.CollectionKind.Name);
            command.Parameters.AddWithValue("@ServiceCategory", (object?)collection.ServiceCategory ?? DBNull.Value);
            command.Parameters.AddWithValue("@AssemblyName", (object?)collection.AssemblyQualifiedName ?? DBNull.Value);

            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

            return GenericResult.Success();
        }
        catch (SqlException ex)
        {
            return GenericResult.Failure(
                MsSqlTypesProviderLog.SaveCollectionFailed(_logger, ex, collection.Name, ex.Message));
        }
    }

    /// <inheritdoc/>
    public async Task<IGenericResult> SaveOption(TypeOptionMetadata option, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            using var command = connection.CreateCommand();
            command.CommandText = @"
                MERGE types.TypeOption AS target
                USING (SELECT @Id AS Id) AS source
                ON target.Id = source.Id
                WHEN MATCHED THEN
                    UPDATE SET
                        TypeCollectionId = @TypeCollectionId,
                        Name = @Name,
                        FullTypeName = @FullTypeName,
                        Category = @Category,
                        Description = @Description
                WHEN NOT MATCHED THEN
                    INSERT (Id, TypeCollectionId, Name, FullTypeName, Category, Description, IsCurrent, CreateDate)
                    VALUES (@Id, @TypeCollectionId, @Name, @FullTypeName, @Category, @Description, 1, SYSDATETIMEOFFSET());";

            command.Parameters.AddWithValue("@Id", option.Id);
            command.Parameters.AddWithValue("@TypeCollectionId", option.TypeCollectionId);
            command.Parameters.AddWithValue("@Name", option.Name);
            command.Parameters.AddWithValue("@FullTypeName", option.FullTypeName);
            command.Parameters.AddWithValue("@Category", (object?)option.Category ?? DBNull.Value);
            command.Parameters.AddWithValue("@Description", (object?)option.Description ?? DBNull.Value);

            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

            return GenericResult.Success();
        }
        catch (SqlException ex)
        {
            return GenericResult.Failure(
                MsSqlTypesProviderLog.SaveOptionFailed(_logger, ex, option.Name, ex.Message));
        }
    }
}
