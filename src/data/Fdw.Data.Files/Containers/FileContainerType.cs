using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.Files.Containers;

/// <summary>
/// Container type for file-based data sources.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ContainerTypes), "File")]
public sealed class FileContainerType : ContainerTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileContainerType"/> class.
    /// </summary>
    public FileContainerType()
        : base(
            name: "File",
            displayName: "File",
            description: "File-based data container supporting CSV, JSON, XML, Parquet, and other formats",
            supportsSchemaDiscovery: true,
            supportedOperations: ["Query"])
    {
    }
}
