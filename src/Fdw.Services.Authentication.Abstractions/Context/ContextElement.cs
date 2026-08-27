namespace Fdw.Services.Authentication.Abstractions.Context;

/// <summary>
/// The things an authentication step may require or contribute.
/// </summary>
/// <remarks>
/// Closed deliberately. It is the vocabulary the chain is made of, not a list of what anyone might
/// want to do — a new step contributing <see cref="Claims"/> needs no change here. The issued token
/// and the session are absent: the flow's product is not something a step may read or write.
/// </remarks>
public enum ContextElement
{
    /// <summary>Someone proved who they are.</summary>
    Subject = 1,

    /// <summary>That someone was resolved to a local principal.</summary>
    Principal = 2,

    /// <summary>Facts about the principal, each carrying where it came from.</summary>
    Claims = 3,

    /// <summary>Whether this principal may be issued a token at all.</summary>
    Decision = 4,
}
