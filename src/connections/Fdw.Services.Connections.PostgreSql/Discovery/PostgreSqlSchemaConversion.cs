using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Conventions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.PostgreSql;
using Fdw.Results;
using Fdw.Schema;
using Fdw.Services.Connections.PostgreSql.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.PostgreSql.Discovery;

/// <summary>
/// Shared static helpers for converting PostgreSQL schema discovery results to FDW container/field types.
/// </summary>
[ExcludeFromCodeCoverage]
public static class PostgreSqlSchemaConversion
{
    /// <summary>Maps a <see cref="DiscoveredField"/> to an <see cref="IField"/>.</summary>
    public static IField MapToField(DiscoveredField discovered)
    {
        var clrType = MapPostgreSqlTypeToClr(discovered.SqlType);

        if (discovered.IsNullable && clrType.IsValueType)
            clrType = typeof(Nullable<>).MakeGenericType(clrType);

        return new Field
        {
            Name = discovered.Name,
            // The column's type as PostgreSQL reports it, recorded as the container field's DataType; the portable type is the
            // native type's own (FDW-1588).
            FieldType = new SimpleFieldType
            {
                TypeName = discovered.SqlType,
                ClrType = clrType
            },
            ExplicitType = PortableTypeOf(discovered.SqlType),
            // Why a primary key is Identifier and never DbKey: the catalog cannot tell a meaningless row
            // number from a business id the database happens to assign (an OrderId IDENTITY), and DbKey
            // withholds the column from every default read. Hiding a key is a person's decision, not
            // discovery's. Every other column stays NotYetTyped: the catalog cannot tell a measure from an
            // attribute (FDW-988).
            Role = discovered.IsPrimaryKey ? DatumTypes.Identifier : DatumTypes.NotYetTyped,
            IsNullable = discovered.IsNullable,
            IsIdentity = discovered.IsIdentity,
            IsComputed = discovered.IsComputed,
            TypeSystemId = "PostgreSql",
            ConverterTypeId = 0
        };
    }

    // Why the native type's abstract type: it is the mapping PostgreSQL columns of that type are read with, so the portable type
    // is the store's own statement, not a guess. A type FDW has no native type for stays unstated (FDW-1588).
    private static IDataType? PortableTypeOf(string sqlType) =>
        PostgreSqlNativeTypeResolver.Resolve(sqlType) is { } native && !ReferenceEquals(native, PostgreSqlNativeTypes.NotFound)
            ? native.AbstractType
            : null;

    /// <summary>
    /// Resolves the container type option a PostgreSQL discovery gives what it found (FDW-6309, FDW-6387).
    /// </summary>
    /// <param name="logger">The logger the coded messages are written through.</param>
    /// <param name="discoveredWord">The word the discoverer wrote for what it found: <c>Table</c> or <c>View</c>. A discovery fact, not an option name.</param>
    /// <returns>
    /// The connection type's own option, <c>PostgreSqlTable</c> or <c>PostgreSqlView</c>; a failure when the word maps to none, and a
    /// failure when ContainerTypes does not hold the option (the lookup answers the NotFound placeholder, which is never used as a type).
    /// </returns>
    public static IGenericResult<IContainerType> GetContainerType(ILogger logger, string discoveredWord)
    {
        // This connection type names its options after the discoverer's word: Table is PostgreSqlTable, View is PostgreSqlView.
        var option = ContainerTypes.ByName("PostgreSql" + discoveredWord);
        return ReferenceEquals(option, ContainerTypes.NotFound)
            ? GenericResult<IContainerType>.Failure(PostgreSqlSchemaDiscoveryLog.ContainerTypeWordNotMapped(logger, discoveredWord))
            : GenericResult<IContainerType>.Success(option);
    }

    /// <summary>
    /// Maps a PostgreSQL data type name to a CLR type.
    /// </summary>
    [ConventionOverride(MaxCyclomaticComplexity = 30)]
    internal static Type MapPostgreSqlTypeToClr(string pgType)
    {
        return pgType.ToLowerInvariant() switch
        {
            "integer" or "int4" or "serial" => typeof(int),
            "bigint" or "int8" or "bigserial" => typeof(long),
            "smallint" or "int2" or "smallserial" => typeof(short),
            "boolean" or "bool" => typeof(bool),
            "numeric" or "decimal" or "money" => typeof(decimal),
            "double precision" or "float8" => typeof(double),
            "real" or "float4" => typeof(float),
            "date" => typeof(DateTime),
            "timestamp without time zone" or "timestamp" => typeof(DateTime),
            "timestamp with time zone" or "timestamptz" => typeof(DateTimeOffset),
            "time without time zone" or "time" => typeof(TimeSpan),
            "time with time zone" or "timetz" => typeof(DateTimeOffset),
            "character varying" or "varchar" or "text" or "char" or "character" or "name" => typeof(string),
            "json" or "jsonb" or "xml" => typeof(string),
            "bytea" => typeof(byte[]),
            "uuid" => typeof(Guid),
            "oid" => typeof(uint),
            "interval" => typeof(TimeSpan),
            "inet" or "cidr" or "macaddr" or "macaddr8" => typeof(string),
            "bit" or "bit varying" or "varbit" => typeof(string),
            "point" or "line" or "lseg" or "box" or "path" or "polygon" or "circle" => typeof(string),
            "tsvector" or "tsquery" => typeof(string),
            "pg_lsn" => typeof(string),
            "txid_snapshot" => typeof(string),
            _ => typeof(string)
        };
    }
}
