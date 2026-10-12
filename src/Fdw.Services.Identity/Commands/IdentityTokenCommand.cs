using System;
using Fdw.Abstractions;
using Fdw.Services.Identity.Abstractions;

namespace Fdw.Services.Identity.Commands;

/// <summary>
/// The command form of a token acquisition, so an identity service satisfies the generic
/// <c>IGenericService.Execute</c> surface every FDW service exposes.
/// </summary>
/// <remarks>
/// <see cref="IIdentityService.Acquire"/> is the ergonomic surface callers use and is what the
/// outbound bridge calls; this command is the same operation reached through the generic path. The
/// domain owns this command — it is deliberately not an <c>ISecretManagerCommand</c>, because
/// acquiring an identity assertion is not retrieving a stored secret.
/// </remarks>
public sealed class IdentityTokenCommand : IGenericCommand
{
    /// <summary>Initializes a new instance of the <see cref="IdentityTokenCommand"/> class.</summary>
    /// <param name="request">The audience and scopes being asked for.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null.</exception>
    public IdentityTokenCommand(IdentityTokenRequest request)
    {
        Request = request ?? throw new ArgumentNullException(nameof(request));
        CommandId = Guid.CreateVersion7();
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>Gets the audience and scopes being asked for.</summary>
    public IdentityTokenRequest Request { get; }

    /// <inheritdoc/>
    public Guid CommandId { get; }

    /// <inheritdoc/>
    public DateTime CreatedAt { get; }

    /// <inheritdoc/>
    public string CommandType => nameof(IdentityTokenCommand);

    /// <inheritdoc/>
    public string Category => "Query";
}
