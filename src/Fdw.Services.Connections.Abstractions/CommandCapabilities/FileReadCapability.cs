using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Abstractions.CommandCapabilities;

/// <summary>
/// File read capability — reads records from a file accessible through the connection.
/// Used by file-system connection types.
/// </summary>
/// <remarks>
/// Configuration keys:
/// <list type="bullet">
///   <item><c>Path</c> — file path relative to the connection's root (required).</item>
///   <item><c>Format</c> — file format: csv, json, or parquet (required).</item>
///   <item><c>Encoding</c> — text encoding; defaults to utf-8 at runtime if not set.</item>
///   <item><c>HasHeader</c> — whether the first row is a header row (CSV only).</item>
/// </list>
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(CommandCapabilityTypes), "FileRead", RestrictToCurrentCompilation = true)]
public sealed class FileReadCapability : CommandCapabilityTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileReadCapability"/> class.
    /// </summary>
    public FileReadCapability()
        : base(
            id: 6,
            name: "FileRead",
            displayName: "File Read",
            configurationFields:
            [
                new ConfigurationFieldDescriptor(
                    Key: "Path",
                    Label: "File Path",
                    Placeholder: "data/input/customers.csv",
                    InputKind: ConfigurationFieldKinds.Text,
                    IsRequired: true),
                new ConfigurationFieldDescriptor(
                    Key: "Format",
                    Label: "Format",
                    Placeholder: string.Empty,
                    InputKind: ConfigurationFieldKinds.Select,
                    SelectOptions: ["csv:CSV", "json:JSON", "parquet:Parquet"],
                    IsRequired: true),
                new ConfigurationFieldDescriptor(
                    Key: "Encoding",
                    Label: "Encoding",
                    Placeholder: string.Empty,
                    InputKind: ConfigurationFieldKinds.Select,
                    SelectOptions: ["utf-8:UTF-8", "utf-16:UTF-16", "ascii:ASCII", "latin-1:Latin-1"]),
                new ConfigurationFieldDescriptor(
                    Key: "HasHeader",
                    Label: "First Row is Header",
                    Placeholder: string.Empty,
                    InputKind: ConfigurationFieldKinds.Boolean),
            ])
    {
    }
}
