using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.Files.Containers;

/// <summary>
/// Container type for file-based data sources.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ContainerTypes), "File", RestrictToCurrentCompilation = true)]
public sealed class FileContainerType : ContainerTypeBase
{
    /// <summary>
    /// Singleton instance of FileContainerType.
    /// </summary>
    public static readonly FileContainerType Instance = new();

    private FileContainerType()
        : base(
            id: 20,
            name: "File",
            displayName: "File",
            description: "File-based data container supporting CSV, JSON, XML, Parquet, and other formats",
            supportsSchemaDiscovery: true)
    {
    }
}
