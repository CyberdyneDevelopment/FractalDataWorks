using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Conventions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Schema;

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
            FieldType = new SimpleFieldType
            {
                TypeName = clrType.Name,
                ClrType = clrType
            },
            // Why: IsPrimaryKey removed from Field — PK identity carried in KeyField tables.
            // Role = Surrogate signals this is the PK field for downstream consumers.
            Role = discovered.IsPrimaryKey
                ? PropertyRoles.ByName("Surrogate")
                : PropertyRoles.ByName("Attribute"),
            IsNullable = discovered.IsNullable,
            IsIdentity = discovered.IsIdentity,
            IsComputed = discovered.IsComputed,
            TypeSystemId = "PostgreSql",
            ConverterTypeId = 0
        };
    }

    /// <summary>Resolves an <see cref="IContainerType"/> by name from <see cref="ContainerTypes"/>.</summary>
    public static IContainerType GetContainerType(string containerTypeName)
        => ContainerTypes.ByName(containerTypeName);

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
