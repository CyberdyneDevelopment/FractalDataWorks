using System.Text.Json.Serialization;

namespace Fdw.UI.WebMcp;

/// <summary>
/// A single tool the browser refused to register, and the reason it gave.
/// </summary>
public sealed class WebMcpToolFailure
{
    /// <summary>
    /// Gets the name of the tool that failed to register.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the error message reported by the browser.
    /// </summary>
    [JsonPropertyName("error")]
    public string Error { get; init; } = string.Empty;
}
