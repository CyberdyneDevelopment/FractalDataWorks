using System;
using System.Collections.Generic;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Data.Results;
using Microsoft.Extensions.Configuration;

namespace Fdw.Services.Data.Discovery.Types;

/// <summary>
/// File-based container discovery -- imports container definitions from a file.
///
/// Identity properties (readonly, set by constructor):
///   Id=2, Name="FromFile", DisplayName="File Discovery"
///
/// Configuration properties (bindable from IConfiguration):
///   FilePath -- path to the file containing container definitions.
///   FileFormat -- the format of the file (e.g., "json", "csv").
///
/// Behavior:
///   SupportsAutoDiscovery=false -- requires a file to be provided.
/// </summary>
[TypeOption(typeof(DiscoveryMethods), "FromFile")]
public sealed class FileDiscovery : DiscoveryMethodBase
{
    private static readonly string[] Expected = ["FilePath", "FileFormat"];
    private static readonly string[] Required = ["FilePath"];

    /// <summary>Initializes a new instance of the <see cref="FileDiscovery"/> class.</summary>
    public FileDiscovery()
        : base(
            id: 2,
            name: "FromFile",
            displayName: "File Discovery",
            description: "Imports container definitions from a file",
            supportsAutoDiscovery: false,
            expectedProperties: Expected,
            requiredProperties: Required)
    {
    }

    /// <summary>Gets or sets the path to the file containing container definitions.</summary>
    public string? FilePath { get; set; }

    /// <summary>Gets or sets the format of the file (e.g., "json", "csv").</summary>
    public string? FileFormat { get; set; }

    /// <inheritdoc/>
    public override DiscoveryMethodBase CreateInstance() => new FileDiscovery();

    /// <inheritdoc/>
    public override void Bind(IConfigurationSection section)
    {
        FilePath = section[nameof(FilePath)];
        FileFormat = section[nameof(FileFormat)];
    }

    /// <inheritdoc/>
    public override void BindFromValues(IReadOnlyDictionary<string, string?> values)
    {
        values.TryGetValue(nameof(FilePath), out var fp);
        FilePath = fp;
        values.TryGetValue(nameof(FileFormat), out var ff);
        FileFormat = ff;
    }

    /// <inheritdoc/>
    public override IReadOnlyList<KeyValuePair<string, string?>> AsKvp()
    {
        return
        [
            new("Type", Name),
            new(nameof(FilePath), FilePath),
            new(nameof(FileFormat), FileFormat),
        ];
    }

    /// <inheritdoc/>
    public override IGenericResult Validate()
    {
        if (string.IsNullOrEmpty(FilePath))
        {
            return GenericResult.Failure(
                DataServiceResultCodes.ByName("DiscoveryValidationFailed"),
                ResultDetails.Create("ValidationErrors", "FilePath is required for FromFile discovery"));
        }

        return GenericResult.Success();
    }
}
