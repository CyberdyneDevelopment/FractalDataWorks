using Fdw.Collections;

namespace Fdw.Services.Authentication.Abstractions;

/// <summary>
/// Base class for token types.
/// Implements the ITokenType interface with common functionality.
/// </summary>
public abstract class TokenTypeBase : TypeOptionBase<int, TokenTypeBase>, ITokenType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TokenTypeBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for the token type.</param>
    /// <param name="name">The name of the token type.</param>
    /// <param name="format">The token format.</param>
    /// <param name="canBeRefreshed">Whether this token can be refreshed.</param>
    /// <param name="containsUserIdentity">Whether this token contains user identity info.</param>
    /// <param name="typicalLifetimeSeconds">The typical lifetime in seconds.</param>
    protected TokenTypeBase(
        int id,
        string name,
        string format,
        bool canBeRefreshed,
        bool containsUserIdentity,
        int? typicalLifetimeSeconds)
        : base(id, name)
    {
        Format = format;
        CanBeRefreshed = canBeRefreshed;
        ContainsUserIdentity = containsUserIdentity;
        TypicalLifetimeSeconds = typicalLifetimeSeconds;
    }

    /// <inheritdoc/>
    public string Format { get; }

    /// <inheritdoc/>
    public bool CanBeRefreshed { get; }

    /// <inheritdoc/>
    public bool ContainsUserIdentity { get; }

    /// <inheritdoc/>
    public int? TypicalLifetimeSeconds { get; }
}
