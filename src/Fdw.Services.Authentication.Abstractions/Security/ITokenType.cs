using Fdw.Collections;

namespace Fdw.Services.Authentication.Abstractions;

/// <summary>
/// Interface for token types.
/// Defines the contract for token types supported by the framework.
/// </summary>
public interface ITokenType : ITypeOption<int>
{
    /// <summary>
    /// Gets the token format (e.g., "JWT", "Opaque", "SAML").
    /// </summary>
    string Format { get; }

    /// <summary>
    /// Gets whether this token type can be refreshed.
    /// </summary>
    bool CanBeRefreshed { get; }

    /// <summary>
    /// Gets whether this token type contains user identity information.
    /// </summary>
    bool ContainsUserIdentity { get; }

    /// <summary>
    /// Gets the typical lifetime in seconds for this token type.
    /// </summary>
    int? TypicalLifetimeSeconds { get; }
}
