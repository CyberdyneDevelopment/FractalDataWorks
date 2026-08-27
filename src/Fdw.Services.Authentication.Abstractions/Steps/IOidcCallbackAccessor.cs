namespace Fdw.Services.Authentication.Abstractions.Steps;

/// <summary>
/// Supplies what the provider sent back to the callback.
/// </summary>
public interface IOidcCallbackAccessor
{
    /// <summary>Gets the authorization code, or null on the first pass through the step.</summary>
    string? Code { get; }

    /// <summary>Gets the state the provider echoed back.</summary>
    string? State { get; }
}
