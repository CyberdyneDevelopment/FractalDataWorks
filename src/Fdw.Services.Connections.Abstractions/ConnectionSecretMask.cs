namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// The value a connection's secret-bearing settings are shown as in any response.
/// </summary>
/// <remarks>
/// A client that sends this value back in an update means "keep what is stored", so an edit form can
/// round-trip a masked value without knowing it.
/// </remarks>
public static class ConnectionSecretMask
{
    /// <summary>The masked placeholder: eight bullets.</summary>
    public const string Value = "••••••••";
}
