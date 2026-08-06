using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.PostgreSql;

/// <summary>
/// Container type for PostgreSQL tables.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ContainerTypes), "PostgreSqlTable", RestrictToCurrentCompilation = true)]
public sealed class PostgreSqlTableContainerType : ContainerTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlTableContainerType"/> class.
    /// </summary>
    public PostgreSqlTableContainerType()
        : base(
            id: 10,
            name: "PostgreSqlTable",
            displayName: "PostgreSQL Table",
            description: "PostgreSQL table container with schema discovery support",
            supportsSchemaDiscovery: true)
    {
    }
}
