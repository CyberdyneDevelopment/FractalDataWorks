using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.PostgreSql;

/// <summary>
/// Container type for PostgreSQL views.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ContainerTypes), "PostgreSqlView")]
public sealed class PostgreSqlViewContainerType : ContainerTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlViewContainerType"/> class.
    /// </summary>
    public PostgreSqlViewContainerType()
        : base(
            name: "PostgreSqlView",
            displayName: "PostgreSQL View",
            description: "PostgreSQL view container with schema discovery support",
            supportsSchemaDiscovery: true,
            supportedOperations: ["Query"])
    {
    }
}
