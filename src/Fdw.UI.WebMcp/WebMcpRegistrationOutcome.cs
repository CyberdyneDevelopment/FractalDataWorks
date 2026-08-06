using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Fdw.UI.WebMcp;

/// <summary>
/// The result of registering a bridge's tools with the browser's WebMCP model context.
/// </summary>
/// <remarks>
/// Deserialized from the JS module's <c>register()</c> return value so the bridge logs what
/// actually happened instead of inferring it. <see cref="Supported"/> separates the ordinary
/// "this browser has no agent surface" case from a genuine registration failure.
/// </remarks>
public sealed class WebMcpRegistrationOutcome
{
    /// <summary>
    /// Gets a value indicating whether the browser exposes a WebMCP model context at all.
    /// </summary>
    [JsonPropertyName("supported")]
    public bool Supported { get; init; }

    /// <summary>
    /// Gets the number of tools successfully registered.
    /// </summary>
    [JsonPropertyName("registered")]
    public int Registered { get; init; }

    /// <summary>
    /// Gets the tools the browser rejected, each with the reason it gave.
    /// </summary>
    [JsonPropertyName("failed")]
    public IReadOnlyList<WebMcpToolFailure> Failed { get; init; } = Array.Empty<WebMcpToolFailure>();
}
