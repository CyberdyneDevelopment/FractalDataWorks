using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// Container type for SQL Server stored procedures.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ContainerTypes), "StoredProcedure", RestrictToCurrentCompilation = true)]
public sealed class StoredProcedureContainerType : ContainerTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StoredProcedureContainerType"/> class.
    /// </summary>
    public StoredProcedureContainerType()
        : base(
            id: 3,
            name: "StoredProcedure",
            displayName: "SQL Stored Procedure",
            description: "SQL Server stored procedure container with parameter discovery",
            supportsSchemaDiscovery: true)
    {
    }
}
