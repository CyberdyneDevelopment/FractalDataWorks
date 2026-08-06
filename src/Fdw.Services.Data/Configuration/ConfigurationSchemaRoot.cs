namespace Fdw.Services.Data.Configuration;

/// <summary>
/// Top-level wrapper that matches the root JSON object in <c>configurationSchema.json</c>.
/// The file has a single <c>"ConfigurationSchema"</c> key that wraps all runtime configuration.
/// </summary>
/// <remarks>
/// Why: STJ deserializes the entire file into this root object first, then the caller extracts
/// <see cref="ConfigurationSchema"/>. The extra indirection lets future tooling add top-level
/// metadata (e.g., schema version) without changing the inner schema shape.
/// </remarks>
// Why: pure data holder, no logic beyond trivial construction/assignment
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class ConfigurationSchemaRoot
{
    /// <summary>
    /// Gets or sets the configuration schema body.
    /// </summary>
    public ConfigurationSchema? ConfigurationSchema { get; set; }
}
