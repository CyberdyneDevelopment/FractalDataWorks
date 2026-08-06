using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// Container type for SQL Server tables.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ContainerTypes), "Table", RestrictToCurrentCompilation = true)]
public sealed class TableContainerType : ContainerTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TableContainerType"/> class.
    /// </summary>
    public TableContainerType()
        : base(
            id: 1,
            name: "Table",
            displayName: "SQL Table",
            description: "SQL Server table container with full schema discovery support",
            supportsSchemaDiscovery: true)
    {
    }
}
