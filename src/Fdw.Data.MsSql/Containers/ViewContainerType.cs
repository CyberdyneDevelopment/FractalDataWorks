using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// Container type for SQL Server views.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ContainerTypes), "View", RestrictToCurrentCompilation = true)]
public sealed class ViewContainerType : ContainerTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ViewContainerType"/> class.
    /// </summary>
    public ViewContainerType()
        : base(
            id: 2,
            name: "View",
            displayName: "SQL View",
            description: "SQL Server view container with schema discovery support",
            supportsSchemaDiscovery: true)
    {
    }
}
